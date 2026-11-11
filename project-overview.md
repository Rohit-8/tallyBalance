MASTER PROJECT SPECIFICATION & AI AGENT INSTRUCTIONS
PROJECT: TallyBalance AI
Enterprise AI-Powered Invoice Processing, Financial Adjustment, Approval and Analytics Platform

Document type: Master development specification
Development approach: Phase-wise, iterative, test-driven development
Primary objective: Build a substantial, portfolio-quality enterprise application while teaching the developer the engineering concepts behind every implementation.

PART 1 — YOUR ROLE AS THE AI DEVELOPMENT AGENT

You are acting as a coordinated team of senior engineers and technical mentors.

You must fulfill these roles:

Senior Software Architect — responsible for architecture, module boundaries, design decisions and system scalability.
Senior .NET Backend Engineer — responsible for C#, ASP.NET Core, APIs, business logic, EF Core and backend engineering.
Senior Frontend Engineer — responsible for React, TypeScript, state management, component architecture and UX.
Database Engineer — responsible for SQL Server, relational modeling, indexes, constraints, transactions and query optimization.
AI Engineer — responsible for LLM integration, document intelligence, embeddings, RAG, evaluation and AI safety.
Distributed Systems Engineer — responsible for messaging, asynchronous processing, idempotency, retries and eventual consistency.
DevOps Engineer — responsible for Podman, CI/CD, deployment, environment management and observability.
QA Engineer — responsible for unit tests, integration tests, end-to-end tests and test strategy.
Security Engineer — responsible for authentication, authorization, tenant isolation, secure file handling and API security.
Technical Mentor — responsible for explaining all concepts to the developer in an understandable, practical way.
Your most important responsibility

Do not merely generate code.

Teach the developer how the application works, why each engineering decision was made, what alternatives exist and what problems the implementation solves.

The developer is a Software Engineer II with experience in:

C#
ASP.NET Core
Spring Boot
Node.js
React
MongoDB
Microsoft SQL Server
REST API development
Backend application development

The developer is currently strengthening knowledge in:

Advanced .NET
System design
Distributed systems
AI/LLM integration
Enterprise architecture
Concurrency and database transactions
Production engineering

Adjust explanations to this background. Do not explain basic programming concepts excessively, but explain unfamiliar architecture and advanced engineering concepts thoroughly.

PART 2 — PROJECT VISION
2.1 Product description

Build an enterprise SaaS application named TallyBalance AI.

TallyBalance AI is an AI-powered financial operations platform that helps organizations manage their accounts payable and invoice lifecycle.

Organizations should be able to:

Register their organization.
Invite employees.
Assign roles and permissions.
Manage vendors.
Upload invoices.
Extract information from invoice documents.
Review and validate invoice information.
Manage invoice line items.
Make financial adjustments.
Maintain adjustment history.
Undo and redo adjustments safely.
Submit invoices for approval.
Configure approval workflows.
Approve or reject invoices.
Generate AI-powered invoice summaries.
Ask natural-language questions about invoices.
Detect potentially duplicate invoices.
Identify anomalies and suspicious financial patterns.
Receive notifications.
Generate reports and analytics.
Track all important activities through audit logs.

The platform must demonstrate real enterprise engineering practices.

It should not be a basic CRUD application.

2.2 Product users

Implement the following roles.

Super Administrator
Manages platform-level settings.
Manages organizations.
Monitors system health.
Can access platform administration functionality.
Cannot casually bypass audit and security controls.
Organization Administrator
Manages their organization.
Invites users.
Assigns roles.
Configures approval workflows.
Manages vendors and settings.
Views organization-level analytics.
Finance Manager
Creates and manages invoices.
Reviews extracted invoice information.
Makes permitted financial adjustments.
Assigns approval requests.
Views financial reports.
Finance Agent
Uploads invoices.
Reviews invoice documents.
Updates permitted invoice information.
Creates adjustment requests.
Tracks approval status.
Approver
Views assigned approval tasks.
Reviews invoice information.
Approves or rejects invoices.
Adds comments.
Views approval history.
Auditor / Viewer
Has read-only access to authorized information.
Can view audit history.
Cannot modify financial records.

Use permission-based authorization in addition to role-based authorization.

A user can have multiple permissions through their roles.

Never rely on frontend hiding alone for authorization. Every protected operation must be enforced on the backend.

PART 3 — TECHNOLOGY STACK

Use the following technology stack unless there is a strong technical reason to propose an alternative.

Before implementation, verify compatible stable versions available in the development environment. Prefer supported LTS releases and pin versions.

Do not randomly upgrade dependencies during development.

3.1 Backend
Primary backend
Language: C#
Framework: ASP.NET Core Web API
Target: .NET 10 LTS, if available and supported by the installed SDK
ORM: Entity Framework Core
Database provider: Microsoft SQL Server
API documentation: OpenAPI / Swagger
Validation: FluentValidation
Mapping: Manual mapping or a suitable mapping library where useful
Logging: Serilog with structured logging
Authentication: ASP.NET Core Identity or an equivalent secure identity implementation
Authorization: Policy-based authorization and RBAC
API versioning: Versioned API endpoints
Background processing: .NET Worker Services
Backend architectural principles

Use:

Clean Architecture principles.
Modular Monolith architecture.
SOLID principles.
Dependency Injection.
Repository abstractions only where they provide value.
Domain-driven design concepts where appropriate.
Separation of concerns.
DTO-based API contracts.
Centralized exception handling.
Asynchronous programming.
CancellationToken propagation.
Explicit transaction boundaries.

Do not introduce unnecessary abstractions.

Do not create interfaces for every single class merely to make the code look enterprise-grade.

3.2 Frontend

Use:

React 19, or a compatible supported version.
TypeScript with strict mode enabled.
Vite.
Tailwind CSS.
shadcn/ui components.
React Router.
TanStack Query for server-state management.
React Hook Form.
Zod for frontend validation.
Recharts for analytics charts.
Lucide React for icons.
Frontend principles
Reusable components.
Feature-based folder structure.
Clear separation between API services and UI components.
Proper loading states.
Error states.
Empty states.
Responsive layouts.
Form validation.
Accessible controls.
Protected routes.
Consistent design system.

Do not put all business logic inside React components.

Do not store sensitive tokens in localStorage by default. Prefer secure, HttpOnly, appropriately configured cookies for refresh-token handling, with an appropriate CSRF defense strategy.

3.3 Database

Primary database:

Microsoft SQL Server.

Use:

Entity Framework Core.
Code-first migrations.
SQL Server indexes.
Foreign key constraints.
Unique constraints.
Check constraints.
Transactions.
RowVersion concurrency.
Decimal data types for monetary values.
Soft deletion only where justified.
Audit tables for critical financial operations.

Use decimal, not float or double, for monetary values.

Use appropriate decimal precision, such as decimal(19,4), with explicit currency and rounding rules.

3.4 Distributed systems

Use:

RabbitMQ for asynchronous messaging.
Redis for caching and distributed coordination only where justified.
.NET BackgroundService for background consumers and scheduled processing.
Transactional Outbox Pattern.
Idempotent consumers.
Retry policies.
Dead-letter queues.

Do not create multiple microservices immediately.

Start with a modular monolith.

Only introduce a separate AI processing worker and messaging infrastructure when the corresponding phase requires them.

3.5 AI and LLM stack

Build an AI integration layer that is provider-independent.

Use:

An LLM API through a provider abstraction.
OpenAI-compatible API integration where appropriate.
Embeddings API.
Qdrant as vector database.
Retrieval-Augmented Generation (RAG).
PDF text extraction.
Structured LLM outputs.
Tool/function calling where appropriate.
Human-in-the-loop approval.

APPROVED PROVIDER STRATEGY (2026-11-10, developer-approved — do not re-ask)

Every AI provider must be reached through a single OpenAI-compatible chat-completions interface. The system must work with any endpoint that implements the OpenAI /v1/chat/completions contract. A provider is defined entirely by three configuration values — base URL, API key, and model name — and adding a provider must never require provider-specific code.

Confirmed providers to support:

Custom MAQ AI endpoint: https://llm.maqsoftware.net/v1 with model identifiers such as qwen-3.8-27b, authenticated with a Bearer token. Request shape:

POST https://llm.maqsoftware.net/v1/chat/completions
Authorization: Bearer sk-...
Content-Type: application/json
{ "model": "qwen-3.8-27b", "messages": [{ "role": "user", "content": "..." }] }

Google Gemini models, accessed through their OpenAI-compatible endpoint.
NVIDIA NIM models, accessed through their OpenAI-compatible endpoint.
Groq models, accessed through their OpenAI-compatible endpoint.

Implementation rules:

Use one OpenAI-compatible client implementation (the OpenAI .NET SDK configured with a custom BaseUrl, or a thin HttpClient adapter over the same contract). Do not introduce vendor-specific SDKs for Gemini, NIM, or Groq.
Provider selection is configuration, not code: a named provider entry supplies BaseUrl, ApiKey, ModelId, and optional per-provider settings such as temperature, max tokens, and timeout.
Never hardcode provider credentials or model identifiers outside configuration. Secrets come from user-secrets in development and a secret store in production, never from committed files.
Because endpoints differ in practice, the adapter must tolerate capability variation: some endpoints do not support response_format or tool calling. Where a capability is unavailable the abstraction must degrade gracefully rather than fail, and the capability set must be part of the provider configuration.
A deterministic fake ILLMProvider is used in all unit and integration tests. No test may make a real network call to any provider.
Provider calls must be traced, timed, and rate-limited, and must record the model identifier and prompt version with every result so outputs remain reproducible after a model change.

Keep model names and API endpoints configurable.

Do not hardcode provider credentials or model identifiers throughout the codebase.

Create abstractions such as:

ILLMProvider
IEmbeddingProvider
IDocumentTextExtractor
IInvoiceExtractionService
IRetrievalService
IAIAnalysisService

These abstractions must be useful and not excessive.

3.6 File storage

Use an abstraction:

IFileStorageService

APPROVED STORAGE DECISION (2026-11-10, developer-approved — do not re-ask)

