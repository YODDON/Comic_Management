# Current Development Conventions

This document distinguishes rules enforced by the current architecture from common patterns that are not yet consistent across the repository. Source code remains the highest source of truth.

## Convention levels

- **REQUIRED**: a boundary confirmed by the current solution/architecture; changing it requires an explicit architecture decision.
- **RECOMMENDED**: a practice that new code should follow to avoid increasing inconsistency.
- **CURRENT PATTERN**: a description of current code that is not necessarily mandatory everywhere.

## Projects and namespaces

- **REQUIRED**: a business service must not reference another business service project. Communicate through REST/gRPC contracts.
- **REQUIRED**: the service that owns data also owns its `DbContext`, migrations, and repositories.
- **CURRENT PATTERN**: each service is one ASP.NET Core project organized with technical folders; there are no separate `.Domain`, `.Application`, or `.Infrastructure` projects.
- **CURRENT PATTERN**: namespaces begin with the project name, for example `ChapterAPI.Services.Chapter`.
- **RECOMMENDED**: class and file names should identify the feature and role, such as `ChapterService`, `IChapterRepository`, or `CreateChapterDto`.

See [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for actual folder placement.

## REST controllers and DTOs

- **REQUIRED**: controllers and gRPC adapters must not access a `DbContext` or repository directly; they call an application/service/use-case abstraction appropriate to the approved architecture.
- **RECOMMENDED**: controllers should handle transport concerns only: routes, model binding, claims, authorization, status codes, and response envelopes.
- **RECOMMENDED**: business validation, orchestration, and mapping belong in the application/service/use-case layer. This may be an application service, use-case class, or an approved command/query handler; it must not live in a transport adapter.
- **RECOMMENDED**: public requests and responses use DTOs/contracts; do not return EF entities when an endpoint contract exists.
- **CURRENT PATTERN**: REST controllers use attribute routing. Most service routes start with `/api/...` and the Gateway exposes shorter public paths through transforms.
- **CURRENT PATTERN**: responses commonly use `SharedKernel.Responses.ApiResponse`, and paging uses SharedKernel types.

Do not rename public routes or JSON contracts merely for naming consistency unless the task explicitly authorizes a breaking change.

## Services, repositories, and dependencies

- **CURRENT REQUIRED**: in the current folder-layered architecture, EF Core access must remain inside repository/data-access components and must not leak into controllers or transport adapters.
- **FUTURE APPROVED EXCEPTION**: if an explicitly approved Clean Architecture/CQRS migration introduces dedicated query infrastructure or a read-side implementation, update this convention together with the code and [DECISIONS.md](DECISIONS.md). The invariant remains: transport layers must not directly access `DbContext`.
- **RECOMMENDED**: the application/service/use-case layer orchestrates persistence abstractions, external integrations, and gRPC clients.
- **RECOMMENDED**: repositories focus on persistence, queries, and local transactions; they do not decide HTTP status codes or read claims.
- **RECOMMENDED**: do not create a generic repository solely to wrap `DbSet` CRUD operations. Prefer domain- or feature-specific abstractions when an abstraction provides real value.
- **CURRENT PATTERN**: service and repository interfaces live under `Interfaces/`; implementations live under `Services/` or `Repositories/`.
- **CURRENT PATTERN**: dependencies are registered in `Program.cs`; the repository has no single extension/assembly-scanning convention across all services.
- **CURRENT PATTERN**: gRPC server transport adapters live under `GrpcServices/`; `.proto` contracts remain under `Protos/`.
- **RECOMMENDED**: use constructor injection; avoid service locators and constructing infrastructure clients inside business code.

## Domain and business rules

- **CURRENT PATTERN**: entities are primarily EF data models with public properties; most invariants live in services, repositories, or helpers such as `WalletRules`.
- **REQUIRED**: do not describe the current repository as complete DDD or compiler-enforced Clean Architecture.
- **RECOMMENDED**: money, state-transition, and idempotency rules should have one clear, testable enforcement point.
- **RECOMMENDED**: do not introduce aggregates, value objects, CQRS handlers, or generic abstractions without a concrete need and the required approval.

## Async/await and cancellation

- **CURRENT PATTERN**: database, HTTP, Cloudinary, and gRPC I/O generally use asynchronous APIs, and many application methods use the `Async` suffix.
- **RECOMMENDED**: do not block with `.Result` or `.Wait()`; propagate `CancellationToken` when the existing contract supports it.
- **RECOMMENDED**: keep one scoped `DbContext` per request; do not make a service singleton when it depends on scoped services.

## Validation and error handling

- **CURRENT PATTERN**: validation is split between model binding/data annotations and service checks; there is no repository-wide validation framework.
- **RECOMMENDED**: validate transport input at the boundary and business invariants in the application/domain layer; repositories must not return HTTP results.
- **CURRENT PATTERN**: the Gateway normalizes selected `401`, `404`, `502`, and `503` responses; services still return errors through their own controllers/response envelopes.
- **RECOMMENDED**: external-dependency failures must be logged with enough safe context and fail according to use-case risk. Do not fail open for authorization, ownership, payment validation, or other critical validation.
- **RECOMMENDED**: do not swallow exceptions in money flows. A failed best-effort compensation must leave an observable state/log that can be investigated.

## Authentication and authorization

- **REQUIRED**: backend components enforce authorization; frontend route guards are only a user-experience mechanism.
- **CURRENT PATTERN**: JWT Bearer uses configured secret, issuer, and audience values. The Gateway authenticates at the edge, and controllers use `[Authorize]`/roles where required.
- **CURRENT PATTERN**: issuer/audience validation is not consistent across all services. Do not treat that inconsistency as a convention.
- **RECOMMENDED**: derive the current user ID from authenticated claims; do not trust a client-supplied `UserId` for an endpoint representing the current user.

## Configuration and secrets

- **REQUIRED**: never commit secrets, database credentials, Cloudinary keys, SMTP credentials, Google client IDs, or SePay keys.
- **CURRENT PATTERN**: configuration uses `appsettings*.json`, environment variables, and `__` for nested keys.
- **RECOMMENDED**: add new keys to the appropriate example file and [DEVELOPMENT.md](DEVELOPMENT.md).
- **RECOMMENDED**: resolve internal endpoints from configuration/environment; do not add new localhost literals to business services.

## Logging and observability

- **CURRENT PATTERN**: applications use default ASP.NET Core logging and local log statements; there is no standardized OpenTelemetry or correlation middleware.
- **RECOMMENDED**: use structured `ILogger` messages with safe business identifiers such as transaction, user, or chapter IDs. Never log tokens, secrets, or passwords.
- **RECOMMENDED**: preserve and forward trace/correlation metadata when available. If a system-wide correlation standard is introduced, update architecture, communication, and decision documentation together.

## gRPC and messaging

- **CURRENT PATTERN**: `.proto` files live in providers and/or are copied into consumers for client generation; service and RPC names use PascalCase.
- **REQUIRED**: a gRPC contract change requires checking the provider, all consumers, DI registration, and endpoint configuration.
- **CURRENT PATTERN**: RabbitMQ is not used by application code, so no runtime event-naming convention currently exists.
- **RECOMMENDED FOR FUTURE EVENTS**: an integration event should be an immutable contract and must not carry an EF/domain entity. Use a past-tense name such as `ChapterReadIntegrationEvent` and include `EventId`, `OccurredAt`, `CorrelationId`, and `Version`. This is guidance for a future approved implementation, not a claim that events currently exist.

## Tests

- **CURRENT PATTERN**: the solution has no test project, and the frontend has no test script.
- **RECOMMENDED**: name a test project `<Project>.Tests`; use behavior-oriented test names such as `Method_WhenCondition_ExpectedResult`, or another convention consistently established by the new test project.
- **RECOMMENDED**: prioritize wallet invariants, payment idempotency/compensation, mission rewards, and purchase entitlement.
- **REQUIRED**: do not report that tests pass when no tests exist or they were not run; state the exact verification command and result.

## Documentation

- **REQUIRED**: source code is the highest source of truth; never document planned work as current behavior.
- **REQUIRED**: after a code change, use the matrix in [AGENTS.md](../AGENTS.md) and update only affected documentation.
- **RECOMMENDED**: each information category has one canonical document; other documents should link to it instead of copying long sections.
