using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tunarrly.Core.Options;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Settings;

public sealed class AppSettingsService(
    IDbContextFactory<TunarrlyDbContext> dbFactory,
    IOptions<LidarrOptions> lidarrDefaults,
    IOptions<LibraryOptions> libraryDefaults,
    IOptions<AiOptions> aiDefaults) : IAppSettingsService
{
    public async Task<LidarrOptions> GetLidarrOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = lidarrDefaults.Value;
        var values = await GetValuesAsync("Lidarr:", cancellationToken);
        return new LidarrOptions
        {
            BaseUrl = Get(values, "Lidarr:BaseUrl", options.BaseUrl),
            ApiKey = Get(values, "Lidarr:ApiKey", options.ApiKey),
            DefaultRootFolder = Get(values, "Lidarr:DefaultRootFolder", options.DefaultRootFolder),
            DefaultQualityProfileId = GetInt(values, "Lidarr:DefaultQualityProfileId", options.DefaultQualityProfileId),
            DefaultMetadataProfileId = GetInt(values, "Lidarr:DefaultMetadataProfileId", options.DefaultMetadataProfileId),
            DefaultMonitor = Get(values, "Lidarr:DefaultMonitor", options.DefaultMonitor),
            SearchOnAdd = GetBool(values, "Lidarr:SearchOnAdd", options.SearchOnAdd)
        };
    }

    public async Task<AiOptions> GetAiOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = aiDefaults.Value;
        var values = await GetValuesAsync("AI:", cancellationToken);
        return new AiOptions
        {
            Enabled = GetBool(values, "AI:Enabled", options.Enabled),
            BaseUrl = Get(values, "AI:BaseUrl", options.BaseUrl),
            ApiKey = Get(values, "AI:ApiKey", options.ApiKey),
            Model = Get(values, "AI:Model", options.Model),
            Temperature = GetDouble(values, "AI:Temperature", options.Temperature),
            TimeoutSeconds = GetInt(values, "AI:TimeoutSeconds", options.TimeoutSeconds),
            MaxInputArtists = GetInt(values, "AI:MaxInputArtists", options.MaxInputArtists),
            MaxRecommendations = GetInt(values, "AI:MaxRecommendations", options.MaxRecommendations)
        };
    }

    public async Task<LibraryOptions> GetLibraryOptionsAsync(CancellationToken cancellationToken = default)
    {
        var options = libraryDefaults.Value;
        var values = await GetValuesAsync("Library:", cancellationToken);
        return new LibraryOptions { Path = Get(values, "Library:Path", options.Path) };
    }

    public async Task SaveLidarrOptionsAsync(LidarrOptions options, CancellationToken cancellationToken = default)
    {
        await SaveValuesAsync(new Dictionary<string, (string Value, bool IsSecret)>
        {
            ["Lidarr:BaseUrl"] = (options.BaseUrl, false),
            ["Lidarr:ApiKey"] = (options.ApiKey, true),
            ["Lidarr:DefaultRootFolder"] = (options.DefaultRootFolder, false),
            ["Lidarr:DefaultQualityProfileId"] = (options.DefaultQualityProfileId.ToString(), false),
            ["Lidarr:DefaultMetadataProfileId"] = (options.DefaultMetadataProfileId.ToString(), false),
            ["Lidarr:DefaultMonitor"] = (options.DefaultMonitor, false),
            ["Lidarr:SearchOnAdd"] = (options.SearchOnAdd.ToString(), false)
        }, cancellationToken);
    }

    public async Task SaveAiOptionsAsync(AiOptions options, CancellationToken cancellationToken = default)
    {
        await SaveValuesAsync(new Dictionary<string, (string Value, bool IsSecret)>
        {
            ["AI:Enabled"] = (options.Enabled.ToString(), false),
            ["AI:BaseUrl"] = (options.BaseUrl, false),
            ["AI:ApiKey"] = (options.ApiKey, true),
            ["AI:Model"] = (options.Model, false),
            ["AI:Temperature"] = (options.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture), false),
            ["AI:TimeoutSeconds"] = (options.TimeoutSeconds.ToString(), false),
            ["AI:MaxInputArtists"] = (options.MaxInputArtists.ToString(), false),
            ["AI:MaxRecommendations"] = (options.MaxRecommendations.ToString(), false)
        }, cancellationToken);
    }

    public Task SaveLibraryOptionsAsync(LibraryOptions options, CancellationToken cancellationToken = default)
        => SaveValuesAsync(new Dictionary<string, (string Value, bool IsSecret)> { ["Library:Path"] = (options.Path, false) }, cancellationToken);

    private async Task<Dictionary<string, string>> GetValuesAsync(string prefix, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.AppSettings
            .Where(x => x.Key.StartsWith(prefix))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
    }

    private async Task SaveValuesAsync(Dictionary<string, (string Value, bool IsSecret)> values, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        foreach (var (key, value) in values)
        {
            var existing = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
            if (existing is null)
            {
                db.AppSettings.Add(new AppSetting { Key = key, Value = value.Value, IsSecret = value.IsSecret });
                continue;
            }

            if (value.IsSecret && string.IsNullOrEmpty(value.Value))
            {
                continue;
            }

            existing.Value = value.Value;
            existing.IsSecret = value.IsSecret;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static bool GetBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
        => values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;

    private static double GetDouble(IReadOnlyDictionary<string, string> values, string key, double fallback)
        => values.TryGetValue(key, out var value) && double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
}
