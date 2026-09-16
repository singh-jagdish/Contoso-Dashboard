# Research: Document Upload and Management

## Decision: Keep the feature inside the existing single-project Blazor Server architecture

- **Rationale**: The repository is a single `ContosoDashboard` ASP.NET Core project targeting .NET 10 with Razor Pages, Blazor Server, EF Core SQL Server, scoped application services, and cookie-based training authentication. Adding a separate API or frontend project would violate the constitution's simplicity and preserve-existing-architecture constraints.
- **Alternatives considered**: A separate REST API and client application; rejected because the current UI, authentication state, and service boundaries are already local to the Blazor Server application.

## Decision: Persist metadata in EF Core and content in a private local directory

- **Rationale**: Existing entities use integer keys and `ApplicationDbContext` calls `EnsureCreated` for local training. Document metadata belongs in the same database, while file bytes must remain outside `wwwroot` so every download and preview can pass authorization. Relative, GUID-based paths preserve portability.
- **Alternatives considered**: Storing bytes in the database; rejected for 25 MB uploads and simpler future blob migration. Storing under `wwwroot`; rejected because static-file middleware would bypass document authorization.

## Decision: Use replaceable storage and malware-scanning interfaces

- **Rationale**: `IFileStorageService` isolates filesystem operations and a scanner interface isolates the deterministic offline training scanner from a future production scanner. The upload transaction order is validate and authorize, generate a unique relative path, scan, save bytes, save metadata, then notify; failures clean up the file and do not expose incomplete metadata.
- **Alternatives considered**: Calling `System.IO.File` directly from pages; rejected because it duplicates security rules and blocks Azure migration. Skipping scanning offline; rejected by the accepted clarification and the authorization/security constitution principle.

## Decision: Process malware scans asynchronously through a queue

- **Rationale**: A 25 MB upload and malware scan should not hold a Blazor Server circuit open or make the UI assume that storage implies trust. The upload service stores the private file and metadata in `PendingScan`, then enqueues a scan message containing only the document identifier and an opaque storage reference. A background worker scans the private content, updates the document to `Clean` or `Rejected`, and emits user feedback/audit activity. Production uses an Azure Function with an Azure Queue Storage trigger; the offline training release uses an in-process/local queue and background worker with the same message and handler contract.
- **Alternatives considered**: Scanning synchronously inside the upload request; rejected because it increases request/circuit time and makes retries unreliable. Exposing the file before scanning; rejected because pending content must not be downloadable or searchable. Requiring Azure Queue Storage in training; rejected because the constitution requires offline operation without cloud subscriptions.

## Decision: Make scan processing idempotent and retry-safe

- **Rationale**: Queue delivery can be retried and Azure Functions may process messages more than once. The handler re-reads the document, ignores already terminal states, uses a scan attempt/status record or guarded status transition, and only publishes availability after a clean result. Transient storage/scanner failures retry with backoff; poison messages move to a dead-letter/poison queue for administrator review without exposing the file.
- **Alternatives considered**: Assuming exactly-once queue delivery; rejected because it is not a safe distributed-processing assumption. Deleting files immediately on transient failure; rejected because it prevents retry and obscures operational diagnosis.

## Decision: Enforce authorization in the service and delivery endpoint

- **Rationale**: Existing services accept `requestingUserId` and return null/false for unauthorized access. Document queries must apply the same pattern before returning lists, search results, metadata, or streams; protected download/preview delivery must repeat the check because a URL can outlive a page authorization decision.
- **Alternatives considered**: Relying only on `[Authorize]` on Razor components; rejected because it does not express per-document ownership, project membership, sharing, or administrator rules.

## Decision: Reuse existing project, task, notification, and role concepts

- **Rationale**: `Project` exposes manager and member relationships, `TaskItem` links to projects, `NotificationService` persists in-app messages, and `UserRole` already defines Employee, TeamLead, ProjectManager, and Administrator. Document services should query these relationships rather than introduce parallel permission models.
- **Alternatives considered**: A new document-specific role system; rejected as duplicate policy and a security risk.

## Decision: Use relational metadata for tags, sharing, and audit events

- **Rationale**: Search must cover title, description, tags, uploader, and project while shares and audit events require repeatable authorization and reporting. Separate relational entities allow indexed, queryable data and avoid parsing serialized JSON in application code.
- **Alternatives considered**: JSON columns or delimited strings; rejected for filtering, uniqueness, and reporting reliability.

## Decision: Treat explicit sharing as authenticated internal access

- **Rationale**: The feature requires sharing with specific users or teams and excludes public or anonymous sharing. Until a separate product decision narrows cross-project sharing, an explicit share grants access to the selected authenticated internal recipient while project membership remains a separate access path. External recipients are out of scope.
- **Alternatives considered**: Restricting every share to project members; deferred because it would narrow the stakeholder requirement without confirmation. Anonymous/public links; rejected by the security constraints.

## Decision: Validate with build plus focused manual/security scenarios

- **Rationale**: The repository currently has no test project or test package. The cheapest executable checks are `dotnet build` and a local run/manual verification matrix covering upload validation, private delivery, authorization, notification, and failure cleanup. A later implementation task should add automated service tests if the project test harness is introduced.
- **Alternatives considered**: Claiming unit-test coverage that does not exist; rejected because it would misrepresent the quality gate.
