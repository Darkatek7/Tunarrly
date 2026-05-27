using Tunarrly.Core.Recommendations;

namespace Tunarrly.Tests;

public sealed class AiRecommendationValidatorTests
{
    [Fact]
    public void ParseAndValidate_ReturnsEmptyForInvalidJson()
    {
        var results = AiRecommendationValidator.ParseAndValidate("not json", [], [], 10);
        Assert.Empty(results);
    }

    [Fact]
    public void ParseAndValidate_HandlesMarkdownFencedJsonAndClampsScores()
    {
        const string json = """
        ```json
        {
          "recommendations": [
            {
              "artistName": "Burial",
              "score": 120,
              "confidence": -5,
              "reasons": ["Shares late-night electronic patterns"],
              "relatedArtists": ["Four Tet"],
              "genres": ["Electronic"],
              "source": "AI"
            }
          ]
        }
        ```
        """;

        var result = Assert.Single(AiRecommendationValidator.ParseAndValidate(json, [], [], 10));
        Assert.Equal("Burial", result.ArtistName);
        Assert.Equal(100, result.Score);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void ParseAndValidate_ExcludesExistingIgnoredAndDuplicateArtists()
    {
        const string json = """
        {
          "recommendations": [
            { "artistName": "Skrillex", "score": 90, "confidence": 90, "reasons": [], "relatedArtists": [], "genres": [], "source": "AI" },
            { "artistName": "Burial", "score": 80, "confidence": 70, "reasons": [], "relatedArtists": [], "genres": [], "source": "AI" },
            { "artistName": "burial", "score": 70, "confidence": 60, "reasons": [], "relatedArtists": [], "genres": [], "source": "AI" },
            { "artistName": "Aphex Twin", "score": 70, "confidence": 60, "reasons": [], "relatedArtists": [], "genres": [], "source": "AI" }
          ]
        }
        """;

        var results = AiRecommendationValidator.ParseAndValidate(json, ["skrillex"], ["aphex twin"], 10);
        var result = Assert.Single(results);
        Assert.Equal("Burial", result.ArtistName);
    }
}
