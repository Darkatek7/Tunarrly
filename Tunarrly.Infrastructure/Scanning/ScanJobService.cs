using Microsoft.EntityFrameworkCore;
using Tunarrly.Core.Models;
using Tunarrly.Core.Services;
using Tunarrly.Infrastructure.Data;

namespace Tunarrly.Infrastructure.Scanning;

public sealed class ScanJobService(IDbContextFactory<TunarrlyDbContext> dbFactory, IScanJobQueue queue, IScanCancellationCoordinator cancellationCoordinator) : IScanJobService
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

    public async Task<OperationResult> CancelRunningScanAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var job = await db.ScanJobs.OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(x => x.Status == JobStatuses.Running, cancellationToken);
        if (job is null)
        {
            return OperationResult.Fail("No library scan is running.");
        }

        job.ErrorMessage = "Cancellation requested.";
        await db.SaveChangesAsync(cancellationToken);
        return cancellationCoordinator.Cancel()
            ? OperationResult.Ok("Library scan cancellation requested.")
            : OperationResult.Fail("Could not cancel the running scan.");
    }
}
