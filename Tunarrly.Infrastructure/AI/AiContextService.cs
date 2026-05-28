using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.AI;

public sealed class AiContextService(IDbContextFactory<TunarrlyDbContext> dbFactory) : IAiContextService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly string[] Included =
    [
        "Monitored Lidarr artist names",
        "Ignored recommendation artist names",
        "Top tag-derived library artist credit counts",
        "Frequent tag-derived genres",
        "Featured artist credit counts",
        "Local recommendation names, scores, and reasons"
    ];
    private static readonly string[] Excluded =
    [
        "Local file paths and folder structure",
        "Lidarr API keys",
        "AI provider tokens",
        "SQLite database path",
        "Raw audio files or embedded artwork",
        "User environment variables"
    ];

    public async Task<AiContextPreview> BuildPreviewAsync(int maxArtists, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var context = new
        {
            monitoredLidarrArtists = await db.LidarrArtists.OrderBy(x => x.Name).Take(maxArtists).Select(x => x.Name).ToListAsync(cancellationToken),
            ignoredArtists = await db.Recommendations.Where(x => x.Status == RecommendationStatuses.Ignored).Select(x => x.ArtistName).ToListAsync(cancellationToken),
            topLibraryArtists = await db.TrackArtistCredits.GroupBy(x => x.ArtistName).OrderByDescending(x => x.Count()).Take(maxArtists).Select(x => new { name = x.Key, count = x.Count() }).ToListAsync(cancellationToken),
            frequentGenres = await db.LibraryTracks.Where(x => x.Genre != null).GroupBy(x => x.Genre!).OrderByDescending(x => x.Count()).Take(50).Select(x => new { name = x.Key, count = x.Count() }).ToListAsync(cancellationToken),
            featuredArtists = await db.TrackArtistCredits.Where(x => x.CreditType == CreditTypes.Featured).GroupBy(x => x.ArtistName).OrderByDescending(x => x.Count()).Take(100).Select(x => new { name = x.Key, count = x.Count() }).ToListAsync(cancellationToken),
            localRecommendations = await db.Recommendations.Where(x => x.Source != RecommendationSources.Ai).OrderByDescending(x => x.Score).Take(50).Select(x => new { x.ArtistName, x.Score, x.ReasonJson }).ToListAsync(cancellationToken)
        };

        var payload = JsonSerializer.Serialize(context, JsonOptions);
        return new AiContextPreview(payload, Encoding.UTF8.GetByteCount(payload), Included, Excluded);
    }
}
