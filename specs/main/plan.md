# Implementation Plan: Document Upload and Management

**Branch**: `main` | **Date**: 2026-09-16 | **Spec**: [../001-document-management/spec.md](../001-document-management/spec.md)
**Input**: Feature specification from `specs/001-document-management/spec.md` (the setup script's current branch path is `specs/main`)

## Summary

Add secure document upload, private local storage, metadata browsing/search, project/task/dashboard integration, sharing, notifications, and administrator audit reporting to the existing offline Blazor Server application. Keep metadata in EF Core, store file bytes outside `wwwroot`, and isolate storage and malware scanning behind replaceable interfaces. Uploads enter a pending-scan state and are processed asynchronously by a background scan job; production can use an Azure Functions Queue Storage trigger, while offline training uses a local queue/worker equivalent without cloud dependencies. Documents become available only after a successful scan. All document operations re-check authorization in the service layer and protected file delivery path.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core Blazor Server, Razor Pages, Entity Framework Core SQL Server, existing cookie authentication and authorization policies  
**Storage**: Existing SQL Server/LocalDB metadata database plus private local filesystem content under an application data directory outside `wwwroot`  
**Testing**: `dotnet build`; focused manual and security scenarios in [quickstart.md](quickstart.md). No test project currently exists in the repository.  
**Target Platform**: Windows local/offline training deployment; existing ASP.NET Core HTTPS host  
**Project Type**: Single web application  
**Performance Goals**: Upload up to 25 MB within 30 seconds; lists/search within 2 seconds for up to 500 documents; supported preview within 3 seconds  
**Constraints**: Offline-capable; no required cloud dependency; integer document keys; text categories; private files; explicit per-operation authorization; asynchronous scan-before-availability; deterministic local scanner and local queue/worker behind replaceable boundaries  
**Scale/Scope**: Existing seeded ContosoDashboard users and projects; initial release covers document metadata, one current file per document, explicit internal sharing, task/dashboard integration, notifications, and admin reports

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. User Value and Simplicity: PASS**. The plan uses existing pages, services, models, and notifications; no new project or speculative platform is introduced.
- **II. Layered Design and Replaceable Infrastructure: PASS**. UI, `IDocumentService`, EF entities, `IFileStorageService`, and `IMalwareScanner` remain separate.
- **III. Authorization and Data Protection: PASS**. Content is outside `wwwroot`; generated paths are used; service queries and file delivery repeat authorization checks.
- **IV. Verifiable Incremental Delivery: PASS**. User stories map to independently testable upload, retrieval, maintenance/sharing, integration, and audit slices; [quickstart.md](quickstart.md) defines focused checks.
- **V. Observable and Maintainable Behavior: PASS**. User feedback, notifications, audit activity, failure cleanup, and training limitations are explicit.
- **Training and Security Constraints: PASS**. The design is local/offline, preserves Blazor Server, reuses mock authentication, and identifies production scanner/identity/audit limitations.
- **Development Workflow and Quality Gates: PASS**. The spec is complete, this plan precedes tasks, and the validation guide includes build and unauthorized-access checks.

## Phase 0: Research Outcomes

Research decisions and alternatives are recorded in [research.md](research.md). The main resolved choices are:

1. Extend the existing single Blazor Server project.
2. Store EF metadata in `ApplicationDbContext` and file bytes in a private filesystem root.
3. Use replaceable storage, scan-queue, and malware scanner interfaces with a deterministic local scanner; use Azure Functions with Queue Storage triggers for the production async scan path.
4. Reuse existing role, project membership, task, dashboard, and notification abstractions.
5. Apply authorization in both services and protected file delivery.

## Phase 1: Design

- [data-model.md](data-model.md) defines `Document`, tags, shares, activity, project/task relationships, constraints, and indexes.
- [contracts/document-management.md](contracts/document-management.md) defines protected routes, file delivery behavior, service boundaries, upload sequencing, failure behavior, and authorization rules.
- [quickstart.md](quickstart.md) defines build, manual, performance, and security validation scenarios.

## Project Structure

### Documentation

```text
specs/001-document-management/
└── spec.md                         # Feature specification

specs/main/
├── plan.md                         # This implementation plan
├── research.md                     # Phase 0 decisions
├── data-model.md                   # Phase 1 data design
├── contracts/
│   └── document-management.md      # UI, endpoint, and service contracts
└── quickstart.md                   # Validation guide
```

### Source Code

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs             # Document DbSets, relationships, indexes
├── Models/
│   ├── Document.cs                           # Document metadata and category values
│   ├── DocumentTag.cs                        # Searchable tags
│   ├── DocumentShare.cs                      # Internal user/team shares
│   ├── DocumentActivity.cs                   # Audit events
│   └── TaskItem.cs / Project.cs / User.cs    # Existing relationships extended as needed
├── Services/
│   ├── DocumentService.cs                    # Authorization and document workflows
│   ├── FileStorageService.cs                 # Private local storage abstraction/implementation
│   ├── MalwareScanner.cs                     # Scanner abstraction/local implementation
│   ├── DashboardService.cs                   # Recent documents and count
│   └── NotificationService.cs                # Existing notification persistence reused
├── Pages/
│   ├── Documents.razor                       # Browse, search, filter, upload, manage
│   ├── SharedDocuments.razor                 # Shared with Me
│   ├── AdminDocuments.razor                  # Administrator audit/report view
│   ├── ProjectDetails.razor                  # Project documents integration
│   ├── TaskDetails.razor                     # Task document integration, if route is added
│   └── Index.razor                           # Recent documents and count widget
├── Program.cs                                # Service registration and protected endpoint
└── AppData/uploads/                          # Runtime private content; not wwwroot or source-controlled
```

**Structure Decision**: Use the existing single `ContosoDashboard` web project and its established `Models`, `Data`, `Services`, and `Pages` folders. Add no separate frontend/backend project. The runtime upload directory is private application data and must be excluded from source control.

## Implementation Sequencing

1. Add entities, EF relationships/indexes, category/type validation, and database initialization compatibility.
2. Add private storage and deterministic scanner abstractions, path validation, upload cleanup, and service registration.
3. Add scan-job contracts and the asynchronous workflow: enqueue after private storage, process with a local worker in training, and provide an Azure Functions Queue Storage-triggered scanner adapter for production.
4. Implement `DocumentService` authorization, upload status, list/search/filter, download/preview metadata, edit/replace/delete, shares, and activity records; exclude pending/rejected scans from ordinary access.
5. Add protected file delivery and authenticated document pages with pending, clean, and rejected states.
6. Integrate project/task views, dashboard summary/recent documents, and notifications, including scan completion/failure feedback.
7. Add administrator reports, seed/manual data as needed, documentation, and focused verification of retries, poison messages, and unauthorized access.

## Complexity Tracking

No constitution violations require justification. The added storage/scanner abstractions are required by the documented offline-to-production migration and security constraints, while the feature remains in the existing single project.
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