MinIO is the chosen object store and runs as a container in the same compose file as SQL Server, Redis, RabbitMQ, and Qdrant, on the Podman engine. Application code depends only on IFileStorageService and never on the MinIO SDK directly, so the store remains swappable for S3 or Azure Blob later. A local-filesystem implementation is retained for unit tests and for developers who want to run without containers, selected by configuration.

Initial implementation:

Local filesystem storage for development and tests.

Development object storage:

MinIO, containerised on Podman, accessed through IFileStorageService.

Future production implementation:

Azure Blob Storage.

Do not store uploaded PDF files directly in SQL Server.

Store file metadata, storage references, hashes and processing status in SQL Server.

3.7 Testing

Backend:

xUnit.
Moq or NSubstitute.
ASP.NET Core integration testing.
Testcontainers for SQL Server and other dependencies where feasible, configured to use the Podman socket (DOCKER_HOST pointing at the Podman pipe/socket) since Docker is not used on this project.

Frontend:

Vitest.
React Testing Library.
Playwright for end-to-end testing.

Use real SQL Server integration tests for important database behavior, not only EF Core InMemory.

3.8 DevOps and observability

IMPORTANT INFRASTRUCTURE RULE — CONTAINERS RUN ON PODMAN, NOT DOCKER:

All local containerized infrastructure (SQL Server, Redis, RabbitMQ, Qdrant, MinIO and any other supporting services) must run using Podman, not Docker. Do not assume the Docker daemon, the docker CLI, or Docker Desktop is installed or required. Container images must be OCI-compatible and pulled from public registries. Dockerfiles, where used, must be buildable with podman build. This rule applies to every phase of this document wherever containers, pods, or compose files are mentioned — do not ask the developer to re-confirm this.

VERIFIED APPROACH (see docs/adr/ADR-008-container-runtime-podman.md, ratified in Phase 0):
Compose files are written in standard Compose format and executed with the docker-compose binary
(v2.39.4, installed user-level at %LOCALAPPDATA%\Microsoft\dotnet-tools) pointed at Podman's
Docker-compatible API socket: DOCKER_HOST=npipe:////./pipe/docker_engine. The engine underneath is
still Podman — no Docker daemon, no Docker Desktop, no Docker dependency. This was smoke-tested
successfully before being adopted. Fall back to podman-compose only if this breaks, and record the
change in a superseding ADR.

Podman resource sizing on Windows must be changed via %USERPROFILE%\.wslconfig (memory, processors,
swap), NOT via `podman machine set`, which is unsupported for WSL machines. The machine must have at
least 8 GiB memory — SQL Server for Linux alone needs roughly 2 GiB.

IMPORTANT ENVIRONMENT RULE — NO ADMINISTRATOR PRIVILEGES:

This development machine cannot be elevated. All tooling must be installed at user scope only
(example: .NET SDK 10.0.403 installed via dotnet-install.ps1 into %LOCALAPPDATA%\Microsoft\dotnet,
with PATH and DOTNET_ROOT persisted in User scope). Never use machine-wide installers, never run
commands that trigger a UAC prompt, and never retry such a command with variations. If a task
genuinely requires elevation, stop and tell the developer instead of attempting workarounds.

Use:

Podman 6.0.2 (WSL machine, rootless where possible).
docker-compose binary driving the Podman engine (per ADR-008); podman-compose as fallback.
GitHub Actions.
OpenTelemetry.
Serilog.
Health checks.
Structured logs.
Correlation IDs.
Metrics.
Distributed tracing.

Deployment target:

Azure App Service or a suitable Azure container-hosting service.
Azure SQL Database.
Azure Blob Storage.

Deployment is a portfolio demonstration, not a claim of production certification.

PART 4 — ARCHITECTURE
4.1 Overall architecture

Use a Modular Monolith architecture.

The system should initially contain:

API Host
Application Layer
Domain Layer
Infrastructure Layer
Background Worker
React Frontend
SQL Server
Redis
RabbitMQ
Qdrant
File Storage

The React frontend communicates with ASP.NET Core APIs.

The backend communicates with infrastructure through interfaces and dependency injection.

The background worker handles asynchronous tasks.

RabbitMQ handles messages between independently executing processing components.

Qdrant stores embeddings for semantic retrieval.

4.2 Backend solution structure

Use this structure as a starting point:

TallyBalanceAI/

backend/

    TallyBalance.sln

    src/

        TallyBalance.Api/

        TallyBalance.Application/

        TallyBalance.Domain/

        TallyBalance.Infrastructure/

        TallyBalance.Worker/

        Modules/

            Identity/

            Organizations/

            Vendors/

            Invoices/

            Adjustments/

            Approvals/

            Documents/

            Notifications/

            AI/

            Analytics/

        Shared/

    tests/

        TallyBalance.UnitTests/

        TallyBalance.IntegrationTests/

        TallyBalance.ArchitectureTests/

frontend/

    TallyBalance-web/

infrastructure/

    podman/          # compose/pod definitions for local infrastructure (podman-compose.yaml or Quadlet units)

    sql/

    scripts/

docs/

    architecture/

    database/

    api/

    adr/

    learning/

scripts/

README.md


You may adapt the structure when needed, but document the reasons.

4.3 Domain boundaries
Identity Module

Responsible for:

Users.
Credentials.
Roles.
Permissions.
Authentication.
Refresh tokens.
User sessions.
Organization Module

Responsible for:

Organizations.
Organization membership.
Tenant configuration.
Organization settings.
Tenant-specific authorization.
Vendor Module

Responsible for:

Vendor information.
Vendor contact details.
Tax identifiers.
Vendor payment metadata.
Vendor status.
Invoice Module

Responsible for:

Invoice creation.
Invoice lifecycle.
Invoice line items.
Invoice status.
Vendor relationships.
Invoice validation.
Adjustment Module

Responsible for:

Financial adjustments.
Adjustment history.
Undo and redo.
Concurrency handling.
Financial audit history.
Approval Module

Responsible for:

Approval workflow definitions.
Approval stages.
Approval assignments.
Approval decisions.
Approval comments.
Workflow transitions.
Document Module

Responsible for:

Document uploads.
Document metadata.
File validation.
File storage.
Document extraction status.
Document processing lifecycle.
Notification Module

Responsible for:

In-app notifications.
Email notification abstraction.
Notification templates.
Delivery attempts.
Retry management.
AI Module

Responsible for:

Invoice extraction.
Invoice summaries.
RAG.
Embeddings.
Duplicate detection assistance.
Financial anomaly assistance.
AI recommendations.
AI execution safety.
Analytics Module

Responsible for:

Financial dashboard.
Invoice reports.
Vendor reports.
Approval metrics.
Processing metrics.
PART 5 — DETAILED FUNCTIONAL REQUIREMENTS
MODULE 1 — IDENTITY AND AUTHENTICATION

Implement secure authentication.

Features
User registration.
User login.
Logout.
Refresh token rotation.
Password hashing.
Forgot password.
Password reset.
Change password.
User profile.
Account status.
Role management.
Permission management.
User invitation.
Session revocation.
Security requirements
Never store plaintext passwords.
Use established password hashing through ASP.NET Core Identity or equivalent.
Use short-lived access tokens.
Implement refresh token rotation and revocation.
Protect refresh token endpoints from CSRF.
Apply login throttling.
Validate token issuer, audience and expiration.
Prevent privilege escalation.
Never trust role information supplied by the frontend.
Record important security events.
Suggested entities
User
Role
Permission
UserRole
RolePermission
RefreshToken
UserSession
PasswordResetToken
API endpoints

POST /api/v1/auth/register

POST /api/v1/auth/login

POST /api/v1/auth/refresh

POST /api/v1/auth/logout

POST /api/v1/auth/forgot-password

POST /api/v1/auth/reset-password

GET /api/v1/auth/me

PUT /api/v1/users/profile

GET /api/v1/users

POST /api/v1/users/invite

Acceptance criteria
Unauthenticated users cannot access protected endpoints.
Expired access tokens are rejected.
Revoked refresh tokens cannot be reused.
Users cannot access another user's private resources.
Unauthorized role changes are rejected.
Authentication and authorization tests pass.
MODULE 2 — MULTI-TENANT ORGANIZATION MANAGEMENT

The application must support multiple organizations.

Each organization has its own users, vendors, invoices and business data.

Features
Organization registration.
Organization profile.
Organization settings.
Invite members.
Assign members to roles.
Remove members.
Suspend organization.
Organization-specific financial configuration.
Organization-specific currency preferences.
Tenant-aware dashboard.

Approved financial configuration decisions (2026-11-10, developer-approved — do not re-ask):

Base currency is INR. New organizations default to CurrencyCode INR and timezone Asia/Kolkata. Per-invoice currency may still differ, because an Indian organization can legitimately hold USD or EUR vendor invoices. INR is the default and reporting currency, not a constraint on individual invoices.

Tax and discount must be configurable at BOTH levels. OrganizationSettings exposes TaxMode and DiscountMode, each one of PerLine, InvoiceLevel, or Both, plus separate TaxRoundingPolicy and AmountRoundingPolicy, plus AllowMixedTaxModes and AllowMixedDiscountModes for per-invoice override.

These are editable by an Organization Administrator or Finance Manager on a Settings → Financial Configuration page in the frontend. This page is a required deliverable of the organization administration screens.

Keep tax rounding and amount rounding as SEPARATE settings. Indian GST practice commonly rounds the tax amount to the nearest rupee while line amounts retain paise. Collapsing them into one policy would force the wrong choice on one of the two.

The effective mode must be snapshotted onto each invoice as EffectiveTaxMode and EffectiveDiscountMode at creation, so that a later settings change cannot retroactively invalidate an existing invoice's stored totals. This is the same versioning principle used for approval workflow versions.
Tenant isolation

Implement organization-level tenant isolation.

Every tenant-owned entity must contain an OrganizationId or have a clearly documented equivalent ownership relationship.

The backend must derive tenant context from authenticated membership, not blindly trust an OrganizationId in the request body.

Implement:

Tenant resolution.
Tenant-aware database queries.
Authorization validation.
Tenant-aware repository/query boundaries.
Tenant-aware background processing.
Tenant-aware caching.
Tenant-aware AI retrieval.

Test attempts to access another tenant's invoices, files and AI context.

Do not assume EF Core global query filters alone are a complete security boundary.

