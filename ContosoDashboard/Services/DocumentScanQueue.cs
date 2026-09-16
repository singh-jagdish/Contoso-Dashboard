namespace ContosoDashboard.Services;

public sealed record DocumentScanRequested(int DocumentId, int ScanJobId, DateTime RequestedAtUtc);

public interface IDocumentScanQueue
{
    ValueTask EnqueueAsync(DocumentScanRequested message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<DocumentScanRequested> ReadAllAsync(CancellationToken cancellationToken = default);
}
