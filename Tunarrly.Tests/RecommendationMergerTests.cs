using Tunarrly.Core.Models;
using Tunarrly.Core.Recommendations;

namespace Tunarrly.Tests;

public sealed class RecommendationMergerTests
{
    [Fact]
    public void Merge_CombinesDifferentSourcesIntoHybridAndPreservesEvidence()
    {
        var local = new RecommendationCandidate("Burial", 75, null, RecommendationSources.Local, ["Already in library"], ["Four Tet"], ["Electronic"]);
        var ai = new RecommendationCandidate("Burial", 88, 92, RecommendationSources.Ai, ["Matches late-night electronic"], ["four tet", "Skrillex"], ["electronic", "Dubstep"]);

        var merged = RecommendationMerger.Merge(local, ai);

        Assert.Equal(RecommendationSources.Hybrid, merged.Source);
        Assert.Equal(88, merged.Score);
        Assert.Equal(92, merged.Confidence);
        Assert.Contains("Already in library", merged.Reasons);
        Assert.Contains("Matches late-night electronic", merged.Reasons);
        Assert.Equal(2, merged.RelatedArtists.Count);
        Assert.Equal(2, merged.Genres.Count);
    }
}
