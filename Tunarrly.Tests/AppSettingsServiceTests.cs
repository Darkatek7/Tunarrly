using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tunarrly.Core.Options;
using Tunarrly.Infrastructure.Data;
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

    [Fact]
    public async Task ClearAllSecretsAsync_BlanksEverySavedSecret()
    {
        var factory = InfrastructureTestHelpers.CreateDbFactory();
        var service = CreateService(factory);
        await service.SaveLidarrOptionsAsync(new LidarrOptions { BaseUrl = "http://lidarr", ApiKey = "lidarr-secret", DefaultRootFolder = "/music", DefaultQualityProfileId = 1, DefaultMetadataProfileId = 1, DefaultMonitor = "all" });
        await service.SaveAiOptionsAsync(new AiOptions { Enabled = true, BaseUrl = "http://ai", ApiKey = "ai-secret", Model = "model", TimeoutSeconds = 60, MaxInputArtists = 10, MaxRecommendations = 5 });

        var cleared = await service.ClearAllSecretsAsync();

        Assert.Equal(2, cleared);
        await using var db = await factory.CreateDbContextAsync();
        Assert.All(await db.AppSettings.Where(x => x.IsSecret).ToListAsync(), setting => Assert.Equal(string.Empty, setting.Value));
    }

    [Fact]
    public async Task SaveAndLoadAiOptions_EncryptsSecretWhenKeyIsConfigured()
    {
        var factory = InfrastructureTestHelpers.CreateDbFactory();
        var service = CreateService(factory, "test-encryption-key-that-is-long-enough");
        await service.SaveAiOptionsAsync(new AiOptions { Enabled = true, BaseUrl = "http://ai", ApiKey = "token", Model = "model", TimeoutSeconds = 60, MaxInputArtists = 10, MaxRecommendations = 5 });

        await using (var db = await factory.CreateDbContextAsync())
        {
            var stored = await db.AppSettings.SingleAsync(x => x.Key == "AI:ApiKey");
            Assert.NotEqual("token", stored.Value);
            Assert.StartsWith("enc:v1:", stored.Value);
        }

        var loaded = await service.GetAiOptionsAsync();
        Assert.Equal("token", loaded.ApiKey);
    }

    [Fact]
    public async Task GetAiOptionsAsync_MigratesPlaintextSecretWhenKeyIsConfigured()
    {
        var factory = InfrastructureTestHelpers.CreateDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.AppSettings.Add(new AppSetting { Key = "AI:ApiKey", Value = "plaintext-token", IsSecret = true });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory, "test-encryption-key-that-is-long-enough");
        var loaded = await service.GetAiOptionsAsync();

        Assert.Equal("plaintext-token", loaded.ApiKey);
        await using var verify = await factory.CreateDbContextAsync();
        var stored = await verify.AppSettings.SingleAsync(x => x.Key == "AI:ApiKey");
        Assert.StartsWith("enc:v1:", stored.Value);
    }

    private static AppSettingsService CreateService() => new(
        InfrastructureTestHelpers.CreateDbFactory(),
        InfrastructureTestHelpers.Options(new LidarrOptions()),
        InfrastructureTestHelpers.Options(new LibraryOptions()),
        InfrastructureTestHelpers.Options(new AiOptions()),
        new SecretProtector(Options.Create(new SecretsOptions())));

    private static AppSettingsService CreateService(IDbContextFactory<TunarrlyDbContext> dbFactory) => new(
        dbFactory,
        InfrastructureTestHelpers.Options(new LidarrOptions()),
        InfrastructureTestHelpers.Options(new LibraryOptions()),
        InfrastructureTestHelpers.Options(new AiOptions()),
        new SecretProtector(Options.Create(new SecretsOptions())));

    private static AppSettingsService CreateService(IDbContextFactory<TunarrlyDbContext> dbFactory, string encryptionKey) => new(
        dbFactory,
        InfrastructureTestHelpers.Options(new LidarrOptions()),
        InfrastructureTestHelpers.Options(new LibraryOptions()),
        InfrastructureTestHelpers.Options(new AiOptions()),
        new SecretProtector(Options.Create(new SecretsOptions { EncryptionKey = encryptionKey })));
}
