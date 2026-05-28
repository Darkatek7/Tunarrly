using Tunarrly.Core.Options;
using Tunarrly.Infrastructure.Settings;

namespace Tunarrly.Tests;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoadLidarrOptions_PreservesBlankSecretAsExistingValue()
    {
        var service = CreateService();
        await service.SaveLidarrOptionsAsync(new LidarrOptions { BaseUrl = "http://lidarr", ApiKey = "secret", DefaultRootFolder = "/music", DefaultQualityProfileId = 2, DefaultMetadataProfileId = 3, DefaultMonitor = "all", SearchOnAdd = true });
        await service.SaveLidarrOptionsAsync(new LidarrOptions { BaseUrl = "http://lidarr2", ApiKey = "", DefaultRootFolder = "/music2", DefaultQualityProfileId = 4, DefaultMetadataProfileId = 5, DefaultMonitor = "none" });

        var loaded = await service.GetLidarrOptionsAsync();

        Assert.Equal("http://lidarr2", loaded.BaseUrl);
        Assert.Equal("secret", loaded.ApiKey);
        Assert.Equal(4, loaded.DefaultQualityProfileId);
    }

    [Fact]
    public async Task ClearSecretAsync_RemovesSavedSecretValue()
    {
        var service = CreateService();
        await service.SaveAiOptionsAsync(new AiOptions { Enabled = true, BaseUrl = "http://ai", ApiKey = "token", Model = "model", TimeoutSeconds = 60, MaxInputArtists = 10, MaxRecommendations = 5 });

        await service.ClearSecretAsync("AI:ApiKey");

        var loaded = await service.GetAiOptionsAsync();
        Assert.Empty(loaded.ApiKey);
    }

    private static AppSettingsService CreateService() => new(
        InfrastructureTestHelpers.CreateDbFactory(),
        InfrastructureTestHelpers.Options(new LidarrOptions()),
        InfrastructureTestHelpers.Options(new LibraryOptions()),
        InfrastructureTestHelpers.Options(new AiOptions()));
}