Suggested entities
Organization
OrganizationMember
OrganizationSettings
OrganizationInvitation
Acceptance criteria
Two organizations can coexist.
Organization A cannot access Organization B's records.
Cross-tenant API tests pass.
Background jobs preserve tenant context.
Organization settings are isolated.
MODULE 3 — VENDOR MANAGEMENT

Build vendor management.

Features
Create vendor.
Update vendor.
View vendor.
Archive vendor.
Search vendors.
Filter vendors.
Vendor contact information.
Tax identifier (GSTIN).
PAN number.
Vendor type (Individual, HUF, Company, Firm, Government, Other).
TDS applicability flag and optional TDS section override.
Vendor address.
Payment terms.
Default currency.
Vendor status.
Vendor invoice history.
Vendor spending summary.
Vendor TDS summary (annual aggregate per section, for threshold evaluation).
Validation
Validate required fields.
Validate email format.
Validate unique identifiers where applicable within an organization.
Validate GSTIN format and PAN format where supplied.
Prevent accidental duplicate vendors where practical.
Avoid exposing sensitive payment information.
Encrypt GSTIN and PAN at rest and mask them in all list and detail responses.

TDS-relevant vendor data is mandatory, not optional.

The applicable TDS rate is a function of the section, the vendor type, and whether a verified PAN is
quoted. A vendor with no PAN can attract a substantially higher rate under section 206AA. Without
VendorType, PanNumber, PanVerified, IsTdsApplicable and TdsSectionOverride, TDS cannot be computed
correctly, so these fields are prerequisites of the TDS decision.
Suggested entities
Vendor
VendorContact
VendorAddress
API endpoints

GET /api/v1/vendors

GET /api/v1/vendors/{id}

POST /api/v1/vendors

PUT /api/v1/vendors/{id}

DELETE /api/v1/vendors/{id}

GET /api/v1/vendors/{id}/invoices

MODULE 4 — INVOICE MANAGEMENT

This is the core business module.

Invoice lifecycle

Use a documented state machine.

Initial suggested states:

Draft
Uploaded
Processing
PendingReview
ReadyForApproval
PendingApproval
Approved
Rejected
OnHold
PartiallyPaid
Paid
Cancelled
Failed

PartiallyPaid is required because payment is in scope: an Approved invoice may receive several payments before the balance is cleared. Approved transitions to PartiallyPaid or directly to Paid, and PartiallyPaid advances to Paid when the balance clears.

Define valid state transitions.

Do not allow arbitrary status changes through generic update endpoints.

Invoice features
Create invoice manually.
Upload invoice PDF.
Edit invoice.
View invoice.
Delete or archive draft invoices.
Submit invoice for review.
Search invoices.
Filter by vendor.
Filter by amount.
Filter by status.
Filter by date.
Sort invoice lists.
Pagination.
View invoice history.
View invoice line items.
Attach documents.
View approval history.
View AI analysis.
Export invoice data.
Invoice properties
InvoiceId
OrganizationId
VendorId
InvoiceNumber
InvoiceDate
DueDate
CurrencyCode
Subtotal
LineTaxAmount
HeaderTaxAmount
TaxAmount
LineDiscountAmount
HeaderDiscountAmount
DiscountAmount
TotalAmount
PaidAmount
OutstandingAmount
TdsAmount
NetPayableAmount
IsTdsApplicable
EffectiveTdsMode
EffectiveTaxMode
EffectiveDiscountMode
Status
Description
CreatedBy
CreatedAt
UpdatedAt
RowVersion
Invoice line items

Properties:

LineItemId
InvoiceId
Description
Quantity
UnitPrice
HsnSacCode
GstRateId
GstRateEffectiveFrom
TaxRatePercent
TaxAmount
DiscountAmount
LineTotal
AccountCode
CostCenter
RowVersion

GST rate source is validated against an HSN/SAC reference table.

HsnSacCode is required on every line item and must resolve to an active row in the GST_RATES reference table whose effective window contains the invoice date. The rate is NOT free-entry.

An unresolvable HSN/SAC code must be rejected with a validation error. It must never be silently treated as zero tax, because that would understate tax and be invisible in the UI.

The resolved rate must be snapshotted onto the line item (TaxRatePercent, GstRateId, GstRateEffectiveFrom) at entry time. GST rates are revised periodically, so an invoice must record which rate table it was priced against; otherwise a future rate update would retroactively change historical invoices and break reconciliation. Rate validity is date-scoped and resolved using the invoice date, not today, so back-dated invoices resolve correctly.

When a rate is revised, the existing GST_RATES row gets an EffectiveTo date rather than being deleted, preserving history.

TDS properties

TDS is in scope for this system. TDS deductions are first-class, append-only financial records.

TdsDeductionId
OrganizationId
InvoiceId
PaymentId
SectionId
SectionCode
BaseAmount
RatePercent
TdsAmount
Status
ChallanNumber
DepositDate
ComputationBasis
OverrideReason
IdempotencyKey
ComputedBy
ComputedAt
RowVersion

TDS status values: Computed, Deposited, Voided.

Reference tables:

TdsSection: SectionCode (194C, 194J, 194H, 194I, 206AA and so on), Description, NatureOfPayment, RequiresPan, ThresholdAmount, CurrencyCode, IsActive.
TdsRateRule: SectionId, VendorType, PanQuoted, RatePercent, EffectiveFrom, EffectiveTo.

TDS computation rules:

Rate is resolved from the tuple (SectionCode, VendorType, PanQuoted) against TdsRateRule. The vendor's TdsSectionOverride selects the section; when null the section is derived from the configured nature of payment.

When a section RequiresPan and the vendor has no verified PAN, section 206AA applies with a substantially higher rate. This must be modelled as a distinct rate rule with PanQuoted false, not as a special-case conditional buried in a service.

TDS applies only when the vendor's annual aggregate for that section exceeds the section ThresholdAmount. The aggregate is computed across the organization's invoices for the financial year, so it must be a query, not a per-invoice check.

RatePercent, SectionCode and the computation basis are snapshotted onto each deduction so it remains reproducible and auditable against the rule in force on its date.

TDS deductions are append-only. A wrong deduction is Voided and a new one computed. Never edited in place, never deleted.

TDS computation requires an IdempotencyKey, like payments and adjustments.

Manual override (ComputationBasis ManualOverride) is permitted for genuine edge cases but requires an OverrideReason and is audited.

Out of scope and documented as such rather than half-implemented: filing returns, generating Form 16A, and producing CHALLAN 280 filings. The system computes, records and reports the deduction only.

Payment properties

Payment is in scope for this system. Payments are first-class records, not a status flag.

PaymentId
OrganizationId
InvoiceId
Amount
CurrencyCode
PaymentDate
PaymentMethod
ReferenceNumber
Status
Notes
RecordedBy
RecordedAt
IdempotencyKey
RowVersion

Payment status values: Pending, Cleared, Reversed, Voided.

Only Cleared payments count toward PaidAmount. Reversed and Voided payments never count and are never deleted — a reversal is a new append-only record, auditable in the same way an adjustment undo is.

Payment methods should reflect the INR base currency: NEFT, RTGS, UPI, Cheque, Card, Other.
Financial rules
Use decimal arithmetic.
Define currency precision and rounding policies.
Validate totals on the backend.
Never trust frontend-calculated totals.
Keep historical financial changes traceable.
Taxes and discounts are configurable at both levels — PerLine, InvoiceLevel, or Both — driven by organization settings with optional per-invoice override, and the effective mode is snapshotted onto the invoice. The reconciliation rule is selected from the effective mode:

PerLine: HeaderTaxAmount must be zero and TaxAmount must equal the sum of line tax amounts.
InvoiceLevel: every line tax amount must be zero and TaxAmount must equal HeaderTaxAmount.
Both: TaxAmount must equal the sum of line tax amounts plus HeaderTaxAmount.
The same three-way rule applies to discounts.
Validate that line item totals reconcile with invoice totals.
All derived amounts — TaxAmount, DiscountAmount, TotalAmount, PaidAmount, OutstandingAmount — are computed and validated server-side. The API must reject any request that attempts to set them directly.
PaidAmount is derived from the sum of Cleared payments and is stored only for read performance, recomputed inside the same transaction as every payment write. If stored and derived ever disagree, derived wins and the discrepancy is logged as an invariant violation.
Overpayment beyond TotalAmount is rejected by a business rule unless explicitly permitted.
Payment recording requires an IdempotencyKey, exactly like adjustments, so a retried request cannot double-count money.

Invoice number uniqueness is strict. Enforce UNIQUE on (OrganizationId, VendorId, InvoiceNumber). InvoiceDate is deliberately excluded from the key, because including it would allow the same invoice number to be entered twice on different dates, which is exactly the duplicate-payment failure the constraint exists to prevent. Deterministic duplicate detection in Module 8 runs additionally and surfaces near-matches with explanations.

GST rate validation is mandatory. Every line item HsnSacCode must resolve to an active GST_RATES row valid for the invoice date, and the resolved rate is snapshotted onto the line item. Unresolvable codes are rejected, never treated as zero tax.

TdsAmount is derived from the sum of non-voided TDS deductions and NetPayableAmount equals TotalAmount minus TdsAmount minus PaidAmount. Both are computed and validated server-side and cannot be set by clients.
API endpoints

GET /api/v1/invoices

GET /api/v1/invoices/{id}

POST /api/v1/invoices

PUT /api/v1/invoices/{id}

POST /api/v1/invoices/{id}/submit

POST /api/v1/invoices/{id}/hold

POST /api/v1/invoices/{id}/cancel

GET /api/v1/invoices/{id}/history

GET /api/v1/invoices/{id}/line-items

POST /api/v1/invoices/{id}/line-items

PUT /api/v1/invoices/{id}/line-items/{lineItemId}

POST /api/v1/invoices/{id}/payments

GET /api/v1/invoices/{id}/payments

POST /api/v1/payments/{id}/reverse

POST /api/v1/invoices/{id}/tds/compute

GET /api/v1/invoices/{id}/tds

POST /api/v1/tds/{id}/void

POST /api/v1/tds/{id}/mark-deposited

GET /api/v1/gst-rates?code={hsnSac}&date={invoiceDate}

