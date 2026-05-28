using Tunarrly.Core.Normalization;

namespace Tunarrly.Tests;

public sealed class NormalizationTests
{
    [Theory]
    [InlineData("  Fred   Again.. ", "fred again")]
    [InlineData("SKRILLEX", "skrillex")]
    [InlineData("Beyonce / Jay-Z", "beyonce jay z")]
    public void NormalizeName_CleansCaseWhitespaceAndPunctuation(string input, string expected)
    {
        Assert.Equal(expected, MusicTextNormalizer.NormalizeName(input));
    }

    [Fact]
    public void NormalizeName_DoesNotCollapseLeadingThe()
    {
        Assert.NotEqual(MusicTextNormalizer.NormalizeName("The National"), MusicTextNormalizer.NormalizeName("National"));
    }

    [Theory]
    [InlineData("Song (feat. Four Tet)", "Four Tet")]
    [InlineData("Track ft. Flowdan", "Flowdan")]
    [InlineData("Tune featuring Romy", "Romy")]
    [InlineData("Artist x Collaborator", "Collaborator")]
    [InlineData("Artist & Partner", "Partner")]
    public void ExtractCollaborators_FindsSupportedFeaturingPatterns(string input, string expected)
    {
        var collaborators = MusicTextNormalizer.ExtractCollaborators(input, null, null);
        Assert.Contains(expected, collaborators);
    }

    [Theory]
    [InlineData("club remix")]
    [InlineData("radio edit")]
    [InlineData("extended version")]
    public void IsLikelyArtistName_RejectsCommonNoisyTerms(string input)
    {
        Assert.False(MusicTextNormalizer.IsLikelyArtistName(input));
    }

    [Theory]
    [InlineData("electronic; dubstep", "Electronic")]
    [InlineData("hip-hop / rap", "Hip Hop")]
    public void NormalizeGenreLabel_UsesFirstCleanGenre(string input, string expected)
    {
        Assert.Equal(expected, MusicTextNormalizer.NormalizeGenreLabel(input));
    }
}
