using System.Threading.Channels;

namespace Tunarrly.Infrastructure.Scanning;

public sealed record ScanJobRequest(string? LibraryPath);

public interface IScanJobQueue
{
    ValueTask QueueAsync(ScanJobRequest request, CancellationToken cancellationToken = default);
    ValueTask<ScanJobRequest> DequeueAsync(CancellationToken cancellationToken = default);
}

public sealed class ScanJobQueue : IScanJobQueue
{
    private readonly Channel<ScanJobRequest> _queue = Channel.CreateUnbounded<ScanJobRequest>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask QueueAsync(ScanJobRequest request, CancellationToken cancellationToken = default)
        => _queue.Writer.WriteAsync(request, cancellationToken);

    public ValueTask<ScanJobRequest> DequeueAsync(CancellationToken cancellationToken = default)
        => _queue.Reader.ReadAsync(cancellationToken);
}
