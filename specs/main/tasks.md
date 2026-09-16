# Tasks: Document Upload and Management

**Input**: Design documents from `specs/main/` and feature specification from `specs/001-document-management/spec.md`
**Prerequisites**: [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [document-management.md](contracts/document-management.md), [quickstart.md](quickstart.md)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare configuration, private runtime storage, and repository conventions for document processing.

- [X] T001 [P] Add `AppData/uploads/` to `ContosoDashboard/.gitignore` while preserving the directory for local runtime creation
- [X] T002 [P] Add document storage, upload limits, scan queue name, retry limits, and scanner mode settings to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`
- [X] T003 [P] Add the document-management routes, storage location, async scan lifecycle, and offline-versus-production scanner behavior to `README.md`

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the data, security, storage, scan, and notification boundaries required by every user story.

- [X] T004 [P] Create the integer-key `Document` entity with required title/category, optional description/project/task associations, display filename, private file path, MIME type capacity of 255 characters, byte size, upload timestamps, and `Pending`/`Scanning`/`Clean`/`Rejected`/`Failed` scan state in `ContosoDashboard/Models/Document.cs`
- [X] T005 [P] Create normalized searchable tag entities and enforce the unique document/tag value constraint in `ContosoDashboard/Models/DocumentTag.cs`
- [X] T006 [P] Create internal user/team sharing entities with exactly one recipient kind and duplicate-share constraints in `ContosoDashboard/Models/DocumentShare.cs`
- [X] T007 [P] Create immutable document activity entities for upload, scan, download, delete, and share actions in `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T008 [P] Create queued scan-job entity and `Queued`/`Processing`/`Completed`/`Retrying`/`Poisoned` states in `ContosoDashboard/Models/DocumentScanJob.cs`
- [X] T009 [P] Define the storage abstraction for generated relative paths, private upload, download, and delete operations in `ContosoDashboard/Services/IFileStorageService.cs`
- [X] T010 [P] Define the scanner abstraction and deterministic offline result contract in `ContosoDashboard/Services/IMalwareScanner.cs`
- [X] T011 [P] Define the identifier-only `DocumentScanRequested` message and queue publisher/handler contracts in `ContosoDashboard/Services/DocumentScanQueue.cs`
- [X] T012 Configure document, tag, share, activity, and scan-job DbSets, relationships, delete behavior, unique indexes, status indexes, and 255-character MIME storage in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T013 Extend `ContosoDashboard/Models/User.cs`, `ContosoDashboard/Models/Project.cs`, and `ContosoDashboard/Models/TaskItem.cs` with document navigation properties without changing existing integer key conventions
- [X] T014 Add document-specific authorization predicates for owner, project member, project manager, explicit internal share, and administrator access in `ContosoDashboard/Services/DocumentAuthorizationService.cs`
- [X] T015 Extend notification types and safe message creation support for document shares, project additions, scan completion, scan rejection, and scan failure in `ContosoDashboard/Models/Notification.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [X] T016 Register document services, local storage, deterministic scanner, local queue, hosted worker, and authorization services in `ContosoDashboard/Program.cs` without requiring cloud services
- [X] T017 Add database initialization or migration handling for the document schema and verify private upload directories are created outside `ContosoDashboard/wwwroot` in `ContosoDashboard/Program.cs`

**Checkpoint**: Data, authorization, private storage, scan, queue, and notification boundaries are available for story work.

## Phase 3: User Story 1 - Upload and Securely Organize Documents (Priority: P1) MVP

**Goal**: Authenticated employees can submit supported documents, see asynchronous scan status, and receive a document only after a clean result.

**Independent Test**: Upload a valid file, observe `Pending`/`Scanning`, process it with the local worker, and confirm `Clean` availability; repeat with oversized, unsupported, scanner-rejected, and storage-failed inputs.

### Implementation

- [X] T018 [P] [US1] Implement private GUID-based filesystem paths, rooted/traversal rejection, stream copying, and cleanup on failure in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T019 [P] [US1] Implement allowlisted extension/content-type validation, 25 MB per-file enforcement, metadata validation, and deterministic training scan behavior in `ContosoDashboard/Services/LocalMalwareScanner.cs`
- [X] T020 [US1] Implement the upload orchestration in `ContosoDashboard/Services/DocumentService.cs` with authorize -> generate path -> save private content -> persist `Pending` metadata -> enqueue scan ordering and cleanup when persistence or enqueueing fails
- [X] T021 [US1] Implement the local bounded queue and hosted background scan worker in `ContosoDashboard/Services/LocalDocumentScanWorker.cs`, including `Pending -> Scanning` transitions, idempotency, bounded retries, poison handling, and owner notifications
- [X] T022 [US1] Add the Azure Queue Storage publisher contract and production deployment adapter documentation, including Queue Storage trigger message shape, managed identity/secret-store configuration, retry policy, and poison queue behavior in `ContosoDashboard/Services/AzureDocumentScanQueuePublisher.cs` and `docs/azure-document-scan.md`
- [X] T023 [US1] Implement upload form, multi-file selection, required metadata, category choices, project/task association, progress state, per-file result messages, and pending/rejected status display in `ContosoDashboard/Pages/Documents.razor`
- [X] T024 [US1] Add upload navigation and authenticated document access entry points in `ContosoDashboard/Shared/NavMenu.razor`
- [X] T025 [US1] Add deterministic local scanner fixtures and manual verification data for clean and rejected files in `ContosoDashboard/AppData/README.md`

**Checkpoint**: User Story 1 independently supports secure asynchronous upload and scan-before-availability.

## Phase 4: User Story 2 - Find and Use Authorized Documents (Priority: P1)

**Goal**: Employees can browse, filter, sort, search, preview, and download only clean documents they are authorized to access.

**Independent Test**: Seed personal, project, shared, pending, rejected, and unrelated documents; verify list/search/filter performance and protected delivery for multiple roles.

### Implementation

- [X] T026 [US2] Complete authorized document list, search, sort, category/project/date filters, empty states, and exclusion of non-clean documents in `ContosoDashboard/Services/DocumentService.cs`
- [X] T027 [US2] Add the protected `GET /documents/file/{documentId}` download/preview endpoint with claim-derived identity, repeated authorization, `404` for missing/unauthorized documents, safe filenames, persisted MIME types, and download activity logging in `ContosoDashboard/Program.cs`
- [X] T028 [US2] Complete the responsive documents table, search/filter controls, sort controls, clean-only download/preview actions, and owner scan-status messages in `ContosoDashboard/Pages/Documents.razor`
- [X] T029 [US2] Add project document retrieval and clean-document display to `ContosoDashboard/Services/ProjectService.cs` and `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T030 [US2] Add query indexes and projection/query-shape optimizations needed to meet the 2-second list/search target for up to 500 documents in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T031 [US2] Add the authenticated Shared with Me query and page shell, limited to clean documents explicitly shared with the current user or team, in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/SharedDocuments.razor`

**Checkpoint**: User Stories 1 and 2 independently support clean-document retrieval with authorization at query and delivery boundaries.

## Phase 5: User Story 3 - Maintain and Share Documents (Priority: P2)

**Goal**: Owners and authorized project managers can update, replace, delete, and share documents with controlled internal recipients.

**Independent Test**: Exercise owner and project-manager actions, then repeat as an unauthorized user and verify no protected state or file changes occur.

### Implementation

- [X] T032 [US3] Implement owner/project-manager authorization for metadata edits, file replacement, confirmed deletion, and share creation in `ContosoDashboard/Services/DocumentService.cs`
- [X] T033 [US3] Implement replacement workflow that stores a new private file, queues a new scan, preserves metadata associations, and keeps the prior file unavailable until the replacement is clean in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T034 [US3] Implement deletion cleanup for database metadata, private content, active shares, and audit events with safe retry behavior in `ContosoDashboard/Services/DocumentService.cs`
- [X] T035 [US3] Implement internal user/team share persistence, duplicate prevention, recipient notifications, and explicit-share access checks in `ContosoDashboard/Models/DocumentShare.cs`, `ContosoDashboard/Services/DocumentService.cs`, and `ContosoDashboard/Services/NotificationService.cs`
- [X] T036 [US3] Add metadata edit, replacement, delete-confirmation, and share-recipient UI flows in `ContosoDashboard/Pages/Documents.razor`
- [X] T037 [US3] Complete Shared With Me rendering, recipient notifications, and clean-document download/preview actions in `ContosoDashboard/Pages/SharedDocuments.razor`

**Checkpoint**: User Story 3 independently supports controlled document maintenance and authenticated internal sharing.

## Phase 6: User Story 4 - Connect Documents to Daily Work (Priority: P2)

**Goal**: Employees can work with documents from tasks and see recent documents and counts on the dashboard.

**Independent Test**: Attach/upload from an authorized task, verify project association, then verify dashboard recent uploads, count, and project notifications.

### Implementation

- [X] T038 [US4] Add task-document retrieval and attach/upload authorization that derives project association from the task in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/TaskService.cs`
- [X] T039 [US4] Add related-document display and attach/upload controls to the existing task detail surface in `ContosoDashboard/Pages/TaskDetails.razor`
- [X] T040 [US4] Add recent-five-document and document-count queries to `ContosoDashboard/Services/DashboardService.cs` with clean-only and current-user scoping
- [X] T041 [US4] Add the Recent Documents widget, document count summary card, pending/rejected status presentation, and links to protected document actions in `ContosoDashboard/Pages/Index.razor`
- [X] T042 [US4] Notify authorized project members after a document reaches clean availability and notify relevant task/project users through existing notification persistence in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`

**Checkpoint**: User Story 4 independently connects clean documents to tasks, projects, dashboard summaries, and notifications.

## Phase 7: User Story 5 - Audit Document Activity (Priority: P3)

**Goal**: Administrators can inspect document activity and generate reports while non-administrators are denied.

**Independent Test**: Perform representative upload, scan, download, share, replacement, and deletion actions; verify complete administrator reporting and non-administrator denial.

### Implementation

- [X] T043 [US5] Implement administrator-only activity queries and aggregate report methods for file types, active uploaders, and access patterns in `ContosoDashboard/Services/DocumentService.cs`
- [X] T044 [US5] Add administrator authorization and audit/report page with scan status, retries, poison jobs, upload/download/delete/share activity, and safe failure details in `ContosoDashboard/Pages/AdminDocuments.razor`
- [X] T045 [US5] Add audit event creation for scan transitions, successful downloads, shares, replacements, and deletions with UTC timestamps in `ContosoDashboard/Services/DocumentService.cs`
- [X] T046 [US5] Add operational guidance for Azure Function monitoring, Queue Storage poison messages, retention, and production malware scanner replacement in `docs/azure-document-scan.md`

**Checkpoint**: User Story 5 independently provides administrator audit and reporting controls.

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Complete quality, security, performance, and documentation checks across the feature.

- [X] T047 [P] Add focused service-level regression coverage for authorization, path traversal, upload cleanup, duplicate queue delivery, retry/poison handling, and clean-only access in `ContosoDashboard.Tests/DocumentManagementTests.cs` if a test project is introduced; otherwise record the equivalent manual checks in `specs/main/quickstart.md`
- [X] T048 [P] Review all document UI and protected endpoint messages for accessible labels, keyboard operation, safe error detail, and no disclosure of filesystem paths in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/SharedDocuments.razor`, and `ContosoDashboard/Pages/AdminDocuments.razor`
- [X] T049 Run `dotnet build .\ContosoDashboard\ContosoDashboard.csproj` and resolve document-feature build errors without changing unrelated behavior
- [X] T050 Run every upload, queue, retry, poison-message, authorization, integration, performance, and audit scenario in `specs/main/quickstart.md`; record results and known training limitations in `README.md`
- [X] T051 Verify `AppData/uploads/` is outside `wwwroot`, excluded from source control, and contains no committed secrets or user-controlled path segments in `ContosoDashboard/.gitignore`, `ContosoDashboard/appsettings.json`, and `ContosoDashboard/Services/LocalFileStorageService.cs`

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** has no dependencies and can start immediately.
- **Phase 2: Foundational** depends on Phase 1 and blocks all user stories.
- **Phase 3: User Story 1** depends on Phase 2 and is the MVP increment.
- **Phase 4: User Story 2** depends on the document and scan lifecycle from Phase 3, especially T020-T023.
- **Phase 5: User Story 3** depends on clean-document retrieval from Phase 4 and the upload service from Phase 3.
- **Phase 6: User Story 4** depends on the document service and existing task/dashboard surfaces from Phases 3-4.
- **Phase 7: User Story 5** depends on activity records produced by Phases 3-6.
- **Phase 8: Polish** depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational; delivers the MVP.
- **US2 (P1)**: Depends on US1's document and scan lifecycle; retrieval can then be independently verified.
- **US3 (P2)**: Depends on US1 upload and US2 clean-document access; maintenance and sharing are independently testable afterward.
- **US4 (P2)**: Depends on US1 document creation and US2 authorized retrieval; task/dashboard integration is otherwise isolated.
- **US5 (P3)**: Depends on activity events from US1-US4; administrator reporting is isolated from ordinary document UI.

### Parallel Opportunities

- Setup tasks T001-T003 can run in parallel.
- Foundational model/interface tasks T004-T011 can run in parallel; T012-T017 follow their contracts and may be split by file ownership.
- In US1, T018-T019 can run in parallel; T020 follows both; T021-T022 can proceed once T011 and T020's message contract are stable.
- In US2, T027, T029, and T030 can proceed in parallel after T026's service query contract is established.
- In US3, T033-T035 can be split by storage, persistence, and notification ownership after T032 defines authorization behavior.
- In US4, T040-T042 can proceed in parallel after T038 establishes task/project association rules.
- In US5, T043-T046 can be split between reporting, UI, activity, and deployment documentation.
- Polish tasks T047-T048 can run in parallel with each other after the feature slices stabilize.

## Parallel Example: User Story 1

```text
Task T018: Implement private GUID-based local storage in ContosoDashboard/Services/LocalFileStorageService.cs
Task T019: Implement allowlist validation and deterministic scanner in ContosoDashboard/Services/LocalMalwareScanner.cs

After T018-T019:
Task T020: Implement upload orchestration in ContosoDashboard/Services/DocumentService.cs
Task T021: Implement local queue worker in ContosoDashboard/Services/LocalDocumentScanWorker.cs
Task T022: Document Azure Queue Storage trigger adapter in ContosoDashboard/Services/AzureDocumentScanQueuePublisher.cs and docs/azure-document-scan.md
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Setup and Foundational phases.
2. Complete US1 upload, private storage, deterministic scan, local worker, and status UI.
3. Run the US1 independent verification, including failure cleanup and clean-only availability.
4. Demonstrate the offline training workflow before adding broader retrieval and collaboration.

### Incremental Delivery

1. Add US2 browsing/search/download/preview with repeated authorization.
2. Add US3 maintenance and authenticated internal sharing.
3. Add US4 task/dashboard integration and notifications.
4. Add US5 administrator audit/reporting.
5. Run cross-cutting build, security, accessibility, performance, and quickstart verification.

### Azure Production Migration

1. Keep `IDocumentScanQueue` and the scan handler payload stable.
2. Replace the local queue publisher/worker with Azure Queue Storage and an Azure Functions Queue Storage trigger.
3. Replace the deterministic local scanner with an approved production malware scanner implementation.
4. Configure managed identity or an approved secret store, retry/poison queue monitoring, and operational alerts without changing document business logic or UI behavior.

## Notes

- No automated test tasks are assumed because the repository currently has no test project; T047 records a focused regression path if one is introduced and otherwise extends the manual verification guide.
- Every task includes an exact repository path and follows the required checklist format: checkbox, sequential ID, optional `[P]`, required story label in story phases, and concrete description.
