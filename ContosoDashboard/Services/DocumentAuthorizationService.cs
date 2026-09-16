using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentAuthorizationService
{
    public bool CanRead(Document document, int userId, UserRole role, bool isProjectMember, bool isExplicitRecipient)
        => document.ScanStatus == DocumentScanStatus.Clean &&
           (role == UserRole.Administrator || document.UploadedByUserId == userId || isProjectMember || isExplicitRecipient);

    public bool CanManage(Document document, int userId, UserRole role, bool isProjectManager)
        => role == UserRole.Administrator || document.UploadedByUserId == userId || isProjectManager;
}
