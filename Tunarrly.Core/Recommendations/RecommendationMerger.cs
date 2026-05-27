using Tunarrly.Core.Models;

namespace Tunarrly.Core.Recommendations;

public static class RecommendationMerger
{
    public static RecommendationCandidate Merge(RecommendationCandidate existing, RecommendationCandidate incoming)
    {
        var source = existing.Source == incoming.Source ? existing.Source : RecommendationSources.Hybrid;
        return new RecommendationCandidate(
            existing.ArtistName,
            Math.Max(existing.Score, incoming.Score),
            Math.Max(existing.Confidence ?? 0, incoming.Confidence ?? 0),
            source,
            existing.Reasons.Concat(incoming.Reasons).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            existing.RelatedArtists.Concat(incoming.RelatedArtists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            existing.Genres.Concat(incoming.Genres).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
