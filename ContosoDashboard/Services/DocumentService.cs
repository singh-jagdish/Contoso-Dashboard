using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentUploadRequest(string Title, string? Description, string Category, string FileName, string ContentType, long Length, int? ProjectId = null, int? TaskId = null);
public sealed record DocumentUploadResult(bool Success, string Message, Document? Document = null);

public class DocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IDocumentScanQueue _queue;
    private readonly DocumentAuthorizationService _authorization;
    private readonly IConfiguration _configuration;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IDocumentScanQueue queue, DocumentAuthorizationService authorization, IConfiguration configuration)
    { _context = context; _storage = storage; _queue = queue; _authorization = authorization; _configuration = configuration; }

    public async Task<DocumentUploadResult> UploadAsync(int userId, DocumentUploadRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var options = _configuration.GetSection("Documents").Get<DocumentOptions>() ?? new();
        if (string.IsNullOrWhiteSpace(request.Title) || !DocumentCategories.All.Contains(request.Category) || request.Length > options.MaxFileSizeBytes)
            return new(false, "The title, category, file type, or file size is invalid.");
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return new(false, "The current user was not found.");
        if (request.ProjectId.HasValue && !await CanUseProjectAsync(request.ProjectId.Value, userId)) return new(false, "You are not allowed to upload to this project.");
        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == request.TaskId.Value, cancellationToken);
            if (task == null || (task.ProjectId.HasValue && !await CanUseProjectAsync(task.ProjectId.Value, userId))) return new(false, "You are not allowed to attach this document to the task.");
        }
        var extension = Path.GetExtension(request.FileName);
        if (!AllowedTypes.ContainsKey(extension) || !AllowedTypes[extension].Contains(request.ContentType, StringComparer.OrdinalIgnoreCase)) return new(false, "This file type is not supported.");
        string? path = null;
        try
        {
            path = await _storage.SaveAsync(content, userId, request.ProjectId, extension, cancellationToken);
            var document = new Document { Title = request.Title.Trim(), Description = request.Description?.Trim(), Category = request.Category, OriginalFileName = Path.GetFileName(request.FileName), FilePath = path, FileType = request.ContentType, FileSize = request.Length, UploadedByUserId = userId, ProjectId = request.ProjectId, TaskId = request.TaskId, ScanRequestedDate = DateTime.UtcNow, ScanStatus = DocumentScanStatus.Pending };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            var job = new DocumentScanJob { DocumentId = document.DocumentId, StoragePath = path };
            _context.DocumentScanJobs.Add(job);
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = document.DocumentId, ActorUserId = userId, Action = "upload" });
            await _context.SaveChangesAsync(cancellationToken);
            await _queue.EnqueueAsync(new DocumentScanRequested(document.DocumentId, job.DocumentScanJobId, DateTime.UtcNow), cancellationToken);
            return new(true, "Upload received and queued for security scanning.", document);
        }
        catch
        {
            if (path != null) await _storage.DeleteAsync(path, cancellationToken);
            return new(false, "The document could not be stored.");
        }
    }

    public async Task<List<Document>> GetUserDocumentsAsync(int userId, string? search = null, string? category = null, int? projectId = null)
    {
        var query = _context.Documents.AsNoTracking().Include(d => d.Project).Include(d => d.Tags).Where(d => d.UploadedByUserId == userId && d.ScanStatus == DocumentScanStatus.Clean);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(d => d.Title.Contains(search) || (d.Description != null && d.Description.Contains(search)) || d.OriginalFileName.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId.Value);
        return await query.OrderByDescending(d => d.UploadedDate).ToListAsync();
    }

    public async Task<Document?> GetForDeliveryAsync(int documentId, int userId)
    {
        var document = await _context.Documents.Include(d => d.Project).ThenInclude(p => p!.ProjectMembers).Include(d => d.Shares).FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document == null) return null;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;
        var isMember = document.Project?.ProjectMembers.Any(m => m.UserId == userId) == true || document.Project?.ProjectManagerId == userId;
        var shared = document.Shares.Any(s => s.RecipientUserId == userId);
        return _authorization.CanRead(document, userId, user.Role, isMember, shared) ? document : null;
    }

    public async Task<List<Document>> GetRecentAsync(int userId, int count = 5) => await _context.Documents.AsNoTracking().Where(d => d.UploadedByUserId == userId && d.ScanStatus == DocumentScanStatus.Clean).OrderByDescending(d => d.UploadedDate).Take(count).ToListAsync();
    public Task<int> GetCountAsync(int userId) => _context.Documents.CountAsync(d => d.UploadedByUserId == userId && d.ScanStatus == DocumentScanStatus.Clean);
    public async Task<List<Document>> GetSharedWithMeAsync(int userId)
        => await _context.Documents.AsNoTracking().Include(d => d.Project).Include(d => d.Shares)
            .Where(d => d.ScanStatus == DocumentScanStatus.Clean && d.Shares.Any(s => s.RecipientUserId == userId))
            .OrderByDescending(d => d.UploadedDate).ToListAsync();
    public async Task<List<DocumentActivity>> GetActivityAsync(int requestingUserId)
    {
        var user = await _context.Users.FindAsync(requestingUserId);
        if (user?.Role != UserRole.Administrator) return new();
        return await _context.DocumentActivities.AsNoTracking().Include(a => a.ActorUser).Include(a => a.Document).OrderByDescending(a => a.CreatedDate).Take(500).ToListAsync();
    }
    public async Task<bool> ShareAsync(int documentId, int ownerId, int recipientUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.UploadedByUserId != ownerId) return false;
        if (await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.RecipientUserId == recipientUserId)) return false;
        _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, SharedByUserId = ownerId, RecipientUserId = recipientUserId });
        await _context.SaveChangesAsync();
        return true;
    }
    private async Task<bool> CanUseProjectAsync(int projectId, int userId) => await _context.Projects.AnyAsync(p => p.ProjectId == projectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)));
    public static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { "application/pdf" }, [".txt"] = new[] { "text/plain" }, [".jpg"] = new[] { "image/jpeg" }, [".jpeg"] = new[] { "image/jpeg" }, [".png"] = new[] { "image/png" },
        [".doc"] = new[] { "application/msword" }, [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }, [".xls"] = new[] { "application/vnd.ms-excel" }, [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }, [".ppt"] = new[] { "application/vnd.ms-powerpoint" }, [".pptx"] = new[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
    };
}
