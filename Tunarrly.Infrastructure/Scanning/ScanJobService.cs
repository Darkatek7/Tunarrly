using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Scanning;

public sealed class ScanJobService(IDbContextFactory<TunarrlyDbContext> dbFactory, IScanJobQueue queue) : IScanJobService
{
    public async Task<OperationResult> EnqueueScanAsync(string? libraryPath = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ScanJobs.AnyAsync(x => x.Status == JobStatuses.Running, cancellationToken))
        {
            return OperationResult.Fail("A library scan is already running.");
        }

        await queue.QueueAsync(new ScanJobRequest(libraryPath), cancellationToken);
        return OperationResult.Ok("Library scan queued.");
    }
}
