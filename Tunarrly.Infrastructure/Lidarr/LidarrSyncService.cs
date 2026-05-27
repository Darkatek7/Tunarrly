using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Normalization;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Lidarr;

public sealed class LidarrSyncService(ILidarrClient client, IDbContextFactory<TunarrlyDbContext> dbFactory) : ILidarrSyncService
{
    public async Task<OperationResult> SyncArtistsAsync(CancellationToken cancellationToken = default)
    {
        var artists = await client.GetArtistsAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        foreach (var artist in artists)
        {
            var existing = await db.LidarrArtists.SingleOrDefaultAsync(x => x.LidarrId == artist.Id, cancellationToken);
            if (existing is null)
            {
                db.LidarrArtists.Add(new LidarrArtist { LidarrId = artist.Id });
                existing = db.LidarrArtists.Local.Last();
            }

            existing.Name = artist.ArtistName;
            existing.NormalizedName = MusicTextNormalizer.NormalizeName(artist.ArtistName);
            existing.ForeignArtistId = artist.ForeignArtistId;
            existing.MusicBrainzId = artist.MusicBrainzId;
            existing.Monitored = artist.Monitored;
            existing.Path = artist.Path;
            existing.QualityProfileId = artist.QualityProfileId;
            existing.MetadataProfileId = artist.MetadataProfileId;
            existing.LastSyncedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Synced {artists.Count} Lidarr artists.");
    }
}