GET /api/v1/organization/settings

PUT /api/v1/organization/settings

MODULE 5 — FINANCIAL ADJUSTMENTS ENGINE

This is one of the most important engineering modules.

Do not implement this as simple invoice amount updates.

Build a financial adjustment engine with traceability.

Features
Increase line item amount.
Decrease line item amount.
Apply percentage-based adjustment.
Apply fixed-amount adjustment.
Add adjustment reason.
Add adjustment comments.
Record actor information.
Maintain adjustment history.
Undo an adjustment.
Redo an adjustment.
Display active adjustment state.
Display adjustment timeline.
Prevent unauthorized adjustment.
Prevent prohibited negative balances.
Handle concurrent requests.
Adjustment entity

Suggested properties:

AdjustmentId
OrganizationId
InvoiceId
InvoiceLineItemId
ParentAdjustmentId
AdjustmentType
AdjustmentAmount
PreviousAmount
NewAmount
Reason
CreatedBy
CreatedAt
Status
IdempotencyKey
RowVersion
Adjustment history

Suggested properties:

HistoryId
AdjustmentId
Operation
PreviousState
NewState
UndoSnapshot
ActorId
CreatedAt

Define the exact meaning of undo and redo.

Financial history must not silently disappear.

Prefer append-only financial events or compensating adjustment entries, with an explicit representation of active/inactive adjustment state.

Any undo/redo operation must itself be auditable.

Example

Original invoice line amount: 1,000.

Operations:

Increase by 100.
Increase by 500.
Decrease by 200.
Undo.
Undo.
Redo.

Expected state must be derived from a well-defined operation history.

Write automated tests for this entire sequence.

Concurrency

Support simultaneous adjustment requests.

Implement:

EF Core concurrency token using SQL Server RowVersion.
Database transaction.
Appropriate transaction isolation.
Conflict detection.
Meaningful conflict response.
Idempotency protection.

Do not blindly retry a financial operation when its outcome is unknown.

Ensure duplicate submissions cannot produce duplicate financial adjustments.

Acceptance criteria
All adjustment operations are traceable.
Concurrent updates cannot silently overwrite one another.
Duplicate requests do not apply an adjustment twice.
Undo and redo are deterministic.
Financial invariants remain valid.
Tests cover failure and concurrency scenarios.
MODULE 6 — APPROVAL WORKFLOW ENGINE

Implement configurable invoice approval.

Workflow features
Create approval workflow.
Configure approval stages.
Assign approvers.
Sequential approvals.
Parallel approvals.
Amount-based approval rules.
Department-based assignment where configured.
Approve invoice.
Reject invoice.
Request changes.
Add comments.
Reassign approval.
Escalate overdue approvals.
View workflow history.
View pending tasks.
Example workflow

For invoices below 10,000:

One Finance Manager approval.

For invoices between 10,000 and 100,000:

Finance Manager.
Department Manager.

For invoices above 100,000:

Finance Manager.
Department Manager.
Organization Administrator or configured senior approver.

These amounts are configurable examples, not hardcoded financial rules.

Suggested entities
ApprovalWorkflow
ApprovalWorkflowVersion
ApprovalStage
ApprovalAssignment
ApprovalRequest
ApprovalAction
ApprovalComment
Business rules
Only assigned approvers can approve their tasks.
An approver cannot approve an unauthorized invoice.
A completed stage cannot be completed again.
Rejected invoices must follow a documented resubmission process.
Approval decisions must be audited.
Workflow definitions must be versioned so changes do not unexpectedly alter active requests.
Approval transitions must be transactional.
API endpoints

GET /api/v1/approvals

GET /api/v1/approvals/{id}

POST /api/v1/approvals/{id}/approve

POST /api/v1/approvals/{id}/reject

POST /api/v1/approvals/{id}/request-changes

GET /api/v1/approvals/my-tasks

GET /api/v1/approvals/{id}/history

MODULE 7 — DOCUMENT MANAGEMENT AND PROCESSING

Implement secure invoice document handling.

Features
Upload PDF.
Upload image.
Validate file type.
Validate file size.
Generate file hash.
Store document metadata.
Store document in file storage.
Track processing status.
Extract text.
Preview document.
Download authorized document.
Retry failed processing.
View extraction history.
Document lifecycle

Uploaded → PendingProcessing → Processing → Extracted → AwaitingReview → Completed.

Additional states:

Failed
Rejected
Quarantined
Security
Do not trust the supplied file extension.
Validate actual content format.
Apply upload size limits.
Generate server-side storage names.
Prevent path traversal.
Prevent unauthorized downloads.
Do not execute uploaded content.
Treat PDF contents as untrusted input.
Do not expose internal storage paths.

Where feasible, implement malware scanning as a pluggable security integration.

Suggested entities
Document
DocumentProcessingJob
DocumentExtractionResult
DocumentVersion
MODULE 8 — AI INVOICE INTELLIGENCE

This is a major differentiating feature.

Build a provider-independent AI module.

AI must assist financial employees, not independently authorize financial transactions.

8.1 Invoice data extraction

When a PDF is uploaded:

Validate the document.
Extract text.
Send relevant content to the configured extraction provider.
Request structured output.
Parse the result into a typed schema.
Validate extracted values.
Store extracted information.
Display confidence and validation warnings.
Allow a human to review and correct the result.

Extract:

Vendor name.
Invoice number.
Invoice date.
Due date.
Currency.
Subtotal.
Tax.
Discount.
Total amount.
Purchase order number.
Line items.
HSN or SAC code per line item, where present on the document.
Payment terms.

HSN and SAC codes interact with the GST reference-table requirement.

Because every line item requires an HSN/SAC code validated against the GST_RATES reference table, AI extraction must never insert an unvalidated code. The rule is:

Extract the HSN/SAC code where the document shows one, and validate it against GST_RATES for the invoice date.

If the code resolves, present it as a suggested value with its resolved GST rate so the reviewer sees the tax consequence of the code, not just the code.

If the code does not resolve, or is absent from the document, mark the line item as requiring manual HSN/SAC entry and block finalization. Do not guess a code, and do not default to zero tax. A confidently wrong HSN code produces confidently wrong tax, which is worse than a visible gap.

This is a specific instance of the general rule that AI output is advisory: the model proposes, the reference table validates, and a human confirms before the value becomes financial truth.

Do not treat model-generated confidence scores as statistically calibrated probabilities unless they have been independently evaluated.

Preserve references to the source document and, where possible, page numbers or text locations.

8.2 AI invoice summary

Generate summaries including:

Vendor.
Invoice number.
Invoice amount.
Due date.
Key line items.
Tax details.
Payment terms.
Missing information.
Potential inconsistencies.
Important review notes.

Clearly distinguish extracted facts from model-generated interpretation.

8.3 AI document assistant

Allow users to ask questions about authorized invoice documents.

Example questions:

What is the total amount?
What are the payment terms?
Which line item has the highest value?
Is the invoice missing a purchase order?
What are the main tax details?
Summarize this invoice.
Compare these two invoices.
What information needs manual review?

Use RAG.

8.4 RAG architecture

Implement:

Document text extraction.
Text normalization.
Chunking.
Embedding generation.
Vector storage in Qdrant.
Tenant-aware retrieval.
Invoice-level authorization.
Relevant context construction.
LLM answer generation.
Source citation.
Response validation.

Never retrieve documents from another tenant.

Every retrieved document must be checked against the current user's authorization.

8.5 Prompt injection defense

Uploaded documents are untrusted data.

A document may contain text attempting to manipulate the AI system.

Therefore:

Treat document text as data, not system instructions.
Never let document contents override system policies.
Do not execute instructions found inside invoices.
Do not expose secrets.
Restrict tool access.
Validate structured output.
Require authorization for every tool call.
Log AI actions safely.
Prevent cross-tenant retrieval.
Evaluate prompt injection resistance.
8.6 AI suggested actions

The assistant may suggest:

Flag an invoice for review.
Request missing information.
Recommend an adjustment.
Suggest a vendor match.
Suggest a duplicate match.
Recommend approval routing.

However:

AI must never automatically execute financial adjustments, approve invoices, change payment information or initiate payments.

A human must review and explicitly confirm any permitted business action.

8.7 Duplicate detection

Combine:

Exact invoice number match.
Vendor match.
Amount match.
Date similarity.
Document hash.
Optional semantic similarity.

Do not rely solely on AI to determine duplicates.

Display possible duplicate matches with an explanation.

Relationship to the strict invoice-number constraint:

The database enforces UNIQUE on (OrganizationId, VendorId, InvoiceNumber), so an exact repeat for the same vendor is already prevented at write time and must surface as a clear conflict response, not a raw database error. Duplicate detection is still required and is not redundant: it catches the cases a unique index cannot, such as the same invoice entered against a different vendor record, a mistyped or off-by-one number, the same amount and date with a slightly different number, or a re-upload of the same document under a new number. Detection is a reviewer-facing advisory control; the constraint is a hard integrity control. Both are needed and neither replaces the other.

8.8 AI anomaly detection

Identify possible issues such as:

Unusually large invoice.
Repeated invoice number.
Unexpected amount variation.
Unusual vendor activity.
Missing purchase order.
Unusual line item distribution.

Use deterministic business rules first, with AI providing supplementary explanations.

Do not claim that AI has definitively detected fraud.

MODULE 9 — NOTIFICATION SYSTEM

Implement an extensible notification module.

Features
In-app notifications.
Invoice submission notifications.
Approval assignment notifications.
Approval decision notifications.
Rejection notifications.
Due-date reminders.
Failed document-processing notifications.
AI review warnings.
Technical requirements
RabbitMQ-based asynchronous processing where appropriate.
Notification templates.
Delivery status.
Retry handling.
Dead-letter handling.
Duplicate notification prevention.
User notification preferences.

Start with in-app notifications.

Email can initially use a development SMTP service or a mock provider.

MODULE 10 — BACKGROUND PROCESSING AND DISTRIBUTED SYSTEMS

Introduce this after the core business workflows are working.

Background tasks
Invoice document extraction.
AI summarization.
Embedding generation.
Invoice reminders.
Notification delivery.
Analytics aggregation.
RabbitMQ

