using System.Threading.Channels;

namespace Tunarrly.Infrastructure.Scanning;

public sealed record ScanJobRequest(string? LibraryPath);

public interface IScanJobQueue
{
    ValueTask QueueAsync(ScanJobRequest request, CancellationToken cancellationToken = default);
    ValueTask<ScanJobRequest> DequeueAsync(CancellationToken cancellationToken = default);
}

public interface IScanCancellationCoordinator
{
    CancellationToken BeginScan(CancellationToken stoppingToken);
    void EndScan();
    bool Cancel();
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

public sealed class ScanCancellationCoordinator : IScanCancellationCoordinator
{
    private readonly object _gate = new();
    private CancellationTokenSource? _current;

    public CancellationToken BeginScan(CancellationToken stoppingToken)
    {
        lock (_gate)
        {
            _current?.Dispose();
            _current = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            return _current.Token;
        }
    }

    public void EndScan()
    {
        lock (_gate)
        {
            _current?.Dispose();
            _current = null;
        }
    }

    public bool Cancel()
    {
        lock (_gate)
        {
            if (_current is null) return false;
            _current.Cancel();
            return true;
        }
    }
}
