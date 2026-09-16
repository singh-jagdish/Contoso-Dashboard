using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentScanJob
{
    [Key] public int DocumentScanJobId { get; set; }
    [Required] public int DocumentId { get; set; }
    [Required, MaxLength(500)] public string StoragePath { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = DocumentScanJobStatus.Queued;
    public int AttemptCount { get; set; }
    [MaxLength(1000)] public string? FailureReason { get; set; }
    [ForeignKey(nameof(DocumentId))] public virtual Document Document { get; set; } = null!;
}

public static class DocumentScanJobStatus
{
    public const string Queued = "Queued";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Retrying = "Retrying";
    public const string Poisoned = "Poisoned";
}
