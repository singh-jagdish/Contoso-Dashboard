using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key] public int DocumentShareId { get; set; }
    [Required] public int DocumentId { get; set; }
    [Required] public int SharedByUserId { get; set; }
    public int? RecipientUserId { get; set; }
    [MaxLength(100)] public string? RecipientTeamKey { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(DocumentId))] public virtual Document Document { get; set; } = null!;
    [ForeignKey(nameof(SharedByUserId))] public virtual User SharedByUser { get; set; } = null!;
    [ForeignKey(nameof(RecipientUserId))] public virtual User? RecipientUser { get; set; }
}
