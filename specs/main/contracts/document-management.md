# Document Management Contract

## Protected UI routes

- `/documents`: authenticated user's document list, filters, sort, search, upload entry point, and authorized actions.
- `/documents/shared`: authenticated user's Shared with Me list.
- `/documents/admin`: administrator-only activity and report view.
- Existing `/projects/{projectId}`: authorized project members see project documents; project managers can upload/manage project documents.
- Existing task detail route: authorized task viewers see related documents and can attach/upload when permitted.

A route must never infer access from a client-supplied user ID. The authenticated claim identity is passed to the service layer.

Document list/search/download/preview operations include only documents with `ScanStatus=Clean`. Owners may see `Pending`, `Scanning`, `Rejected`, or `Failed` status messages without receiving file content; administrators may inspect scan operational state.

## Protected file delivery

`GET /documents/file/{documentId}?preview={true|false}`

- Requires authentication.
- Re-evaluates document access for the current user on every request.
- Returns the stored content with the persisted content type and a safe download filename.
- `preview=true` is honored only for supported PDF and image content types; unsupported preview requests fall back to download behavior or a clear unsupported response.
- Returns `404` for missing or unauthorized documents to avoid disclosing document existence.
- Records a download activity after an authorized successful delivery.
- Never accepts a filesystem path from the caller.

## Asynchronous scan job contract

`DocumentScanRequested` message:

```json
{
	"documentId": 123,
	"scanJobId": 456,
	"requestedAtUtc": "2026-09-16T12:00:00Z"
}
```

- The message contains identifiers only; the worker loads the authoritative private storage path from the database.
- Production transport: Azure Storage Queue named by configuration, consumed by an Azure Functions Queue Storage trigger. The function uses managed identity/secret-store configuration for storage access and calls the production malware scanner adapter.
- Offline training transport: a local bounded queue and hosted background worker invoke the same handler without Azure SDK or cloud access. The deterministic local scanner implements the same `IMalwareScanner` boundary.
- The handler transitions `Queued -> Processing -> Completed` and updates the document to `Clean` or `Rejected`. It is idempotent for duplicate deliveries and does not reprocess terminal documents.
- Transient failures increment the attempt count and retry with bounded backoff. Exhausted messages move to a poison/dead-letter queue and set the document to `Failed`; no failed document is available to users.
- A clean result creates the scan-completed activity/status update and enables normal list/search/download/preview. A rejected result records a safe failure reason, keeps content inaccessible, and notifies the owner.

## Service boundary

`IDocumentService` owns validation, permission checks, upload orchestration, list/search/filter, metadata updates, replacement, deletion, sharing, and activity/report queries. Methods receive the requesting user identity and return only authorized results.

`IFileStorageService` owns private content operations: upload to a generated relative path, download by relative path, delete by relative path, and optional URL/stream handling. Implementations must reject rooted paths and traversal outside the configured private root.

`IMalwareScanner` owns file inspection and returns a deterministic pass/fail result for the offline training implementation. Production deployment supplies an approved scanner implementation without changing document business rules.

## Upload sequence contract

1. Validate metadata, file size, allowlisted type, project/task permission, and current user.
2. Generate a GUID-based relative path using user/project-or-personal segments.
3. Save content through `IFileStorageService` and save metadata with `ScanStatus=Pending` through `ApplicationDbContext`.
4. Enqueue `DocumentScanRequested` after the metadata transaction succeeds.
5. Process the scan asynchronously through the local worker or Azure Function queue trigger.
6. Make the document available only after a clean result; create project notifications and upload/scan activity at the appropriate successful transitions.
7. If database persistence or enqueueing fails, delete the newly stored content or mark the document for safe retry; never report availability before a scan job exists.

## Failure and authorization contract

- Invalid input, rejected malware scan, unauthorized access, missing content, storage/database failures, queue failures, and poison messages produce clear user feedback without exposing paths or sensitive details.
- Owners can edit/replace/delete/share their documents; project managers can manage documents in their projects; administrators can audit all documents.
- Search, list, preview, download, edit, replace, delete, share, and report operations all apply authorization independently.
