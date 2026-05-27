using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Recommendations;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.AI;

public sealed class AiProviderClient(HttpClient httpClient, IAppSettingsService settings, IDbContextFactory<TunarrlyDbContext> dbFactory) : IAiProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var options = await settings.GetAiOptionsAsync(cancellationToken);
        if (!options.Enabled) return OperationResult.Fail("AI recommendations are disabled.");
        if (!ConfigureClient(options, out var error)) return OperationResult.Fail(error);

        var payload = new ChatCompletionRequest(options.Model, [new ChatMessage("user", "Return only this JSON: {\"ok\":true}")], options.Temperature);
        try
        {
            using var response = await httpClient.PostAsJsonAsync("chat/completions", payload, JsonOptions, cancellationToken);
            return response.IsSuccessStatusCode
                ? OperationResult.Ok("AI provider connection succeeded.")
                : OperationResult.Fail($"AI provider returned {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return OperationResult.Fail($"AI provider test failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var options = await settings.GetAiOptionsAsync(cancellationToken);
        if (!options.Enabled || !ConfigureClient(options, out _)) return Array.Empty<RecommendationCandidate>();

        var context = await BuildContextAsync(options.MaxInputArtists, cancellationToken);
        var messages = new[]
        {
            new ChatMessage("system", "You are a music discovery assistant for a self-hosted Lidarr companion app. Recommend artists the user may want to add to Lidarr. Use only the provided music profile. Avoid artists already monitored in Lidarr. Avoid ignored artists. Prefer explainable recommendations based on genres, collaborations, featured artists, and library patterns. Return strict JSON only."),
            new ChatMessage("user", JsonSerializer.Serialize(context, JsonOptions))
        };
        var payload = new ChatCompletionRequest(options.Model, messages, options.Temperature, new { type = "json_object" });
        using var response = await httpClient.PostAsJsonAsync("chat/completions", payload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<RecommendationCandidate>();

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
        var content = completion?.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<RecommendationCandidate>();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.LidarrArtists.Select(x => x.NormalizedName).ToListAsync(cancellationToken);
        var ignored = await db.Recommendations.Where(x => x.Status == RecommendationStatuses.Ignored).Select(x => x.NormalizedArtistName).ToListAsync(cancellationToken);
        return AiRecommendationValidator.ParseAndValidate(content, existing, ignored, options.MaxRecommendations);
    }

    private bool ConfigureClient(Core.Options.AiOptions options, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.Model))
        {
            error = "AI base URL and model are required.";
            return false;
        }

        httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(options.ApiKey) ? null : new AuthenticationHeaderValue("Bearer", options.ApiKey);
        return true;
    }

    private async Task<object> BuildContextAsync(int maxArtists, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var lidarr = await db.LidarrArtists.OrderBy(x => x.Name).Take(maxArtists).Select(x => x.Name).ToListAsync(cancellationToken);
        var ignored = await db.Recommendations.Where(x => x.Status == RecommendationStatuses.Ignored).Select(x => x.ArtistName).ToListAsync(cancellationToken);
        var local = await db.Recommendations.Where(x => x.Source != RecommendationSources.Ai).OrderByDescending(x => x.Score).Take(50).Select(x => new { x.ArtistName, x.Score, x.ReasonJson }).ToListAsync(cancellationToken);
        var topArtists = await db.TrackArtistCredits.GroupBy(x => x.ArtistName).OrderByDescending(x => x.Count()).Take(maxArtists).Select(x => new { Name = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
        var genres = await db.LibraryTracks.Where(x => x.Genre != null).GroupBy(x => x.Genre!).OrderByDescending(x => x.Count()).Take(50).Select(x => new { Name = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
        var featured = await db.TrackArtistCredits.Where(x => x.CreditType == CreditTypes.Featured).GroupBy(x => x.ArtistName).OrderByDescending(x => x.Count()).Take(100).Select(x => new { Name = x.Key, Count = x.Count() }).ToListAsync(cancellationToken);
        return new { monitoredLidarrArtists = lidarr, ignoredArtists = ignored, topLibraryArtists = topArtists, frequentGenres = genres, featuredArtists = featured, localRecommendations = local };
    }

    private sealed record ChatCompletionRequest(string Model, IReadOnlyList<ChatMessage> Messages, double Temperature, object? ResponseFormat = null);
    private sealed record ChatMessage(string Role, string Content);
    private sealed record ChatCompletionResponse(IReadOnlyList<Choice> Choices);
    private sealed record Choice(ChatMessage Message);
}