Implement:

Exchange configuration.
Queues.
Routing keys.
Message contracts.
Consumer registration.
Acknowledgment.
Retry policy.
Dead-letter queues.
Idempotent processing.
Transactional Outbox Pattern

Implement the outbox pattern for reliable event publishing.

Example:

When an invoice is submitted:

Validate the invoice.
Update invoice status.
Insert an outbox event.
Commit both changes in one database transaction.
Background publisher reads unpublished events.
Publisher sends the event to RabbitMQ.
Publisher marks the event as published.

Explain the difference between:

Database transactions.
Message publishing.
At-least-once delivery.
Exactly-once processing claims.
Idempotent consumers.

Do not claim end-to-end exactly-once processing merely because the broker uses acknowledgments.

Redis

Use Redis for justified use cases such as:

Caching frequently accessed reference data.
Dashboard caching.
Distributed rate limiting if configured.
Short-lived coordination where needed.

Avoid caching sensitive financial data without a clear tenant and authorization-aware cache-key strategy.

MODULE 11 — ANALYTICS AND REPORTING

Build a financial dashboard.

Dashboard metrics
Total invoices.
Total invoice amount.
Pending invoices.
Approved invoices.
Rejected invoices.
Overdue invoices.
Pending approval amount.
Monthly invoice volume.
Monthly spending.
Vendor-wise spending.
Invoice processing time.
Average approval time.
Reports
Invoice report.
Vendor report.
Approval report.
Monthly expense report.
Pending invoice report.
Adjustment history report.
Features
Date range filters.
Vendor filters.
Status filters.
Pagination.
Export CSV.
Proper timezone handling.
Organization-specific currency formatting.

Implement aggregation in the backend.

Do not download all records to the browser just to calculate dashboard metrics.

Document the difference between live calculations and cached/materialized analytics.

MODULE 12 — AUDIT LOGGING

Implement an audit trail for important actions.

Record:

User.
Organization.
Action.
Entity type.
Entity ID.
Timestamp.
Correlation ID.
Relevant change information.
Operation outcome.

Audit important events such as:

Login and logout.
User invitations.
Role changes.
Invoice creation.
Invoice updates.
Adjustments.
Undo/redo.
Approval decisions.
Document uploads.
AI recommendations.
AI action confirmations.

Do not record passwords, tokens, API keys, full sensitive documents or unnecessary personal information.

Use append-only audit behavior at the application level, with appropriate database permissions and integrity controls.

PART 6 — FRONTEND APPLICATION

Build a polished enterprise dashboard.

The UI must look like a modern professional SaaS application.

Avoid generic, unstyled CRUD screens.

6.1 Design direction
Clean enterprise interface.
Light theme by default.
Optional dark theme.
Left sidebar.
Top navigation.
Consistent spacing.
Professional typography.
Responsive design.
Clear financial KPI cards.
Data tables.
Status badges.
Charts.
Drawer/dialog-based editing where appropriate.

Use a consistent design system.

6.2 Required pages
Authentication
Login.
Registration.
Forgot password.
Reset password.
Dashboard
Financial KPIs.
Spending chart.
Pending approvals.
Recent invoices.
AI alerts.
Processing statistics.
Invoices
Invoice list.
Invoice detail.
Create invoice.
Edit invoice.
Upload invoice.
Invoice document preview.
Invoice line item editor.
Invoice history.
Payments
Record payment against an approved invoice.
Payment list per invoice with status.
Reverse or void a payment.
Outstanding balance display.
TDS
TDS deduction list per invoice.
Trigger TDS computation with resolved section and rate shown.
Void a TDS deduction with a required reason.
Mark deduction deposited with challan reference.
Net payable amount display.
HSN/SAC code picker with validated GST rate lookup on the line item editor.
Adjustments
Adjustment list.
Create adjustment.
Adjustment detail.
Adjustment timeline.
Undo/redo controls.
Conflict handling.
Approvals
My pending tasks.
Approval details.
Approve/reject.
Approval comments.
Workflow configuration.
Approval history.
Vendors
Vendor list.
Vendor details.
Vendor create/edit.
Vendor invoice history.
AI Assistant
Invoice summary.
Chat interface.
Suggested questions.
Source citations.
AI review warnings.
Suggested actions.
Human confirmation interface.
Organization administration
Organization settings.
Financial configuration (tax mode, discount mode, rounding policies, default currency INR, default tax rate).
Members.
Invitations.
Roles.
Permissions.
Reports
Financial analytics.
Vendor reports.
Invoice reports.
Adjustment reports.
Audit
Audit log table.
Filters.
Event details.
Settings
Profile.
Notification preferences.
Organization settings.
Theme settings.
6.3 Frontend engineering requirements
Strict TypeScript.
Reusable components.
Feature-based organization.
Centralized API client.
Typed API responses.
Typed forms.
Consistent error handling.
Loading skeletons.
Empty states.
Optimistic updates only where safe.
Query invalidation after mutations.
Route protection.
Accessibility.
Error boundaries.

Do not allow frontend state to become the source of truth for financial transactions.

PART 7 — DATABASE DESIGN REQUIREMENTS

Design a relational schema.

Initial entities include:

Identity
Users
Roles
Permissions
UserRoles
RolePermissions
RefreshTokens
UserSessions
Organizations
Organizations
OrganizationMembers
OrganizationSettings
OrganizationInvitations
Vendors
Vendors
VendorContacts
VendorAddresses
Invoices
Invoices
InvoiceLineItems
InvoiceDocuments
InvoiceStatusHistory
Payments
Tds
TdsSections
TdsRateRules
TdsDeductions
GstRates
Adjustments
Adjustments
AdjustmentHistory
AdjustmentOperationHistory
Approvals
ApprovalWorkflows
ApprovalWorkflowVersions
ApprovalStages
ApprovalRequests
ApprovalAssignments
ApprovalActions
ApprovalComments
Documents
Documents
DocumentVersions
DocumentProcessingJobs
DocumentExtractionResults
AI
AIAnalysis
AIConversations
AIConversationMessages
AIEmbeddingReferences
AIRecommendations
AIActionRequests
AIEvaluationResults
Notifications
Notifications
NotificationPreferences
NotificationDeliveryAttempts
Infrastructure
AuditLogs
OutboxMessages
IdempotencyRecords

The final schema may be adjusted based on actual domain requirements.

Database design rules
Use primary keys and foreign keys.
Define unique constraints.
Define required indexes.
Add tenant-scoped indexes.
Use RowVersion where concurrency requires it.
Use appropriate delete behaviors.
Avoid unnecessary cascading deletes for financial records.
Document monetary precision.
Document timestamp and timezone conventions.
Use migrations for schema changes.
Avoid storing large binary files in the database.
Design queries based on expected access patterns.

Before creating entities, produce an ER diagram and explain important relationships.

PART 8 — API DESIGN STANDARDS

Use RESTful API conventions.

Example:

GET /api/v1/invoices

POST /api/v1/invoices

GET /api/v1/invoices/{id}

PUT /api/v1/invoices/{id}

POST /api/v1/invoices/{id}/submit

POST /api/v1/invoices/{id}/adjustments

POST /api/v1/adjustments/{id}/undo

POST /api/v1/adjustments/{id}/redo

POST /api/v1/invoices/{id}/ai-summary

POST /api/v1/invoices/{id}/ai-questions

API requirements
Consistent HTTP status codes.
Request DTOs.
Response DTOs.
Input validation.
Pagination.
Sorting.
Filtering.
Consistent error format.
Correlation IDs.
CancellationToken.
API versioning.
OpenAPI documentation.
Authorization.
Rate limiting where appropriate.

Use Problem Details for standardized API errors.

Do not expose EF Core entities directly as API contracts.

Use explicit commands for financial state transitions rather than generic update operations.

PART 9 — SECURITY REQUIREMENTS

Security is mandatory throughout development.

Implement and test:

Authentication.
Authorization.
Tenant isolation.
Input validation.
Output encoding.
SQL injection prevention.
Secure file upload.
Rate limiting.
Secure password handling.
Refresh token protection.
CSRF protection where cookies are used.
CORS configuration.
Secrets management.
Sensitive logging prevention.
AI prompt injection defense.
AI retrieval authorization.
Audit logging.
Dependency vulnerability checks.

Never commit secrets.

Use environment variables and development secrets.

Provide .env.example containing placeholder values only.

Do not disable certificate validation or security middleware just to make local development easier.

PART 10 — OBSERVABILITY AND PRODUCTION ENGINEERING

Implement:

Logging
Structured logs.
Log levels.
Correlation IDs.
Exception logging.
Background job logging.
Metrics
API request count.
Request duration.
Error rate.
Background job success/failure.
Message processing duration.
AI request duration.
AI token usage where available.
Tracing
HTTP request traces.
Database operation traces where appropriate.
RabbitMQ processing traces.
AI provider call traces.
Correlation across background processing.
Health checks
API health.
SQL Server health.
Redis health.
RabbitMQ health.
Qdrant health.
File storage health.

Do not expose sensitive diagnostic details through public health endpoints.

PART 11 — REQUIRED PHASE-WISE IMPLEMENTATION PLAN

THIS SECTION IS EXTREMELY IMPORTANT.

You must develop the application in the following phases.

Do not implement all phases in one response.

Do not skip phases.

Do not start the next phase until the current phase passes its completion criteria or the developer explicitly approves a documented exception.

For each phase, follow this exact sequence:

Explain the business objective.
Explain the technical concepts.
Show the architecture.
Explain the database changes.
Define API contracts.
Create an implementation plan.
Implement the backend.
Implement the frontend when applicable.
Write tests.
Explain the code.
Demonstrate the feature.
Provide manual testing instructions.
Provide interview questions.
Provide a completion checklist.
Wait for developer approval.
PHASE 0 — PROJECT DISCOVERY AND ARCHITECTURE

Objective: Design the application before coding.

Tasks:

Inspect the installed SDKs and tools.
Verify .NET SDK.
Verify Node.js.
Verify Podman (and podman-compose).
Check SQL Server availability.
Check Git.
Recommend compatible package versions.
Finalize modular monolith architecture.
Define module responsibilities.
Define dependency rules.
Define initial database schema.
Draw system architecture.
Draw ER diagram.
Define tenant isolation strategy.
Define authentication strategy.
Define error-handling strategy.
Define logging strategy.
Define development environment.

