using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tunarrly.Core.Services;

namespace Tunarrly.Infrastructure.Scanning;

public sealed class ScanWorker(IScanJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<ScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScanJobRequest request;
            try
            {
                request = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<ILibraryScanner>();
                await scanner.ScanAsync(request.LibraryPath, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queued library scan failed.");
            }
        }
    }
}
