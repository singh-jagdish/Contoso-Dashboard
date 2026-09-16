# Data Model: Document Upload and Management

## Document

Represents one current work document and its searchable metadata.

- `DocumentId`: integer primary key.
- `Title`: required, bounded text.
- `Description`: optional bounded text.
- `Category`: required text value from the approved category list; not an integer enum.
- `Tags`: searchable custom tags represented through `DocumentTag` rows.
- `OriginalFileName`: display-only source name; never used as a storage path.
- `FilePath`: required relative private-storage path containing a generated GUID name.
- `FileType`: required MIME/content type with capacity for 255 characters.
- `FileSize`: required byte count, maximum 25 MB.
- `UploadedDate`: UTC upload timestamp.
- `UploadedByUserId`: required `User` relationship.
- `ProjectId`: optional `Project` relationship.
- `TaskId`: optional `TaskItem` relationship; task association must agree with the task's project.
- `ScanStatus`: required text state: `Pending`, `Scanning`, `Clean`, `Rejected`, or `Failed`.
- `ScanRequestedDate`: UTC timestamp for the queued scan request.
- `ScanCompletedDate`: nullable UTC timestamp for the latest terminal scan result.
- `ScanAttemptCount`: retry count for operational visibility.
- `ScanError`: optional bounded non-sensitive failure summary; never store scanner secrets or raw file content.

Validation: title/category required; category must be approved; extension/content type must be allowlisted; size must be at most 25 MB; path must be generated server-side; document is unavailable while `Pending`, `Scanning`, `Rejected`, or `Failed`.

## DocumentScanJob

Represents the asynchronous request to inspect a privately stored document.

- `DocumentScanJobId`: integer primary key.
- `DocumentId`: required document relationship.
- `StoragePath`: server-generated relative path or opaque storage key; never accepted from a client message.
- `RequestedDate`: UTC enqueue time.
- `StartedDate`: nullable UTC processing time.
- `CompletedDate`: nullable UTC completion time.
- `Status`: `Queued`, `Processing`, `Completed`, `Retrying`, or `Poisoned`.
- `AttemptCount`: delivery/processing attempt count.
- `FailureReason`: optional bounded operational summary.

The queue message should contain the `DocumentId` and a correlation/job identifier; the handler obtains the authoritative storage path from the database. This prevents clients or stale messages from changing the file target.

## DocumentTag

Associates a normalized custom tag value with a document for search and filtering.

- `DocumentTagId`: integer primary key.
- `DocumentId`: required relationship.
- `Value`: required normalized text value.

Uniqueness: one document cannot contain the same normalized tag twice.

## DocumentShare

Grants an explicit document permission to one internal user or team recipient.

- `DocumentShareId`: integer primary key.
- `DocumentId`: required relationship.
- `SharedByUserId`: required owner/authorized actor relationship.
- `RecipientUserId`: optional internal user recipient.
- `RecipientTeamKey`: optional internal team recipient identifier.
- `CreatedDate`: UTC timestamp.

Validation: exactly one recipient kind is populated; duplicate active shares are rejected; anonymous/external recipients are not supported. Explicit shares are an additional access path and do not remove owner, project, or administrator access.

## DocumentActivity

Immutable audit event for document actions.

- `DocumentActivityId`: integer primary key.
- `DocumentId`: required relationship/reference.
- `ActorUserId`: required user relationship/reference.
- `Action`: required text value: upload, download, delete, or share.
- `CreatedDate`: UTC timestamp.
- `Details`: optional bounded text for safe contextual information.

Validation: create an event after the corresponding action succeeds; administrators can query events, other users cannot.

## Relationships and access states

- A document belongs to one uploader and may belong to one project and one task.
- A task association must use the task's project context; a task without a project cannot receive a project-associated document.
- Project members can read project documents; project managers can manage documents in their projects.
- Owners can read/manage their own documents and create shares.
- Explicit shares grant read access to the recipient; administrator access is global.
- Deleted documents leave normal query results and protected delivery; their private file is removed after confirmation and the delete activity is recorded.
- Pending, scanning, rejected, failed, and poisoned documents are excluded from normal search/list/download/preview results, while the owner sees a safe status message and administrators can inspect operational state.

## Indexes and constraints

- Index `UploadedByUserId`, `ProjectId`, `UploadedDate`, and `Category` for list and recent-document queries.
- Index searchable uploader/project relationships and normalized tag values.
- Index `DocumentActivity` by `DocumentId`, `ActorUserId`, `Action`, and `CreatedDate` for audit reports.
- Index `Document` by `ScanStatus` and `ScanRequestedDate`, and `DocumentScanJob` by `Status`, `DocumentId`, and `RequestedDate` for worker polling, retries, and administrator review.
- Enforce unique generated `FilePath` and unique document/tag pair; use integer keys consistent with existing models.