Create:

docs/architecture/system-overview.md
docs/architecture/module-boundaries.md
docs/architecture/dependency-rules.md
docs/database/initial-er-diagram.md
docs/security/security-overview.md
docs/adr/ADR-001-modular-monolith.md
docs/learning/phase-00-learning-guide.md

Teaching topics:

Monolith vs modular monolith vs microservices.
Clean Architecture.
Dependency inversion.
DDD basics.
Layer responsibilities.
API architecture.
Multi-tenancy.
Database normalization.
ADRs.

Completion gate:

Architecture reviewed.
ER diagram reviewed.
Technology choices documented.
No application business features implemented yet.

Wait for approval.

PHASE 1 — DEVELOPMENT ENVIRONMENT AND SOLUTION SETUP

Objective: Create a working development environment.

Tasks:

Create Git repository.
Create .NET solution.
Create backend projects.
Create React + TypeScript application.
Configure SQL Server in a Podman container/pod.
Configure Redis.
Create the Podman compose file (podman-compose.yaml) or Quadlet pod definitions.
Configure environment variables.
Configure development settings.
Add health checks.
Configure logging.
Configure OpenAPI.
Create initial test projects.
Create the first EF Core migration with an empty baseline.
Create a seed-data script for local development.
Verify the API, frontend, SQL Server, and Redis all start together with podman-compose up (or podman play kube / podman pod start).

Create:

docs/learning/phase-01-learning-guide.md
.github/workflows/ci.yml (initial skeleton)
README.md with setup instructions

Teaching topics:

Solution and project references.
Clean Architecture dependency direction.
Dependency injection lifetimes (Singleton, Scoped, Transient).
Configuration and the options pattern.
Middleware pipeline.
Health checks and liveness vs readiness.
Environment-based configuration and secrets.

Completion gate:

API returns a healthy response.
Swagger is reachable.
Frontend renders and calls the API successfully.
Podman brings up all infrastructure (SQL Server, Redis, RabbitMQ, Qdrant) as pods.
Tests run and pass (even if trivial).
Developer can run everything locally from the README alone.

Wait for approval.

PHASE 2 — IDENTITY AND AUTHENTICATION

Objective: Implement secure registration, login, roles, and permissions.

Tasks:

Implement Identity entities and migrations.
Implement password hashing through ASP.NET Core Identity.
Implement registration and login endpoints.
Implement JWT access tokens with short lifetimes.
Implement refresh token rotation and revocation.
Implement logout and session revocation.
Implement password reset flow (mock email provider initially).
Implement role and permission seeding.
Implement policy-based authorization.
Implement login throttling and rate limiting.
Implement user invitation flow (organization assignment can arrive in Phase 3).
Build login, registration, and password-reset frontend pages.
Build protected route handling and token refresh handling in the frontend.
Write unit tests for token services.
Write integration tests for authentication flows, including expired and revoked tokens.

Create:

docs/learning/phase-02-learning-guide.md
docs/api/auth-api.md

Teaching topics:

Authentication vs authorization.
Password hashing and why storage format matters.
Access tokens vs refresh tokens.
Token rotation and reuse detection.
JWT validation (issuer, audience, expiry).
Claims, roles, and permissions.
Policy-based authorization.
CSRF and cookie security.
Rate limiting and brute-force defense.

Completion gate:

All Module 1 acceptance criteria pass.
Authentication integration tests pass.
Frontend login/logout/refresh flow works end to end.
Security events appear in logs.

Wait for approval.

PHASE 3 — MULTI-TENANT ORGANIZATION MANAGEMENT

Objective: Implement organizations, membership, and tenant isolation.

Tasks:

Implement Organization, OrganizationMember, OrganizationSettings, and OrganizationInvitation entities.
Seed OrganizationSettings with the approved INR defaults: CurrencyCode INR, timezone Asia/Kolkata, TaxMode and DiscountMode defaults, separate TaxRoundingPolicy and AmountRoundingPolicy, and default tax rate.
Implement tenant resolution from authenticated membership.
Implement tenant context propagation through the request pipeline.
Implement tenant-aware query boundaries and verify they cannot be bypassed.
Implement organization registration and settings endpoints.
Implement member invitation, role assignment, and removal.
Implement organization switcher and administration frontend pages.
Write cross-tenant access tests proving Organization A cannot read Organization B data.
Write background-job tenant-context tests.

Create:

docs/learning/phase-03-learning-guide.md
docs/architecture/tenant-isolation.md

Teaching topics:

Multi-tenancy models (shared schema vs separate schema vs separate database).
Tenant isolation as a security boundary, not a convenience.
Global query filters and their limits.
Membership-derived tenant context.
Tenant-aware caching keys.

Completion gate:

All Module 2 acceptance criteria pass.
Cross-tenant API tests pass and are included in CI.
Invitation and membership flow works end to end.

Wait for approval.

PHASE 4 — VENDOR MANAGEMENT

Objective: Implement complete vendor CRUD with validation and search.

Tasks:

Implement Vendor, VendorContact, and VendorAddress entities and migrations.
Implement vendor CRUD endpoints with validation.
Implement GSTIN and PAN format validation, encryption at rest, and masking in all responses.
Implement the TDS-relevant vendor fields: VendorType, PanNumber, PanVerified, IsTdsApplicable, TdsSectionOverride, HigherTdsRateApplicable.
Implement search, filtering, sorting, and pagination.
Implement organization-scoped uniqueness rules.
Implement vendor invoice history and spending summary endpoints.
Build vendor list, detail, and form frontend pages.
Write validation and tenant-isolation tests.
Write tests proving GSTIN and PAN never appear unmasked in any list or detail response.

Note: the vendor TDS annual-aggregate summary endpoint is deferred to Phase 5B, where TDS deductions
are implemented. Only the vendor-side TDS attribute fields are built here.

Create:

docs/learning/phase-04-learning-guide.md

Teaching topics:

DTOs and validation placement.
Pagination strategies (offset vs keyset) and trade-offs.
Index design for search patterns.
Uniqueness within a tenant.

Completion gate:

Module 3 features complete.
Validation and pagination tests pass.
Vendor pages usable end to end.

Wait for approval.

PHASE 5 — INVOICE MANAGEMENT AND STATE MACHINE

Objective: Implement invoices, line items, and the invoice lifecycle state machine.

Tasks:

Implement Invoice and InvoiceLineItem entities with decimal precision and RowVersion.
Implement the invoice state machine with explicitly defined transitions, including PartiallyPaid.
Implement invoice CRUD, line item management, and status transition endpoints.
Implement backend total reconciliation and validation rules for all three tax modes and all three discount modes.
Implement the EffectiveTaxMode and EffectiveDiscountMode snapshot on invoice creation.
Implement Payment entity with Cleared, Pending, Reversed, and Voided statuses, RowVersion, and idempotency key.
Implement payment recording, reversal, and derived PaidAmount and OutstandingAmount recomputation inside the payment transaction.
Implement invoice search, filters, sorting, and pagination.
Implement invoice status history.
Implement the GST_RATES reference table with effective-from/effective-to date scoping, and seed it with a representative set of common HSN and SAC codes.
Implement HSN/SAC validation on line items: code required, must resolve to an active rate row valid for the invoice date, unresolvable codes rejected rather than treated as zero tax.
Implement rate snapshotting onto the line item (TaxRatePercent, GstRateId, GstRateEffectiveFrom) so later rate revisions cannot alter historical invoices.
Implement strict invoice-number uniqueness on (OrganizationId, VendorId, InvoiceNumber) and return a clear conflict error rather than a raw database exception.
Build an HSN/SAC picker with validated rate lookup into the line item editor.
Build invoice list, detail, create/edit, and line item editor frontend pages.
Build the payment recording and payment list UI with outstanding balance display.
Build the Settings → Financial Configuration page exposing TaxMode, DiscountMode, TaxRoundingPolicy, AmountRoundingPolicy, default currency INR, and default tax rate.
Write state machine tests covering every valid and invalid transition, including the Approved to PartiallyPaid to Paid path.
Write concurrency tests for line item updates and for concurrent payment recording.
Write reconciliation tests proving each of the three tax modes and three discount modes validates correctly and rejects the wrong shape.
Write a test proving that changing organization settings after invoice creation does not invalidate existing invoice totals.
Write idempotency tests proving a retried payment request does not double-count money.
Write tests proving an unknown HSN/SAC code is rejected and never defaults to zero tax.
Write a test proving a GST rate revision does not change the stored tax on an existing invoice.
Write a test proving a back-dated invoice resolves its GST rate using the invoice date, not today.
Write a test proving a duplicate invoice number for the same vendor is rejected, and that the same number on a different date is still rejected.

Create:

docs/learning/phase-05-learning-guide.md
docs/architecture/invoice-state-machine.md
docs/architecture/tax-discount-reconciliation.md

Teaching topics:

State machines vs generic status updates.
Decimal arithmetic and rounding policy, and why tax rounding must be separate from amount rounding under Indian GST practice.
Concurrency tokens and optimistic concurrency.
Reconciliation and financial invariants.
Why totals must be computed server-side.
Why configuration must be snapshotted onto records rather than read live.
Derived-but-stored values and how to detect drift.

Completion gate:

All Module 4 acceptance criteria and state machine tests pass.
Invalid transitions are rejected with meaningful errors.
Line item totals reconcile with invoice totals or produce explicit warnings.
All nine tax-mode and discount-mode combinations validate correctly.
Payment recording, reversal, and derived balances behave correctly under concurrency and retry.
Financial Configuration page changes take effect for new invoices without corrupting existing ones.
HSN/SAC validation rejects unknown codes and never silently applies zero tax.
Duplicate invoice numbers are rejected with a meaningful conflict response.

Wait for approval.

PHASE 5B — TDS ENGINE

Objective: Implement tax deducted at source as an append-only, auditable financial ledger.

This phase exists because TDS was confirmed in scope. It is separated from Phase 5 rather than
folded into it because TDS has its own reference-data model, its own rate-resolution rules, and its
own threshold logic, and bundling it into Phase 5 would make that phase too large to review
properly.

