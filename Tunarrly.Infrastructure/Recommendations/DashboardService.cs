using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Recommendations;

public sealed class DashboardService(IDbContextFactory<TunarrlyDbContext> dbFactory, IAppSettingsService settings) : IDashboardService
{
    public async Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var lidarr = await settings.GetLidarrOptionsAsync(cancellationToken);
        var ai = await settings.GetAiOptionsAsync(cancellationToken);
        var scanJobs = await db.ScanJobs.ToListAsync(cancellationToken);
        var aiRuns = await db.AiRecommendationRuns.ToListAsync(cancellationToken);
        var recommendationTimes = await db.Recommendations.Select(x => x.LastCalculatedAt).ToListAsync(cancellationToken);
        var lastScan = scanJobs.OrderByDescending(x => x.StartedAt).FirstOrDefault();
        var lastAiRun = aiRuns.OrderByDescending(x => x.StartedAt).FirstOrDefault();
        return new DashboardStats(
            !string.IsNullOrWhiteSpace(lidarr.BaseUrl) && !string.IsNullOrWhiteSpace(lidarr.ApiKey),
            !string.IsNullOrWhiteSpace(lidarr.DefaultRootFolder)
                && lidarr.DefaultQualityProfileId > 0
                && lidarr.DefaultMetadataProfileId > 0,
            ai.Enabled,
            await db.LidarrArtists.CountAsync(cancellationToken),
            await db.LibraryArtists.CountAsync(cancellationToken),
            await db.LibraryTracks.CountAsync(cancellationToken),
            await db.Recommendations.CountAsync(x => x.Status == RecommendationStatuses.New, cancellationToken),
            await db.Recommendations.CountAsync(x => x.Source == RecommendationSources.Ai || x.Source == RecommendationSources.Hybrid, cancellationToken),
            lastScan?.FinishedAt ?? lastScan?.StartedAt,
            (await db.LidarrArtists.ToListAsync(cancellationToken)).OrderByDescending(x => x.LastSyncedAt).Select(x => (DateTimeOffset?)x.LastSyncedAt).FirstOrDefault(),
            lastAiRun?.StartedAt,
            recommendationTimes.OrderByDescending(x => x).Select(x => (DateTimeOffset?)x).FirstOrDefault(),
            lastScan?.Status,
            lastScan?.FilesScanned ?? 0,
            lastScan?.FilesFailed ?? 0,
            lastAiRun?.Status);
    }
}
