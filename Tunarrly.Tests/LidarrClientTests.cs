using System.Net;
using System.Text;
using Tunarrly.Core.Models;
using Tunarrly.Core.Options;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Lidarr;

namespace Tunarrly.Tests;

public sealed class LidarrClientTests
{
    [Fact]
    public async Task GetArtistsAsync_SendsApiKeyHeaderAndParsesArtists()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Equal("secret-key", request.Headers.GetValues("X-Api-Key").Single());
            Assert.Equal("http://lidarr:8686/api/v1/artist", request.RequestUri?.ToString());
            return Json("[{\"id\":7,\"artistName\":\"Burial\",\"foreignArtistId\":\"mbid\",\"monitored\":true,\"qualityProfileId\":1,\"metadataProfileId\":1}]");
        });

        var client = new LidarrClient(new HttpClient(handler), new FakeSettings());
        var artist = Assert.Single(await client.GetArtistsAsync());

        Assert.Equal(7, artist.Id);
        Assert.Equal("Burial", artist.ArtistName);
        Assert.True(artist.Monitored);
    }

    [Fact]
    public async Task GetMetadataProfilesAsync_ReturnsEmptyWhenEndpointUnavailable()
    {
        var client = new LidarrClient(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))), new FakeSettings());

        Assert.Empty(await client.GetMetadataProfilesAsync());
    }

    [Fact]
    public async Task AddArtistAsync_ReportsDuplicateConflictWithoutThrowing()
    {
        var client = new LidarrClient(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict))), new FakeSettings());

        var result = await client.AddArtistAsync(new("Burial", "foreign-id", null, null, null));

        Assert.False(result.Success);
        Assert.Contains("already", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddArtistAsync_PostsConfiguredDefaults()
    {
        string? body = null;
        var client = new LidarrClient(new HttpClient(new AsyncStubHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created);
        })), new FakeSettings());

        var result = await client.AddArtistAsync(new("Burial", "foreign-id", null, null, null));

        Assert.True(result.Success);
        Assert.Contains("\"rootFolderPath\":\"/music\"", body);
        Assert.Contains("\"qualityProfileId\":1", body);
        Assert.Contains("\"metadataProfileId\":1", body);
        Assert.Contains("\"searchForMissingAlbums\":false", body);
        Assert.DoesNotContain("secret-key", body);
    }

    [Fact]
    public async Task AddArtistAsync_UsesPerAddOptions()
    {
        string? body = null;
        var client = new LidarrClient(new HttpClient(new AsyncStubHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created);
        })), new FakeSettings());

        await client.AddArtistAsync(new("Burial", "foreign-id", null, null, null), new LidarrAddOptions("/custom", 9, 8, "future", true, false));

        Assert.Contains("\"rootFolderPath\":\"/custom\"", body);
        Assert.Contains("\"qualityProfileId\":9", body);
        Assert.Contains("\"metadataProfileId\":8", body);
        Assert.Contains("\"monitored\":false", body);
        Assert.Contains("\"monitor\":\"future\"", body);
        Assert.Contains("\"searchForMissingAlbums\":true", body);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    private sealed class AsyncStubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request);
    }

    private sealed class FakeSettings : IAppSettingsService
    {
        public Task<LidarrOptions> GetLidarrOptionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrOptions { BaseUrl = "http://lidarr:8686", ApiKey = "secret-key", DefaultRootFolder = "/music", DefaultQualityProfileId = 1, DefaultMetadataProfileId = 1, DefaultMonitor = "all" });

        public Task<AiOptions> GetAiOptionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AiOptions());
        public Task<LibraryOptions> GetLibraryOptionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryOptions());
        public Task SaveLidarrOptionsAsync(LidarrOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAiOptionsAsync(AiOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveLibraryOptionsAsync(LibraryOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearSecretAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> ClearAllSecretsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
