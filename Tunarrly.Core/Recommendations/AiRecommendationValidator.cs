using System.Text.Json;
using System.Text.RegularExpressions;
using Tunarrly.Core.Models;
using Tunarrly.Core.Normalization;

namespace Tunarrly.Core.Recommendations;

public static partial class AiRecommendationValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<RecommendationCandidate> ParseAndValidate(
        string? content,
        IEnumerable<string> existingNormalizedArtists,
        IEnumerable<string> ignoredNormalizedArtists,
        int maxRecommendations)
    {
        var json = ExtractJsonObject(content);
        if (json is null)
        {
            return [];
        }

        AiRecommendationResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AiRecommendationResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (parsed?.Recommendations is null)
        {
            return [];
        }

        var existing = existingNormalizedArtists.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ignored = ignoredNormalizedArtists.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return parsed.Recommendations
            .Where(x => !string.IsNullOrWhiteSpace(x.ArtistName))
            .Select(x => x with
            {
                ArtistName = MusicTextNormalizer.CleanArtistName(x.ArtistName),
                Score = Math.Clamp(x.Score, 0, 100),
                Confidence = Math.Clamp(x.Confidence, 0, 100),
                Reasons = CleanList(x.Reasons),
                RelatedArtists = CleanList(x.RelatedArtists),
                Genres = CleanList(x.Genres)
            })
            .Where(x => MusicTextNormalizer.IsLikelyArtistName(x.ArtistName))
            .Where(x => !existing.Contains(MusicTextNormalizer.NormalizeName(x.ArtistName)))
            .Where(x => !ignored.Contains(MusicTextNormalizer.NormalizeName(x.ArtistName)))
            .DistinctBy(x => MusicTextNormalizer.NormalizeName(x.ArtistName))
            .Take(Math.Max(0, maxRecommendations))
            .Select(x => new RecommendationCandidate(x.ArtistName, x.Score, x.Confidence, RecommendationSources.Ai, x.Reasons ?? [], x.RelatedArtists ?? [], x.Genres ?? []))
            .ToArray();
    }

    private static string? ExtractJsonObject(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.Trim();
        var fenced = JsonFenceRegex().Match(trimmed);
        if (fenced.Success)
        {
            trimmed = fenced.Groups["json"].Value.Trim();
        }

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : null;
    }

    private static IReadOnlyList<string> CleanList(IEnumerable<string>? values)
        => values?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];

    private sealed record AiRecommendationResponse(IReadOnlyList<AiRecommendation> Recommendations);
    private sealed record AiRecommendation(string ArtistName, int Score, int Confidence, IReadOnlyList<string>? Reasons, IReadOnlyList<string>? RelatedArtists, IReadOnlyList<string>? Genres, string? Source);

    [GeneratedRegex("^```(?:json)?\\s*(?<json>.*?)\\s*```$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex JsonFenceRegex();
}
