using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Normalization;
using Tunarrly.Core.Options;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;
using Tunarrly.Infrastructure.Recommendations;
using Tunarrly.Infrastructure.Settings;

namespace Tunarrly.Tests;

public sealed class RecommendationServiceIntegrationTests
{
    [Fact]
    public async Task GenerateLocalAsync_CreatesRecommendationAndMergesOnSecondRun()
    {
        var dbFactory = InfrastructureTestHelpers.CreateDbFactory();
        await SeedCreditAsync(dbFactory, "Burial", CreditTypes.Featured);
        var service = CreateService(dbFactory);

        await service.GenerateLocalAsync();
        await service.GenerateLocalAsync();

        await using var db = await dbFactory.CreateDbContextAsync();
        var recommendation = Assert.Single(await db.Recommendations.ToListAsync());
        Assert.Equal("Burial", recommendation.ArtistName);
        Assert.Contains("featured artist", recommendation.ReasonJson);
        Assert.NotEmpty(await db.ArtistRelations.ToListAsync());
    }

    [Fact]
    public async Task GenerateLocalAsync_DoesNotOverwriteIgnoredRecommendation()
    {
        var dbFactory = InfrastructureTestHelpers.CreateDbFactory();
        await SeedCreditAsync(dbFactory, "Burial", CreditTypes.Featured);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Recommendations.Add(new Recommendation { ArtistName = "Burial", NormalizedArtistName = MusicTextNormalizer.NormalizeName("Burial"), Score = 1, Source = RecommendationSources.Local, Status = RecommendationStatuses.Ignored });
            await db.SaveChangesAsync();
        }

        await CreateService(dbFactory).GenerateLocalAsync();

        await using var verify = await dbFactory.CreateDbContextAsync();
        var recommendation = Assert.Single(await verify.Recommendations.ToListAsync());
        Assert.Equal(RecommendationStatuses.Ignored, recommendation.Status);
        Assert.Equal(1, recommendation.Score);
    }

    private static RecommendationService CreateService(IDbContextFactory<TunarrlyDbContext> dbFactory)
    {
        var settings = new AppSettingsService(dbFactory, InfrastructureTestHelpers.Options(new LidarrOptions()), InfrastructureTestHelpers.Options(new LibraryOptions()), InfrastructureTestHelpers.Options(new AiOptions()));
        return new RecommendationService(dbFactory, new NoOpAiClient(), new NoOpLidarrClient(), new NoOpLidarrSync(), settings);
    }

    private static async Task SeedCreditAsync(IDbContextFactory<TunarrlyDbContext> dbFactory, string artistName, string creditType)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var artist = new LibraryArtist { Name = artistName, NormalizedName = MusicTextNormalizer.NormalizeName(artistName) };
        db.LibraryArtists.Add(artist);
        await db.SaveChangesAsync();
        var track = new LibraryTrack { Title = "Track", NormalizedTitle = "track", Path = $"/music/{Guid.NewGuid():N}.mp3", MainArtistId = artist.Id, FileModifiedAt = DateTimeOffset.UtcNow };
        db.LibraryTracks.Add(track);
        await db.SaveChangesAsync();
        db.TrackArtistCredits.Add(new TrackArtistCredit { TrackId = track.Id, ArtistName = artistName, NormalizedArtistName = artist.NormalizedName, CreditType = creditType });
        await db.SaveChangesAsync();
    }

    private sealed class NoOpAiClient : IAiProviderClient
    {
        public Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Ok("ok"));
        public Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecommendationCandidate>>([]);
    }

    private sealed class NoOpLidarrSync : ILidarrSyncService
    {
        public Task<OperationResult> SyncArtistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Ok("ok"));
    }

    private sealed class NoOpLidarrClient : ILidarrClient
    {
        public Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Ok("ok"));
        public Task<IReadOnlyList<LidarrArtistDto>> GetArtistsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LidarrArtistDto>>([]);
        public Task<IReadOnlyList<LidarrQualityProfileDto>> GetQualityProfilesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LidarrQualityProfileDto>>([]);
        public Task<IReadOnlyList<LidarrMetadataProfileDto>> GetMetadataProfilesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LidarrMetadataProfileDto>>([]);
        public Task<IReadOnlyList<LidarrRootFolderDto>> GetRootFoldersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LidarrRootFolderDto>>([]);
        public Task<IReadOnlyList<LidarrLookupResult>> SearchArtistAsync(string artistName, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LidarrLookupResult>>([]);
        public Task<OperationResult> AddArtistAsync(LidarrLookupResult artist, LidarrAddOptions? addOptions = null, CancellationToken cancellationToken = default) => Task.FromResult(OperationResult.Ok("ok"));
    }
}
