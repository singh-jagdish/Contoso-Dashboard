<!--
Sync Impact Report
- Version change: scaffold -> 1.0.0
- Modified principles: all five placeholder principles replaced with project principles
- Added sections: Training and Security Constraints; Development Workflow and Quality Gates
- Removed sections: none from the resolved scaffold
- Follow-up TODOs: none
-->
# ContosoDashboard Constitution

## Core Principles

### I. User Value and Simplicity
Every feature MUST address a clear employee or training outcome and MUST use the simplest design that satisfies the documented need. Work MUST avoid speculative abstractions, unnecessary infrastructure, and features that cannot be demonstrated through a user journey. This keeps the training application understandable and keeps implementation effort focused on useful behavior.

### II. Layered Design and Replaceable Infrastructure
Application behavior MUST remain separated between the user interface, business services, data models, and infrastructure concerns. Infrastructure dependencies MUST be accessed through appropriate abstractions when the project requires offline operation and a credible migration path to production services. Business rules MUST NOT be duplicated across pages or hidden inside persistence-specific code.

### III. Authorization and Data Protection
All authenticated features MUST enforce authorization at the page or endpoint boundary and at the service boundary before protected data is read or changed. User, project, task, and future document data MUST be scoped to the requesting user's permissions. File data MUST remain outside publicly served directories, and user-controlled names MUST NOT become storage paths without validation and safe unique naming.

### IV. Verifiable Incremental Delivery
Each feature MUST be expressed as independently testable user scenarios with explicit acceptance outcomes. Implementation MUST proceed in small increments that can be built, exercised, and reviewed independently. Changes that affect shared services, authorization, persistence, or cross-feature contracts MUST include focused regression coverage or an explicitly documented manual verification path.

### V. Observable and Maintainable Behavior
User-visible operations MUST provide clear success and failure feedback, and important business events MUST be traceable through appropriate notifications or logs. Code MUST use meaningful names, preserve existing conventions, and avoid hiding failures behind silent fallbacks. Known limitations and training-only behavior MUST be documented rather than presented as production-ready capability.

## Training and Security Constraints

ContosoDashboard is an offline training application. Local development MUST work without cloud subscriptions or external service dependencies. Mock authentication, seeded data, and local infrastructure are acceptable for training, but production limitations MUST remain explicit. Production-oriented changes MUST account for secure identity, password handling, MFA, transport security, auditability, accessibility, and data retention before deployment.

The application MUST preserve the existing ASP.NET Core and Blazor Server architecture unless a specification demonstrates that a change is necessary. Database and storage changes MUST preserve data ownership and access-control rules. Secrets MUST be supplied through environment variables or an approved secret store and MUST NOT be committed to configuration files.

## Development Workflow and Quality Gates

New work MUST begin with a feature specification that states user stories, acceptance scenarios, functional requirements, edge cases, and measurable success criteria. A reviewed implementation plan MUST precede task generation, and tasks MUST identify dependencies and concrete repository paths. The Spec Kit review gates MUST be honored before planning and before task generation.

Before completion, the implementation MUST build successfully, focused tests or verification steps MUST pass, and security-sensitive behavior MUST be checked for unauthorized access. Documentation MUST be updated when behavior, setup, configuration, or known limitations change. Reviewers MUST distinguish implemented behavior from planned or illustrative documentation.

## Governance

This constitution governs feature specifications, plans, tasks, implementation, and reviews for ContosoDashboard. A change to these principles requires an amendment to this file, an updated version, a dated impact report, and review of affected specifications and plans. Versioning follows semantic versioning: major versions represent incompatible governance changes, minor versions add or materially expand principles or constraints, and patch versions clarify wording without changing obligations.

Every feature review MUST verify compliance with the core principles, security constraints, and quality gates. Exceptions MUST be documented in the relevant plan with the reason, risks, and a simpler alternative that was considered. The constitution is reviewed whenever the architecture, deployment model, authentication model, or training purpose materially changes.

**Version**: 1.0.0 | **Ratified**: 2026-09-16 | **Last Amended**: 2026-09-16
