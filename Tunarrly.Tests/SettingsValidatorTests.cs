using Tunarrly.Core.Options;

namespace Tunarrly.Tests;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void ValidateLidarr_RejectsInvalidUrlAndProfileIds()
    {
        var errors = SettingsValidator.ValidateLidarr(
            new LidarrOptions { BaseUrl = "not-a-url", DefaultQualityProfileId = 0, DefaultMetadataProfileId = -1, DefaultRootFolder = "", DefaultMonitor = "" },
            new LibraryOptions { Path = "" });

        Assert.Contains(errors, x => x.Contains("base URL"));
        Assert.Contains(errors, x => x.Contains("quality profile"));
        Assert.Contains(errors, x => x.Contains("metadata profile"));
        Assert.Contains(errors, x => x.Contains("Music library path"));
    }

    [Fact]
    public void ValidateAi_WhenDisabled_DoesNotRequireProviderUrlOrModel()
    {
        var errors = SettingsValidator.ValidateAi(new AiOptions { Enabled = false, BaseUrl = "", Model = "", TimeoutSeconds = 60, MaxInputArtists = 1, MaxRecommendations = 1 });

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateAi_WhenEnabled_RejectsInvalidValues()
    {
        var errors = SettingsValidator.ValidateAi(new AiOptions { Enabled = true, BaseUrl = "ftp://example.com", Model = "", Temperature = 3, TimeoutSeconds = 1, MaxInputArtists = 0, MaxRecommendations = 0 });

        Assert.Equal(6, errors.Count);
    }
}