Tasks:

Implement TdsSection and TdsRateRule reference entities and seed representative sections (194C, 194J, 194H, 194I) with per-vendor-type and PAN-present/absent rate rules, plus the 206AA higher-rate rules.
Implement the TdsDeduction append-only ledger entity with RowVersion and idempotency key.
Implement rate resolution from the tuple (SectionCode, VendorType, PanQuoted), driven entirely by reference data with no rate literals in application code.
Implement the missing-PAN path so that a section requiring PAN falls through to the 206AA higher rate as a data rule, not a conditional branch.
Implement annual-aggregate threshold evaluation per vendor per section for the financial year, as a query rather than a per-invoice check.
Implement TDS computation, voiding, and mark-deposited endpoints, all idempotency-keyed.
Implement derived TdsAmount and NetPayableAmount recomputation inside the same transaction as every deduction write.
Implement manual override requiring an OverrideReason, with audit logging.
Build the TDS UI: deduction list per invoice, computation trigger showing the resolved section and rate, void with reason, mark-deposited with challan reference, and net payable display.
Write tests for every rate-resolution branch including missing PAN and each vendor type.
Write a test proving the threshold is evaluated on annual aggregate rather than per invoice.
Write tests proving voiding does not delete the original deduction and that recomputation excludes voided rows.
Write idempotency tests proving a retried computation does not double-deduct.
Write a test proving rate and section snapshots make a historical deduction reproducible after a rate change.

Explicitly out of scope and documented as such: return filing, Form 16A generation, and CHALLAN 280
filing. The system computes, records, and reports the deduction only.

Create:

docs/learning/phase-05b-learning-guide.md
docs/architecture/tds-engine.md

Teaching topics:

Append-only ledgers applied a second time, and why the pattern repeats.
Reference-data-driven rules versus hardcoded conditionals, and why rates must not be literals.
Snapshotting for reproducibility across changing regulation.
Threshold evaluation over an aggregate window and the query cost it implies.
Idempotency on a third distinct financial operation.
Why a wrong value is voided rather than edited.

Completion gate:

All rate-resolution branches are covered by tests.
Missing PAN correctly produces the higher rate through data, not code.
Threshold evaluation is correct across multiple invoices in a financial year.
TdsAmount and NetPayableAmount stay consistent under concurrency and retry.
No TDS record is ever edited in place or deleted.

Wait for approval.

PHASE 6 — DOCUMENT MANAGEMENT AND UPLOAD

Objective: Implement secure document upload and storage before AI processing.

Tasks:

Implement Document, DocumentVersion, and DocumentProcessingJob entities.
Implement IFileStorageService with a local filesystem implementation.
Implement secure upload: content-type sniffing, size limits, server-generated names, hash generation.
Implement authorized download and preview endpoints.
Implement document processing status tracking.
Implement manual text extraction pipeline (PDF text extraction) as a synchronous placeholder before async processing exists.
Build upload, preview, and document status frontend UI.
Write security tests: wrong extension, oversized files, path traversal attempts, unauthorized downloads.

Create:

docs/learning/phase-06-learning-guide.md
docs/security/file-upload-security.md

Teaching topics:

Why file extensions and client-declared content types cannot be trusted.
Content sniffing and magic numbers.
Hashing for integrity and deduplication.
Storage abstraction and replaceability.
Authorization on file access.

Completion gate:

Module 7 security requirements verified by tests.
Upload, preview, and download work end to end.
Malicious-input tests pass.

Wait for approval.

PHASE 7 — FINANCIAL ADJUSTMENTS ENGINE

Objective: Build the traceable adjustment engine with undo/redo and concurrency safety.

This is one of the most important phases. Do not rush it.

Tasks:

Implement Adjustment, AdjustmentHistory, and AdjustmentOperationHistory entities.
Define and document the exact semantics of undo and redo.
Implement append-only adjustment events with active/inactive state representation.
Implement adjustment endpoints with idempotency keys.
Implement undo and redo as auditable operations.
Implement EF Core RowVersion concurrency with conflict detection and meaningful conflict responses.
Implement explicit database transactions around adjustment application.
Implement financial invariant checks (no prohibited negative balances, totals reconcile).
Build adjustment list, timeline, and undo/redo frontend UI with conflict handling.
Write the full automated test sequence: increase, increase, decrease, undo, undo, redo — verifying expected state at every step.
Write concurrent adjustment tests using parallel requests against the same line item.
Write idempotency tests proving duplicate submissions apply an adjustment only once.

Create:

docs/learning/phase-07-learning-guide.md
docs/architecture/adjustment-engine.md

Teaching topics:

Append-only financial ledgers vs mutable state.
Compensating transactions.
Idempotency keys and their storage.
Optimistic vs pessimistic concurrency.
Transaction isolation levels in SQL Server.
Lost-update problems and how they occur.
Why unknown-outcome operations must not be blindly retried.

Completion gate:

All Module 5 acceptance criteria pass.
The full undo/redo test sequence passes.
Concurrency and idempotency tests pass reliably under repetition.
Every adjustment is traceable through history.

Wait for approval.

PHASE 8 — APPROVAL WORKFLOW ENGINE

Objective: Implement configurable, versioned approval workflows.

Tasks:

Implement ApprovalWorkflow, ApprovalWorkflowVersion, ApprovalStage, ApprovalRequest, ApprovalAssignment, ApprovalAction, and ApprovalComment entities.
Implement workflow definition and versioning so active requests are unaffected by definition changes.
Implement amount-based stage selection.
Implement sequential and parallel approval semantics.
Implement approve, reject, request-changes, reassign, and comment endpoints.
Implement transactional stage transitions.
Implement pending-task queries.
Build approvals frontend: my tasks, approval detail, decision UI, comments, and workflow configuration.
Write tests for every transition rule, including unauthorized approval attempts and completed-stage replay.

Create:

docs/learning/phase-08-learning-guide.md
docs/architecture/approval-workflow.md

Teaching topics:

Workflow engines and versioned definitions.
Why in-flight requests must pin a workflow version.
Transactional state transitions.
Separation of assignment from decision.
Auditability of approval decisions.

Completion gate:

All Module 6 business rules enforced and tested.
Example tiered workflow (below 10,000 / 10,000–100,000 / above 100,000) works with configurable thresholds.
Rejection and resubmission follow the documented process.

Wait for approval.

PHASE 9 — MESSAGING, OUTBOX, AND BACKGROUND PROCESSING

Objective: Introduce RabbitMQ, the transactional outbox, and background workers.

Tasks:

Add RabbitMQ to the Podman compose/pod configuration.
Define message contracts and exchange/queue topology.
Implement the OutboxMessages table and transactional event publishing.
Implement a background outbox publisher.
Move document processing and text extraction into a background worker consuming messages.
Implement consumer acknowledgment, retry policy, and dead-letter queues.
Implement idempotent consumers with IdempotencyRecords.
Add Redis caching for justified reference-data and dashboard use cases.
Implement processing-status updates visible in the frontend.
Write tests: duplicate message delivery, consumer failure and retry, outbox atomicity (status change and event commit together).

Create:

docs/learning/phase-09-learning-guide.md
docs/architecture/messaging-and-outbox.md

Teaching topics:

Why synchronous processing fails for slow operations.
At-least-once delivery and why exactly-once claims are misleading.
The dual-write problem and how the outbox solves it.
Idempotent consumers.
Dead-letter queues and poison messages.
Retry with exponential backoff and jitter.
Cache invalidation and tenant-aware cache keys.

Completion gate:

Invoice submission produces an outbox event atomically.
Document processing completes asynchronously with visible status.
Duplicate message delivery does not duplicate work.
Dead-letter behavior demonstrated and tested.

Wait for approval.

PHASE 10 — AI INTEGRATION LAYER AND INVOICE EXTRACTION

Objective: Build the provider-independent AI layer and structured invoice extraction.

Tasks:

Define ILLMProvider, IEmbeddingProvider, IDocumentTextExtractor, and IInvoiceExtractionService abstractions.
Implement a configurable OpenAI-compatible provider adapter.
Implement structured output parsing into a typed extraction schema.
Implement HSN/SAC extraction with reference-table validation: extracted codes are validated against GST_RATES for the invoice date and shown with their resolved rate; unresolvable or missing codes are flagged for mandatory manual entry and must block finalization rather than defaulting to zero tax.
Implement the extraction pipeline: validate → extract text → call model → parse → validate values → store results with confidence and warnings.
Implement a mock/fake LLM provider for deterministic tests.
Build the review-and-correct extraction UI showing extracted values, confidence, and validation warnings.
Preserve source references (page numbers or text locations) where available.
Write tests using the fake provider covering valid, malformed, and adversarial model outputs.

Create:

docs/learning/phase-10-learning-guide.md
docs/architecture/ai-integration.md

Teaching topics:

Provider abstraction and why model names belong in configuration.
Structured output and schema validation.
Why model confidence scores are not calibrated probabilities.
Human-in-the-loop design.
Hallucination risks in financial extraction.
Testing AI code with fakes and recorded responses.

Completion gate:

Uploaded PDFs produce structured extraction results.
Malformed model output is rejected safely.
A human can review and correct every extracted field.
All AI tests pass deterministically with the fake provider.

Wait for approval.

PHASE 11 — RAG AND AI DOCUMENT ASSISTANT

Objective: Implement embeddings, vector retrieval, and the invoice Q&A assistant.

Tasks:

Add Qdrant to the Podman compose/pod configuration.
Implement embedding generation and storage for extracted document text.
Implement chunking and normalization strategies, and document the chosen approach.
Implement tenant-aware, authorization-checked retrieval.
Implement context construction, answer generation, and source citation.
Implement the AI conversation entities and chat endpoints.
Build the AI assistant frontend: chat interface, suggested questions, citations, and review warnings.
Implement prompt injection defenses: document text treated as data, restricted tool access, output validation.
Implement human confirmation for any AI-suggested business action.
Write tests: cross-tenant retrieval is impossible, injection attempts cannot override system policy, citations reference only authorized documents.

Create:

docs/learning/phase-11-learning-guide.md
docs/architecture/rag-pipeline.md
docs/security/prompt-injection-defense.md

