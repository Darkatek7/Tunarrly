using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tunarrly.Core.Models;
using Tunarrly.Core.Normalization;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Scanning;

public sealed class LibraryScanner(IAppSettingsService settings, IDbContextFactory<TunarrlyDbContext> dbFactory, ILogger<LibraryScanner> logger) : ILibraryScanner
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".flac", ".m4a", ".ogg", ".opus", ".wav", ".aac" };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationResult> ScanAsync(string? libraryPath = null, CancellationToken cancellationToken = default)
    {
        var configured = await settings.GetLibraryOptionsAsync(cancellationToken);
        var path = string.IsNullOrWhiteSpace(libraryPath) ? configured.Path : libraryPath;
        if (!Directory.Exists(path)) return OperationResult.Fail($"Library path does not exist: {path}");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ScanJobs.AnyAsync(x => x.Status == JobStatuses.Running, cancellationToken))
        {
            return OperationResult.Fail("A library scan is already running.");
        }

        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Where(x => Extensions.Contains(Path.GetExtension(x))).ToArray();
        var job = new ScanJob { Status = JobStatuses.Running, LibraryPath = path, FilesDiscovered = files.Length, StartedAt = DateTimeOffset.UtcNow };
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        var failedFiles = new List<string>();

        foreach (var file in files)
        {
            job.CurrentFile = Path.GetFileName(file);
            try
            {
                if (await IndexFileAsync(db, file, cancellationToken))
                {
                    job.FilesScanned++;
                }
                else
                {
                    job.FilesSkipped++;
                }
            }
            catch (Exception ex)
            {
                job.FilesFailed++;
                failedFiles.Add(Path.GetFileName(file));
                job.FailureSummaryJson = JsonSerializer.Serialize(failedFiles.Distinct().Take(10), JsonOptions);
                logger.LogWarning(ex, "Failed to index audio file {FileName}", Path.GetFileName(file));
            }

            if ((job.FilesScanned + job.FilesSkipped + job.FilesFailed) % 25 == 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await RemoveMissingTracksAsync(db, path, files, cancellationToken);

        job.Status = JobStatuses.Completed;
        job.CurrentFile = null;
        job.ErrorMessage = job.FilesFailed == 0 ? null : $"{job.FilesFailed} files failed. Stored failed-file summary contains file names only.";
        job.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Indexed {job.FilesScanned} files. Skipped unchanged: {job.FilesSkipped}. Failed: {job.FilesFailed}.");
    }

    private static async Task<bool> IndexFileAsync(TunarrlyDbContext db, string file, CancellationToken cancellationToken)
    {
        var info = new FileInfo(file);
        var existingTrack = await db.LibraryTracks.SingleOrDefaultAsync(x => x.Path == file, cancellationToken);
        if (existingTrack is not null && existingTrack.FileSizeBytes == info.Length && existingTrack.FileModifiedAt == info.LastWriteTimeUtc)
        {
            return false;
        }

        using var tagFile = TagLib.File.Create(file);
        var tag = tagFile.Tag;
        var title = string.IsNullOrWhiteSpace(tag.Title) ? Path.GetFileNameWithoutExtension(file) : tag.Title;
        var albumTitle = string.IsNullOrWhiteSpace(tag.Album) ? "Unknown Album" : tag.Album;
        var artistName = MusicTextNormalizer.CleanArtistName(tag.FirstPerformer ?? tag.FirstAlbumArtist ?? "Unknown Artist");
        var albumArtistName = MusicTextNormalizer.CleanArtistName(tag.FirstAlbumArtist ?? artistName);

        var artist = await UpsertArtistAsync(db, artistName, cancellationToken);
        var albumArtist = await UpsertArtistAsync(db, albumArtistName, cancellationToken);
        var album = await db.LibraryAlbums.FirstOrDefaultAsync(x => x.ArtistId == albumArtist.Id && x.NormalizedTitle == MusicTextNormalizer.NormalizeTitle(albumTitle), cancellationToken);
        if (album is null)
        {
            album = new LibraryAlbum { Title = albumTitle, NormalizedTitle = MusicTextNormalizer.NormalizeTitle(albumTitle), ArtistId = albumArtist.Id, Year = tag.Year > 0 ? (int)tag.Year : null, Path = Path.GetDirectoryName(file) };
            db.LibraryAlbums.Add(album);
            await db.SaveChangesAsync(cancellationToken);
        }

        var track = existingTrack;
        if (track is null)
        {
            track = new LibraryTrack { Path = file, CreatedAt = DateTimeOffset.UtcNow };
            db.LibraryTracks.Add(track);
        }

        track.Title = title;
        track.NormalizedTitle = MusicTextNormalizer.NormalizeTitle(title);
        track.AlbumId = album.Id;
        track.MainArtistId = artist.Id;
        track.Genre = tag.Genres.FirstOrDefault();
        track.Year = tag.Year > 0 ? (int)tag.Year : null;
        track.DurationSeconds = tagFile.Properties.Duration.TotalSeconds;
        track.FileSizeBytes = info.Length;
        track.FileModifiedAt = info.LastWriteTimeUtc;
        track.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var existingCredits = db.TrackArtistCredits.Where(x => x.TrackId == track.Id);
        db.TrackArtistCredits.RemoveRange(existingCredits);
        AddCredit(db, track.Id, artistName, CreditTypes.Main);
        AddCredit(db, track.Id, albumArtistName, CreditTypes.AlbumArtist);
        foreach (var collaborator in MusicTextNormalizer.ExtractCollaborators(title, string.Join(", ", tag.Performers), albumArtistName, tag.Performers))
        {
            await UpsertArtistAsync(db, collaborator, cancellationToken);
            AddCredit(db, track.Id, collaborator, CreditTypes.Featured);
        }

        return true;
    }

    private static async Task<LibraryArtist> UpsertArtistAsync(TunarrlyDbContext db, string name, CancellationToken cancellationToken)
    {
        var normalized = MusicTextNormalizer.NormalizeName(name);
        var artist = await db.LibraryArtists.SingleOrDefaultAsync(x => x.NormalizedName == normalized, cancellationToken);
        if (artist is not null) return artist;
        artist = new LibraryArtist { Name = name, NormalizedName = normalized };
        db.LibraryArtists.Add(artist);
        await db.SaveChangesAsync(cancellationToken);
        return artist;
    }

    private static void AddCredit(TunarrlyDbContext db, int trackId, string artistName, string creditType)
    {
        if (!MusicTextNormalizer.IsLikelyArtistName(artistName)) return;
        db.TrackArtistCredits.Add(new TrackArtistCredit { TrackId = trackId, ArtistName = artistName, NormalizedArtistName = MusicTextNormalizer.NormalizeName(artistName), CreditType = creditType });
    }

    private static async Task RemoveMissingTracksAsync(TunarrlyDbContext db, string libraryPath, IReadOnlyCollection<string> discoveredFiles, CancellationToken cancellationToken)
    {
        var discovered = discoveredFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tracks = await db.LibraryTracks.Where(x => x.Path.StartsWith(libraryPath)).ToListAsync(cancellationToken);
        var missing = tracks.Where(x => !discovered.Contains(x.Path)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        db.LibraryTracks.RemoveRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
