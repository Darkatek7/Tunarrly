namespace Tunarrly.Infrastructure.Data;

public sealed class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LibraryArtist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? MusicBrainzId { get; set; }
    public string Source { get; set; } = "Library";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LibraryAlbum
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public LibraryArtist? Artist { get; set; }
    public int? Year { get; set; }
    public string? Path { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LibraryTrack
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public int? AlbumId { get; set; }
    public LibraryAlbum? Album { get; set; }
    public int? MainArtistId { get; set; }
    public LibraryArtist? MainArtist { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public double? DurationSeconds { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTimeOffset FileModifiedAt { get; set; }
    public string? FileHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TrackArtistCredit
{
    public int Id { get; set; }
    public int TrackId { get; set; }
    public LibraryTrack? Track { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string NormalizedArtistName { get; set; } = string.Empty;
    public string CreditType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LidarrArtist
{
    public int Id { get; set; }
    public int LidarrId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? MusicBrainzId { get; set; }
    public string? ForeignArtistId { get; set; }
    public bool Monitored { get; set; }
    public string? Path { get; set; }
    public int? QualityProfileId { get; set; }
    public int? MetadataProfileId { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ArtistRelation
{
    public int Id { get; set; }
    public string SourceArtistName { get; set; } = string.Empty;
    public string SourceNormalizedName { get; set; } = string.Empty;
    public string TargetArtistName { get; set; } = string.Empty;
    public string TargetNormalizedName { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Recommendation
{
    public int Id { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string NormalizedArtistName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int? Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReasonJson { get; set; } = "[]";
    public string RelatedArtistsJson { get; set; } = "[]";
    public string GenresJson { get; set; } = "[]";
    public DateTimeOffset LastCalculatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AiRecommendationRun
{
    public int Id { get; set; }
    public string ProviderBaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InputSummaryJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}

public sealed class ScanJob
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LibraryPath { get; set; } = string.Empty;
    public string? CurrentFile { get; set; }
    public int FilesDiscovered { get; set; }
    public int FilesScanned { get; set; }
    public int FilesFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}
