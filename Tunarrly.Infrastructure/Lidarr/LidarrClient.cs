using System.Net.Http.Json;
using System.Text.Json;
using Tunarrly.Core.Models;
using Tunarrly.Core.Services;

namespace Tunarrly.Infrastructure.Lidarr;

public sealed class LidarrClient(HttpClient httpClient, IAppSettingsService settings) : ILidarrClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var client = await ConfigureClientAsync(cancellationToken);
        if (client is null) return OperationResult.Fail("Lidarr base URL and API key are required.");

        try
        {
            using var status = await httpClient.GetAsync("api/v1/system/status", cancellationToken);
            if (!status.IsSuccessStatusCode) return OperationResult.Fail($"System status failed: {(int)status.StatusCode}.");
            using var artists = await httpClient.GetAsync("api/v1/artist", cancellationToken);
            if (!artists.IsSuccessStatusCode) return OperationResult.Fail($"Artist endpoint failed: {(int)artists.StatusCode}.");
            return OperationResult.Ok("Lidarr connection succeeded.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return OperationResult.Fail($"Lidarr connection failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<LidarrArtistDto>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfigureClientAsync(cancellationToken) is null) return Array.Empty<LidarrArtistDto>();
        var artists = await GetJsonOrEmptyAsync<LidarrArtistResponse>("api/v1/artist", cancellationToken);
        return artists.Select(x => new LidarrArtistDto(x.Id, x.ArtistName ?? string.Empty, x.ForeignArtistId, x.MusicBrainzId, x.Monitored, x.Path, x.QualityProfileId, x.MetadataProfileId)).ToArray();
    }

    public async Task<IReadOnlyList<LidarrQualityProfileDto>> GetQualityProfilesAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfigureClientAsync(cancellationToken) is null) return [];
        var profiles = await GetJsonOrEmptyAsync<NamedIdResponse>("api/v1/qualityprofile", cancellationToken);
        return profiles.Select(x => new LidarrQualityProfileDto(x.Id, x.Name ?? $"Profile {x.Id}")).ToArray();
    }

    public async Task<IReadOnlyList<LidarrMetadataProfileDto>> GetMetadataProfilesAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfigureClientAsync(cancellationToken) is null) return [];
        var profiles = await GetJsonOrEmptyAsync<NamedIdResponse>("api/v1/metadataprofile", cancellationToken);
        return profiles.Select(x => new LidarrMetadataProfileDto(x.Id, x.Name ?? $"Profile {x.Id}")).ToArray();
    }

    public async Task<IReadOnlyList<LidarrRootFolderDto>> GetRootFoldersAsync(CancellationToken cancellationToken = default)
    {
        if (await ConfigureClientAsync(cancellationToken) is null) return [];
        var folders = await GetJsonOrEmptyAsync<RootFolderResponse>("api/v1/rootfolder", cancellationToken);
        return folders.Select(x => new LidarrRootFolderDto(x.Id, x.Path ?? string.Empty, x.FreeSpace)).Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToArray();
    }

    public async Task<IReadOnlyList<LidarrLookupResult>> SearchArtistAsync(string artistName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artistName) || await ConfigureClientAsync(cancellationToken) is null) return Array.Empty<LidarrLookupResult>();
        var url = $"api/v1/artist/lookup?term={Uri.EscapeDataString(artistName)}";
        var results = await GetJsonOrEmptyAsync<LidarrLookupResponse>(url, cancellationToken);
        return results.Select(x => new LidarrLookupResult(x.ArtistName ?? string.Empty, x.ForeignArtistId, x.MusicBrainzId, x.Overview, x.Disambiguation)).ToArray();
    }

    public async Task<OperationResult> AddArtistAsync(LidarrLookupResult artist, CancellationToken cancellationToken = default)
    {
        var options = await ConfigureClientAsync(cancellationToken);
        if (options is null) return OperationResult.Fail("Lidarr is not configured.");
        if (string.IsNullOrWhiteSpace(artist.ForeignArtistId)) return OperationResult.Fail("Selected Lidarr match does not include a foreign artist id.");

        var payload = new
        {
            artistName = artist.ArtistName,
            foreignArtistId = artist.ForeignArtistId,
            monitored = true,
            rootFolderPath = options.DefaultRootFolder,
            qualityProfileId = options.DefaultQualityProfileId,
            metadataProfileId = options.DefaultMetadataProfileId,
            addOptions = new { monitor = options.DefaultMonitor, searchForMissingAlbums = options.SearchOnAdd }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/v1/artist", payload, JsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return OperationResult.Ok($"Added {artist.ArtistName} to Lidarr.");
            }

            return response.StatusCode == System.Net.HttpStatusCode.Conflict
                ? OperationResult.Fail($"{artist.ArtistName} already appears to exist in Lidarr.")
                : OperationResult.Fail($"Lidarr add failed: {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return OperationResult.Fail($"Lidarr add failed: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<T>> GetJsonOrEmptyAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<T>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return [];
        }
    }

    private async Task<Core.Options.LidarrOptions?> ConfigureClientAsync(CancellationToken cancellationToken)
    {
        var options = await settings.GetLidarrOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiKey)) return null;
        httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        return options;
    }

    private sealed record LidarrArtistResponse(int Id, string? ArtistName, string? ForeignArtistId, string? MusicBrainzId, bool Monitored, string? Path, int? QualityProfileId, int? MetadataProfileId);
    private sealed record LidarrLookupResponse(string? ArtistName, string? ForeignArtistId, string? MusicBrainzId, string? Overview, string? Disambiguation);
    private sealed record NamedIdResponse(int Id, string? Name);
    private sealed record RootFolderResponse(int Id, string? Path, long? FreeSpace);
}
