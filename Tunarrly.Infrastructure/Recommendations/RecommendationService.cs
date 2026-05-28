using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Normalization;
using Tunarrly.Core.Recommendations;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Recommendations;

public sealed class RecommendationService(
    IDbContextFactory<TunarrlyDbContext> dbFactory,
    IAiProviderClient aiClient,
    ILidarrClient lidarrClient,
    ILidarrSyncService lidarrSync,
    IAppSettingsService settings) : IRecommendationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationResult> GenerateLocalAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var lidarrNames = await db.LidarrArtists.Where(x => x.Monitored).Select(x => x.NormalizedName).ToListAsync(cancellationToken);
        var ignored = await db.Recommendations.Where(x => x.Status == RecommendationStatuses.Ignored).Select(x => x.NormalizedArtistName).ToListAsync(cancellationToken);
        var credits = await db.TrackArtistCredits.ToListAsync(cancellationToken);
        var genresByArtist = await db.LibraryTracks
            .Where(x => x.Genre != null && x.MainArtist != null)
            .Select(x => new { x.MainArtist!.Name, x.MainArtist.NormalizedName, x.Genre })
            .ToListAsync(cancellationToken);

        var candidates = new Dictionary<string, MutableCandidate>();
        foreach (var group in credits.GroupBy(x => x.NormalizedArtistName))
        {
            if (lidarrNames.Contains(group.Key) || ignored.Contains(group.Key) || string.IsNullOrWhiteSpace(group.Key)) continue;
            var name = group.First().ArtistName;
            var candidate = Get(candidates, name);
            var count = group.Count();
            candidate.Score += Math.Min(35, count * 4);
            candidate.Reasons.Add($"Appears in your downloaded library on {count} track credit{(count == 1 ? string.Empty : "s")}");
            candidate.Evidence.Add(new RecommendationEvidence("AlreadyInLibrary", $"Appears in your downloaded library on {count} track credit{(count == 1 ? string.Empty : "s")}", Math.Min(35, count * 4), new Dictionary<string, string> { ["trackCredits"] = count.ToString() }));
            if (group.Any(x => x.CreditType == CreditTypes.Featured))
            {
                var featuredCount = group.Count(x => x.CreditType == CreditTypes.Featured);
                candidate.Score += Math.Min(30, featuredCount * 8);
                candidate.Reasons.Add($"Appears as featured artist on {featuredCount} downloaded track{(featuredCount == 1 ? string.Empty : "s")}");
                candidate.Evidence.Add(new RecommendationEvidence("FeaturedCredit", $"Appears as featured artist on {featuredCount} downloaded track{(featuredCount == 1 ? string.Empty : "s")}", Math.Min(30, featuredCount * 8), new Dictionary<string, string> { ["featuredTrackCredits"] = featuredCount.ToString() }));
            }
        }

        foreach (var genreGroup in genresByArtist.GroupBy(x => x.NormalizedName))
        {
            if (lidarrNames.Contains(genreGroup.Key) || ignored.Contains(genreGroup.Key)) continue;
            var topGenre = genreGroup.Where(x => !string.IsNullOrWhiteSpace(x.Genre)).GroupBy(x => x.Genre!).OrderByDescending(x => x.Count()).FirstOrDefault();
            if (topGenre is null) continue;
            var candidate = Get(candidates, genreGroup.First().Name);
            candidate.Score += Math.Min(20, topGenre.Count() * 2);
            candidate.Genres.Add(topGenre.Key);
            candidate.Reasons.Add($"Shares genre {topGenre.Key} with {topGenre.Count()} track{(topGenre.Count() == 1 ? string.Empty : "s")} in your library");
            candidate.Evidence.Add(new RecommendationEvidence("SharedGenre", $"Shares genre {topGenre.Key} with {topGenre.Count()} track{(topGenre.Count() == 1 ? string.Empty : "s")} in your library", Math.Min(20, topGenre.Count() * 2), new Dictionary<string, string> { ["genre"] = topGenre.Key, ["tracks"] = topGenre.Count().ToString() }));
        }

        foreach (var candidate in candidates.Values)
        {
            candidate.Score = Math.Clamp(candidate.Score + 25, 1, 100);
            await UpsertRecommendationAsync(db, candidate.ToCandidate(RecommendationSources.Local), cancellationToken);
            await UpsertEvidenceRelationAsync(db, candidate, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Generated {candidates.Count} local recommendations.");
    }

    public async Task<OperationResult> GenerateAiAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var options = await settings.GetAiOptionsAsync(cancellationToken);
        var run = new AiRecommendationRun
        {
            Status = JobStatuses.Running,
            ProviderBaseUrl = options.BaseUrl,
            Model = options.Model,
            InputSummaryJson = await BuildAiInputSummaryAsync(db, cancellationToken),
            StartedAt = DateTimeOffset.UtcNow
        };
        db.AiRecommendationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var candidates = await aiClient.GetRecommendationsAsync(cancellationToken);
            foreach (var candidate in candidates)
            {
                await UpsertRecommendationAsync(db, candidate, cancellationToken);
            }

            run.Status = JobStatuses.Completed;
            run.OutputJson = JsonSerializer.Serialize(candidates, JsonOptions);
            run.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return OperationResult.Ok($"Generated {candidates.Count} AI recommendations.");
        }
        catch (Exception ex)
        {
            run.Status = JobStatuses.Failed;
            run.ErrorMessage = ex.Message;
            run.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return OperationResult.Fail($"AI recommendation failed: {ex.Message}");
        }
    }

    public async Task<OperationResult> SetStatusAsync(int recommendationId, string status, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var recommendation = await db.Recommendations.FindAsync([recommendationId], cancellationToken);
        if (recommendation is null) return OperationResult.Fail("Recommendation not found.");
        recommendation.Status = status;
        recommendation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Recommendation marked {status}.");
    }

    public async Task<OperationResult> AddToLidarrAsync(int recommendationId, LidarrLookupResult match, LidarrAddOptions? addOptions = null, CancellationToken cancellationToken = default)
    {
        var result = await lidarrClient.AddArtistAsync(match, addOptions, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var recommendation = await db.Recommendations.FindAsync([recommendationId], cancellationToken);
        if (recommendation is not null)
        {
            recommendation.Status = result.Success ? RecommendationStatuses.AddedToLidarr : RecommendationStatuses.FailedToAdd;
            recommendation.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        if (result.Success) await lidarrSync.SyncArtistsAsync(cancellationToken);
        return result;
    }

    private static async Task UpsertRecommendationAsync(TunarrlyDbContext db, RecommendationCandidate candidate, CancellationToken cancellationToken)
    {
        var normalized = MusicTextNormalizer.NormalizeName(candidate.ArtistName);
        if (string.IsNullOrWhiteSpace(normalized)) return;
        var existing = await db.Recommendations.SingleOrDefaultAsync(x => x.NormalizedArtistName == normalized, cancellationToken);
        if (existing is null)
        {
            db.Recommendations.Add(new Recommendation
            {
                ArtistName = candidate.ArtistName,
                NormalizedArtistName = normalized,
                Score = Math.Clamp(candidate.Score, 0, 100),
                Confidence = candidate.Confidence,
                Source = candidate.Source,
                Status = RecommendationStatuses.New,
                ReasonJson = JsonSerializer.Serialize(candidate.Reasons.Distinct(), JsonOptions),
                RelatedArtistsJson = JsonSerializer.Serialize(candidate.RelatedArtists.Distinct(), JsonOptions),
                GenresJson = JsonSerializer.Serialize(candidate.Genres.Distinct(), JsonOptions),
                LastCalculatedAt = DateTimeOffset.UtcNow
            });
            return;
        }

        if (existing.Status == RecommendationStatuses.Ignored) return;
        var merged = RecommendationMerger.Merge(
            new RecommendationCandidate(existing.ArtistName, existing.Score, existing.Confidence, existing.Source, ReadList(existing.ReasonJson), ReadList(existing.RelatedArtistsJson), ReadList(existing.GenresJson)),
            candidate);
        existing.Score = merged.Score;
        existing.Confidence = merged.Confidence;
        existing.Source = merged.Source;
        existing.ReasonJson = JsonSerializer.Serialize(merged.Reasons, JsonOptions);
        existing.RelatedArtistsJson = JsonSerializer.Serialize(merged.RelatedArtists, JsonOptions);
        existing.GenresJson = JsonSerializer.Serialize(merged.Genres, JsonOptions);
        existing.LastCalculatedAt = DateTimeOffset.UtcNow;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static IReadOnlyList<string> ReadList(string json)
        => JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? Array.Empty<string>();

    private static async Task<string> BuildAiInputSummaryAsync(TunarrlyDbContext db, CancellationToken cancellationToken)
    {
        var summary = new
        {
            lidarrArtists = await db.LidarrArtists.CountAsync(cancellationToken),
            libraryArtists = await db.LibraryArtists.CountAsync(cancellationToken),
            libraryTracks = await db.LibraryTracks.CountAsync(cancellationToken),
            localRecommendations = await db.Recommendations.CountAsync(x => x.Source != RecommendationSources.Ai, cancellationToken),
            ignoredArtists = await db.Recommendations.CountAsync(x => x.Status == RecommendationStatuses.Ignored, cancellationToken)
        };

        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static async Task UpsertEvidenceRelationAsync(TunarrlyDbContext db, MutableCandidate candidate, CancellationToken cancellationToken)
    {
        var normalized = MusicTextNormalizer.NormalizeName(candidate.ArtistName);
        var existing = await db.ArtistRelations.FirstOrDefaultAsync(x => x.SourceNormalizedName == "library" && x.TargetNormalizedName == normalized && x.RelationType == RelationTypes.Manual, cancellationToken);
        if (existing is null)
        {
            db.ArtistRelations.Add(new ArtistRelation
            {
                SourceArtistName = "Library",
                SourceNormalizedName = "library",
                TargetArtistName = candidate.ArtistName,
                TargetNormalizedName = normalized,
                RelationType = RelationTypes.Manual,
                Weight = candidate.Evidence.Sum(x => x.Weight),
                EvidenceJson = JsonSerializer.Serialize(candidate.Evidence, JsonOptions)
            });
            return;
        }

        existing.TargetArtistName = candidate.ArtistName;
        existing.Weight = candidate.Evidence.Sum(x => x.Weight);
        existing.EvidenceJson = JsonSerializer.Serialize(candidate.Evidence, JsonOptions);
    }

    private static MutableCandidate Get(Dictionary<string, MutableCandidate> candidates, string name)
    {
        var normalized = MusicTextNormalizer.NormalizeName(name);
        if (candidates.TryGetValue(normalized, out var candidate)) return candidate;
        candidate = new MutableCandidate(name);
        candidates[normalized] = candidate;
        return candidate;
    }

    private sealed class MutableCandidate(string artistName)
    {
        public string ArtistName { get; } = artistName;
        public int Score { get; set; }
        public List<string> Reasons { get; } = [];
        public List<string> RelatedArtists { get; } = [];
        public List<string> Genres { get; } = [];
        public List<RecommendationEvidence> Evidence { get; } = [];
        public RecommendationCandidate ToCandidate(string source) => new(ArtistName, Score, null, source, Reasons.Distinct().ToArray(), RelatedArtists.Distinct().ToArray(), Genres.Distinct().ToArray());
    }
}