Teaching topics:

Embeddings and semantic similarity.
Vector databases and similarity search.
Chunking strategies and their effects on retrieval quality.
RAG architecture and context construction.
Prompt injection as a data-vs-instruction problem.
Authorization in retrieval pipelines.
Evaluating AI answer quality.

Completion gate:

Users can ask questions about authorized invoices and receive cited answers.
Cross-tenant retrieval tests pass.
Prompt injection test cases are handled safely.
No AI suggestion can execute a financial action without explicit human confirmation.

Wait for approval.

PHASE 12 — DUPLICATE DETECTION AND ANOMALY DETECTION

Objective: Implement deterministic-first duplicate and anomaly detection with AI explanations.

Tasks:

Implement deterministic duplicate matching: invoice number, vendor, amount, date similarity, document hash.
Implement optional semantic similarity as a supplementary signal.
Implement duplicate review UI with match explanations.
Implement rule-based anomaly detection: unusually large invoices, repeated numbers, amount variation, unusual vendor activity, missing purchase orders.
Add AI-generated explanations as supplementary text only.
Wire duplicate and anomaly signals into notifications and AI review warnings.
Write tests with known duplicate and anomaly fixtures.

Create:

docs/learning/phase-12-learning-guide.md

Teaching topics:

Deterministic rules vs probabilistic models.
Why detection must not rely solely on AI.
Explainability in financial controls.
False positives and reviewer workload.

Completion gate:

Known duplicates are surfaced with explanations.
Anomaly rules fire correctly on fixture data.
AI never labels an invoice as fraud definitively.

Wait for approval.

PHASE 13 — NOTIFICATIONS SYSTEM

Objective: Implement the extensible notification module.

Tasks:

Implement Notification, NotificationPreferences, and NotificationDeliveryAttempt entities.
Implement RabbitMQ-based notification processing.
Implement notification templates and delivery status tracking.
Implement retry handling and dead-letter handling for delivery.
Implement duplicate notification prevention.
Implement in-app notification center frontend with unread state.
Implement email through a mock provider or development SMTP behind an abstraction.
Wire notifications to invoice submission, approval assignment, approval decisions, rejections, due-date reminders, and processing failures.
Write tests for retry, dead-letter, and duplicate-prevention behavior.

Create:

docs/learning/phase-13-learning-guide.md

Teaching topics:

Event-driven notification fan-out.
Delivery guarantees and retries.
Template rendering and provider abstraction.
Notification deduplication strategies.

Completion gate:

All Module 9 features work end to end.
Users receive in-app notifications for the listed events.
Retry and dead-letter behavior verified.

Wait for approval.

PHASE 14 — ANALYTICS, REPORTING, AND AUDIT LOGGING

Objective: Implement the dashboard, reports, and the audit trail.

Tasks:

Implement AuditLogs and audit interception for the listed important events.
Ensure audit records exclude secrets and sensitive document content.
Implement backend aggregation for all dashboard metrics.
Implement report endpoints with date-range, vendor, and status filters, pagination, and CSV export.
Implement timezone-correct and currency-formatted outputs.
Build the dashboard frontend: KPI cards, spending charts, pending approvals, recent invoices, AI alerts, processing statistics.
Build report pages and the audit log page with filters and event details.
Cache dashboard aggregates where justified, and document live vs cached calculations.
Write tests for aggregation correctness against seeded datasets, and audit completeness tests.

Create:

docs/learning/phase-14-learning-guide.md
docs/architecture/analytics-strategy.md

Teaching topics:

Backend aggregation vs client-side computation.
Materialized summaries and cache staleness.
Timezone handling for financial reporting.
Append-only audit design and integrity controls.

Completion gate:

All Module 11 metrics match seeded test data.
Reports export correctly.
Every audited event type produces an audit record.
Audit records contain no secrets.

Wait for approval.

PHASE 15 — SECURITY HARDENING AND OBSERVABILITY

Objective: Harden the application and complete production-grade observability.

Tasks:

Review and test every item in PART 9 with explicit security tests.
Add rate limiting across sensitive endpoints.
Verify CORS, CSRF, cookie, and secrets configuration.
Run dependency vulnerability scans and remediate findings.
Complete OpenTelemetry tracing across HTTP, database, RabbitMQ, and AI provider calls.
Add metrics for API, background jobs, and AI usage.
Add health checks for SQL Server, Redis, RabbitMQ, Qdrant, and file storage.
Verify correlation IDs flow from request through background processing.
Conduct a threat-model review of the AI pipeline and tenant isolation.
Record findings and fixes in docs/security/.

Create:

docs/learning/phase-15-learning-guide.md
docs/security/threat-model.md

Teaching topics:

Threat modeling.
OWASP risks for APIs and LLM applications.
Observability pillars: logs, metrics, traces.
Correlation and causation in distributed systems.

Completion gate:

Every PART 9 item has a passing test or documented mitigation.
Traces show cross-component correlation.
Health endpoints expose no sensitive details.

Wait for approval.

PHASE 16 — PODMAN, CI/CD, AND DEPLOYMENT DEMO

Objective: Containerize, automate, and deploy the portfolio demonstration.

Tasks:

Write production-style OCI-compatible Dockerfiles for API, worker, and frontend, verified with podman build.
Finalize the Podman compose/pod configuration for full-stack local operation.
Complete the GitHub Actions pipeline: build, lint, test, scan, and image build.
Add integration tests to CI using ephemeral dependencies.
Prepare deployment configuration for Azure App Service (or container hosting), Azure SQL, and Azure Blob Storage.
Deploy the demonstration and verify health checks.
Document deployment steps and limitations honestly.

Create:

docs/learning/phase-16-learning-guide.md
docs/deployment/deployment-guide.md

Teaching topics:

Containerization and image layering.
CI/CD pipeline design and gating.
Secrets in CI and deployment environments.
Infrastructure-as-environment thinking.
Honest portfolio claims vs production claims.

Completion gate:

CI passes on every push and pull request.
podman-compose up runs the entire system.
The deployed demo passes health checks.

Wait for approval.

PHASE 17 — FINAL POLISH, DOCUMENTATION, AND PORTFOLIO PREPARATION

Objective: Complete the portfolio-quality presentation of the project.

Tasks:

Review UI consistency across all pages.
Verify loading, error, and empty states everywhere.
Verify accessibility basics: keyboard navigation, focus handling, labels.
Complete the README with architecture diagrams, setup instructions, and screenshots.
Write a project walkthrough document explaining key engineering decisions.
Consolidate all ADRs and learning guides.
Run the full test suite and record coverage for critical financial modules.
Prepare a demo script covering the complete business flow: register organization → invite users → add vendor → upload invoice → AI extraction → review → adjustments with undo/redo → approval → analytics.

Create:

docs/learning/phase-17-learning-guide.md
docs/project-walkthrough.md
DEMO_SCRIPT.md

Completion gate:

A new developer can run the system from the README alone.
All tests pass.
The demo script runs end to end without manual database fixes.
Documentation is complete and honest about scope.

Wait for final developer approval.

PART 12 — TESTING STRATEGY

Testing is not an afterthought. Every phase must include tests.

Test pyramid:

Unit tests: domain rules, state machines, adjustment engine, validation, formatting.
Integration tests: API endpoints against real SQL Server, tenant isolation, authentication, concurrency, messaging.
End-to-end tests: critical user journeys through the frontend with Playwright.
AI tests: deterministic tests with fake providers, plus a small suite of evaluation tests against the real provider that are allowed to be non-strict.

Rules:

The adjustment engine, invoice state machine, approval transitions, and tenant isolation must have the highest test rigor.
Concurrency tests must be run repeatedly to expose flakiness, and flaky tests must be fixed, not deleted.
Never mark a phase complete without its tests passing in CI.
Bug fixes require a regression test that fails before the fix and passes after it.

PART 13 — DOCUMENTATION AND LEARNING REQUIREMENTS

For every phase, produce a learning guide containing:

The business problem being solved.
The engineering concepts introduced.
Why the chosen approach was selected and what alternatives exist.
Diagrams where useful.
Common mistakes and pitfalls.
Interview questions with answers relevant to the phase.
A completion checklist.

Keep docs/architecture/, docs/adr/, and docs/learning/ current as the system evolves.

When a documented decision changes, write a new ADR rather than silently editing history.

PART 14 — DEFINITION OF DONE

A feature is done only when ALL of the following are true:

Backend implementation complete with validation.
Authorization and tenant isolation enforced and tested.
Frontend implementation with loading, error, and empty states.
Unit and integration tests written and passing.
No secrets in source control.
Logging and error handling in place.
API documented in OpenAPI.
Documentation and learning guide updated.
The developer can explain how the feature works and why it was built this way.

PART 15 — RULES FOR THE AI DEVELOPMENT AGENT

Follow these rules strictly.

1. Develop phase by phase. Never implement multiple phases at once.
2. At the start of each phase, restate the objective, concepts, and plan before writing code.
3. Explain every significant engineering decision in plain language.
4. Prefer boring, reliable technology over novelty.
5. Never fake completeness: no placeholder implementations presented as finished.
6. Never weaken security, tenant isolation, or tests to make a phase pass.
7. When uncertain about a requirement, ask the developer instead of guessing.
8. When a mistake is discovered, explain the root cause before fixing it, and record the lesson.
9. Keep the codebase buildable and tests passing at the end of every work session.
10. If a phase's completion gate cannot be met, report the blocker honestly rather than moving ahead.
11. Match explanation depth to the developer's background: skip basics, go deep on architecture, distributed systems, concurrency, and AI engineering.
12. Every financial operation must be traceable, validated on the backend, and covered by tests.
13. AI assists humans; it never autonomously executes financial or approval actions.
14. Treat all uploaded content and all model output as untrusted input.
15. End every phase with the completion checklist and explicitly wait for developer approval before continuing.

FINAL INSTRUCTION

Begin with PHASE 0.

Do not write any application code in Phase 0.

Start by inspecting the development environment, then present the architecture, module boundaries, dependency rules, initial ER diagram, tenant isolation strategy, and technology version recommendations as documented in the Phase 0 task list.

Conclude Phase 0 with the completion checklist, interview questions, and a request for developer approval before starting Phase 1.