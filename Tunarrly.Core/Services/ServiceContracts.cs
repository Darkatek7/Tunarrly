using Tunarrly.Core.Models;
using Tunarrly.Core.Options;

namespace Tunarrly.Core.Services;

public interface IAppSettingsService
{
    Task<LidarrOptions> GetLidarrOptionsAsync(CancellationToken cancellationToken = default);
    Task<AiOptions> GetAiOptionsAsync(CancellationToken cancellationToken = default);
    Task<LibraryOptions> GetLibraryOptionsAsync(CancellationToken cancellationToken = default);
    Task SaveLidarrOptionsAsync(LidarrOptions options, CancellationToken cancellationToken = default);
    Task SaveAiOptionsAsync(AiOptions options, CancellationToken cancellationToken = default);
    Task SaveLibraryOptionsAsync(LibraryOptions options, CancellationToken cancellationToken = default);
    Task ClearSecretAsync(string key, CancellationToken cancellationToken = default);
}

public interface ILidarrClient
{
    Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LidarrArtistDto>> GetArtistsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LidarrQualityProfileDto>> GetQualityProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LidarrMetadataProfileDto>> GetMetadataProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LidarrRootFolderDto>> GetRootFoldersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LidarrLookupResult>> SearchArtistAsync(string artistName, CancellationToken cancellationToken = default);
    Task<OperationResult> AddArtistAsync(LidarrLookupResult artist, LidarrAddOptions? addOptions = null, CancellationToken cancellationToken = default);
}

public interface IAiProviderClient
{
    Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationsAsync(CancellationToken cancellationToken = default);
}

public interface IAiContextService
{
    Task<AiContextPreview> BuildPreviewAsync(int maxArtists, CancellationToken cancellationToken = default);
}

public interface ILidarrSyncService
{
    Task<OperationResult> SyncArtistsAsync(CancellationToken cancellationToken = default);
}

public interface ILibraryScanner
{
    Task<OperationResult> ScanAsync(string? libraryPath = null, CancellationToken cancellationToken = default);
}

public interface IScanJobService
{
    Task<OperationResult> EnqueueScanAsync(string? libraryPath = null, CancellationToken cancellationToken = default);
}

public interface IRecommendationService
{
    Task<OperationResult> GenerateLocalAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> GenerateAiAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> SetStatusAsync(int recommendationId, string status, CancellationToken cancellationToken = default);
    Task<OperationResult> AddToLidarrAsync(int recommendationId, LidarrLookupResult match, LidarrAddOptions? addOptions = null, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default);
}
