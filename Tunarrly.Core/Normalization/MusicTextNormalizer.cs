using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Tunarrly.Core.Normalization;

public static partial class MusicTextNormalizer
{
    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        normalized = PunctuationRegex().Replace(normalized, " ");
        normalized = WhiteSpaceRegex().Replace(normalized, " ").Trim();
        return normalized;
    }

    public static string NormalizeTitle(string? value) => NormalizeName(value);

    public static string NormalizeGenreLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var first = GenreSeparatorRegex().Split(value).Select(CleanArtistName).FirstOrDefault(IsLikelyArtistName);
        if (string.IsNullOrWhiteSpace(first)) return string.Empty;
        var normalized = NormalizeName(first);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }

    public static IReadOnlyList<string> SplitArtistList(params string?[] values)
    {
        var results = new List<string>();
        foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            var candidates = ConservativeArtistSeparatorRegex().Split(value!);
            foreach (var candidate in candidates)
            {
                var cleaned = CleanArtistName(candidate);
                if (IsLikelyArtistName(cleaned))
                {
                    results.Add(cleaned);
                }
            }
        }

        return results.DistinctBy(NormalizeName).ToArray();
    }

    public static IReadOnlyList<string> ExtractCollaborators(string? title, string? artist, string? albumArtist, IEnumerable<string>? contributingArtists = null)
    {
        var results = new List<string>();
        foreach (var source in new[] { title, artist, albumArtist }.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            foreach (Match match in CollaborationRegex().Matches(source!))
            {
                results.AddRange(SplitArtistList(match.Groups["artists"].Value));
            }
        }

        if (contributingArtists is not null)
        {
            results.AddRange(SplitArtistList(contributingArtists.ToArray()));
        }

        return results.Select(CleanArtistName)
            .Where(IsLikelyArtistName)
            .DistinctBy(NormalizeName)
            .ToArray();
    }

    public static string CleanArtistName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Trim();
        cleaned = cleaned.Trim(' ', '-', '_', ',', ';', ':', '/', '\\', '(', ')', '[', ']', '{', '}');
        cleaned = WhiteSpaceRegex().Replace(cleaned, " ").Trim();
        return cleaned;
    }

    public static bool IsLikelyArtistName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var cleaned = value.Trim();
        return cleaned.Length is >= 2 and <= 120
            && cleaned.Any(char.IsLetterOrDigit)
            && !cleaned.Contains("remix", StringComparison.OrdinalIgnoreCase)
            && !cleaned.Contains("edit", StringComparison.OrdinalIgnoreCase)
            && !cleaned.Contains("version", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[\p{P}\p{S}]+", RegexOptions.Compiled)]
    private static partial Regex PunctuationRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"\s*(?:,|;|\s+and\s+)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ConservativeArtistSeparatorRegex();

    [GeneratedRegex(@"\s*(?:,|;|/|\||\\)\s*", RegexOptions.Compiled)]
    private static partial Regex GenreSeparatorRegex();

    [GeneratedRegex(@"(?:\bfeat\.?|\bft\.?|\bfeaturing\b|\bwith\b|\bvs\.?|\bx\b|&)\s+(?<artists>[^\)\]\[\(]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CollaborationRegex();
}
