using Tunarrly.Core.Models;
using Tunarrly.Infrastructure.AI;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Tests;

public sealed class AiContextServiceTests
{
    [Fact]
    public async Task BuildPreviewAsync_DoesNotIncludeLocalPathsOrSecrets()
    {
        var dbFactory = InfrastructureTestHelpers.CreateDbFactory();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var artist = new LibraryArtist { Name = "Burial", NormalizedName = "burial" };
            var album = new LibraryAlbum { Title = "Untrue", NormalizedTitle = "untrue", Artist = artist, Path = "/music/private/Burial/Untrue" };
            var track = new LibraryTrack
            {
                Title = "Archangel",
                NormalizedTitle = "archangel",
                Album = album,
                MainArtist = artist,
                Path = "/music/private/Burial/Untrue/Archangel.flac",
                Genre = "Dubstep",
                FileModifiedAt = DateTimeOffset.UtcNow
            };
            db.LibraryArtists.Add(artist);
            db.LibraryAlbums.Add(album);
            db.LibraryTracks.Add(track);
            db.TrackArtistCredits.Add(new TrackArtistCredit { Track = track, ArtistName = "Burial", NormalizedArtistName = "burial", CreditType = CreditTypes.Main });
            db.LidarrArtists.Add(new LidarrArtist { LidarrId = 1, Name = "Four Tet", NormalizedName = "four tet", Monitored = true, Path = "/music/private/Four Tet" });
            db.AppSettings.Add(new AppSetting { Key = "AI:ApiKey", Value = "secret-token", IsSecret = true });
            await db.SaveChangesAsync();
        }

        var preview = await new AiContextService(dbFactory).BuildPreviewAsync(25);

        Assert.Contains("Burial", preview.PayloadJson);
        Assert.Contains("Dubstep", preview.PayloadJson);
        Assert.DoesNotContain("/music/private", preview.PayloadJson);
        Assert.DoesNotContain("Archangel.flac", preview.PayloadJson);
        Assert.DoesNotContain("secret-token", preview.PayloadJson);
        Assert.Contains(preview.Excluded, item => item.Contains("Local file paths", StringComparison.OrdinalIgnoreCase));
    }
}
