namespace Tunarrly.Core.Models;

public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message) => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}

public sealed record DashboardStats(
    bool LidarrConfigured,
    bool AiEnabled,
    int LidarrArtists,
    int LibraryArtists,
    int LibraryTracks,
    int NewRecommendations,
    int AiRecommendations,
    DateTimeOffset? LastScanAt,
    DateTimeOffset? LastAiRunAt);

public sealed record LidarrArtistDto(
    int Id,
    string ArtistName,
    string? ForeignArtistId,
    string? MusicBrainzId,
    bool Monitored,
    string? Path,
    int? QualityProfileId,
    int? MetadataProfileId);

public sealed record LidarrLookupResult(
    string ArtistName,
    string? ForeignArtistId,
    string? MusicBrainzId,
    string? Overview,
    string? Disambiguation);

public sealed record LidarrQualityProfileDto(int Id, string Name);

public sealed record LidarrMetadataProfileDto(int Id, string Name);

public sealed record LidarrRootFolderDto(int Id, string Path, long? FreeSpaceBytes);

public sealed record ProfileItem(string Name, int Count, IReadOnlyList<string> Evidence);

public sealed record RecommendationCandidate(
    string ArtistName,
    int Score,
    int? Confidence,
    string Source,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RelatedArtists,
    IReadOnlyList<string> Genres);

public sealed record ScanRequest(string LibraryPath);
