using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class LocalDocumentScanWorker : BackgroundService
{
    private readonly IDocumentScanQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocalDocumentScanWorker> _logger;
    public LocalDocumentScanWorker(IDocumentScanQueue queue, IServiceScopeFactory scopeFactory, ILogger<LocalDocumentScanWorker> logger) { _queue = queue; _scopeFactory = scopeFactory; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try { await ProcessAsync(message, stoppingToken); } catch (Exception ex) { _logger.LogError(ex, "Document scan job {JobId} failed", message.ScanJobId); }
        }
    }
    private async Task ProcessAsync(DocumentScanRequested message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var scanner = scope.ServiceProvider.GetRequiredService<IMalwareScanner>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var document = await db.Documents.FirstOrDefaultAsync(d => d.DocumentId == message.DocumentId, cancellationToken);
        var job = await db.DocumentScanJobs.FirstOrDefaultAsync(j => j.DocumentScanJobId == message.ScanJobId, cancellationToken);
        if (document == null || job == null || document.ScanStatus is DocumentScanStatus.Clean or DocumentScanStatus.Rejected) return;
        document.ScanStatus = DocumentScanStatus.Scanning; document.ScanAttemptCount++; job.Status = DocumentScanJobStatus.Processing; job.AttemptCount++; job.StartedDate = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken);
        await using var stream = await storage.OpenReadAsync(document.FilePath, cancellationToken) ?? throw new FileNotFoundException();
        var result = await scanner.ScanAsync(stream, document.OriginalFileName, cancellationToken);
        document.ScanCompletedDate = DateTime.UtcNow; document.ScanStatus = result.IsClean ? DocumentScanStatus.Clean : DocumentScanStatus.Rejected; document.ScanError = result.Reason; job.Status = DocumentScanJobStatus.Completed; job.CompletedDate = DateTime.UtcNow; job.FailureReason = result.Reason;
        db.DocumentActivities.Add(new DocumentActivity { DocumentId = document.DocumentId, ActorUserId = document.UploadedByUserId, Action = result.IsClean ? "scan-clean" : "scan-rejected", Details = result.Reason });
        await db.SaveChangesAsync(cancellationToken);
        await notifications.CreateNotificationAsync(new Notification
        {
            UserId = document.UploadedByUserId,
            Title = result.IsClean ? "Document scan complete" : "Document scan rejected",
            Message = result.IsClean ? $"'{document.Title}' is ready to use." : $"'{document.Title}' was rejected by the security scan.",
            Type = result.IsClean ? NotificationType.DocumentScanCompleted : NotificationType.DocumentScanRejected,
            Priority = result.IsClean ? NotificationPriority.Informational : NotificationPriority.Important
        });
    }
}
