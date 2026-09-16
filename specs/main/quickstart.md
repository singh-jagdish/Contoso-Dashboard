# Quickstart: Document Upload and Management

## Prerequisites

- Windows with the .NET 10 SDK available.
- SQL Server LocalDB or the configured local SQL Server connection used by `ContosoDashboard`.
- The repository's existing seeded training users and cookie login flow.

## Build and run

```powershell
dotnet build .\ContosoDashboard\ContosoDashboard.csproj
dotnet run --project .\ContosoDashboard\ContosoDashboard.csproj
```

Open the HTTPS URL printed by the application and sign in using the existing seeded training accounts.

## Validation scenarios

1. Upload one allowed file no larger than 25 MB with a title and approved category. Confirm progress, success feedback, metadata, and a private stored file outside `wwwroot`.
2. Confirm a successful upload initially shows `Pending`/`Scanning`, is not downloadable or searchable, then becomes available only after the local background worker records a clean scan.
3. Attempt an oversized file, an unsupported extension/content type, and a deterministic scanner-rejected file. Confirm clear failure feedback and no document list entry or accessible file.
4. Force a transient scan failure and confirm retry/backoff; exhaust attempts and confirm the message is isolated in the poison/dead-letter path and the document remains inaccessible.
5. In a production-like configuration, place a scan message in Azure Queue Storage and confirm an Azure Functions Queue Storage trigger invokes the same handler and updates the document status.
6. Upload a project document as a project member. Confirm project members can list/download it after a clean result and an unrelated user cannot see, search, preview, or download it.
7. Download and preview an authorized PDF/image. Confirm the protected delivery path rechecks authorization; record the activity as an administrator.
8. Edit metadata, replace the file, and delete after confirmation. Confirm search/list removal and private-file deletion after deletion.
9. Share a document with an internal user/team. Confirm the recipient notification and Shared with Me result; confirm public/anonymous access is unavailable.
10. Attach or upload from a task and confirm the document follows the task's project association.
11. Open the dashboard and confirm five recent uploads and the document count; add a project document and confirm relevant in-app notifications.
12. As an administrator, review activity and reports for uploads, scans, downloads, deletions, shares, file types, uploaders, and access patterns. Confirm non-administrators are denied.

## Performance checks

- Measure a representative 25 MB upload and confirm completion within 30 seconds under typical local/network conditions.
- Seed or create up to 500 authorized documents and confirm list and search responses within 2 seconds.
- Confirm supported preview display within 3 seconds.

## Security checks

- Try a document ID belonging to another user through the protected file route; expect `404` or an equivalent non-disclosing denial.
- Try path traversal and user-controlled filename values; confirm the storage path remains generated and private.
- Confirm a failed upload cannot leave an accessible metadata record or orphaned private file.
- Confirm administrator-only reports and activity data are not available to ordinary users.

## Focused regression checklist

Run these checks after each document-service change. They are the repository's current focused regression suite because the project does not yet contain an automated test project:

- Upload a clean PDF as a seeded employee; verify a `Pending` or `Scanning` state is never downloadable, then verify the local worker changes it to `Clean` and the owner receives a notification.
- Upload a filename containing `virus` or `eicar`; verify it becomes `Rejected`, has no protected download response, and does not appear in clean search results.
- Request `/documents/file/{id}` as an unrelated authenticated user; verify a non-disclosing `404` and no download activity record.
- Submit a rooted or traversal path to `LocalFileStorageService`; verify it throws and never writes outside `AppData/uploads`.
- Deliver the same scan message twice; verify a terminal clean/rejected document is not processed twice.
- Verify `/documents/admin` is available to an Administrator and denied to Employee, TeamLead, and ProjectManager roles.
- Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj --no-restore` and record any failure before release.
