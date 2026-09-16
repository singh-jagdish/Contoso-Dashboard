using System.Threading.Channels;

namespace ContosoDashboard.Services;

public sealed class LocalDocumentScanQueue : IDocumentScanQueue
{
    private readonly Channel<DocumentScanRequested> _channel;
    public LocalDocumentScanQueue(IConfiguration configuration)
    {
        var capacity = configuration.GetSection("Documents").Get<DocumentOptions>()?.ScanQueueCapacity ?? 100;
        _channel = Channel.CreateBounded<DocumentScanRequested>(new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    }
    public ValueTask EnqueueAsync(DocumentScanRequested message, CancellationToken cancellationToken = default) => _channel.Writer.WriteAsync(message, cancellationToken);
    public IAsyncEnumerable<DocumentScanRequested> ReadAllAsync(CancellationToken cancellationToken = default) => _channel.Reader.ReadAllAsync(cancellationToken);
}
