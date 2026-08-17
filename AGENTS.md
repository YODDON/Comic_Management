# Comic Management — AI Operating Manual

This file is the entry point and rulebook for AI coding agents working in this repository. It routes agents to canonical documentation; it does not replace those documents.

## Repository workflow

Before changing code:

1. Read this `AGENTS.md`.
2. Determine the exact task scope and affected services.
3. Read only the relevant canonical documents from the routing table below.
4. Inspect the current source code, configuration, migrations, contracts, and callers in scope.
5. Treat code as authoritative when documentation and implementation disagree.
6. Inspect `git status` and preserve unrelated user changes already present in the working tree.
7. Make a small, explicit implementation plan.
8. Implement the smallest coherent change that satisfies the task.
9. Restore/build/test and smoke-check affected contracts in proportion to the risk and scope.
10. Perform a documentation-impact check and update only documentation actually affected by the change.
11. Inspect `git diff`, verify only intended changes remain, and verify code and documentation agree before finishing.

## Git Branch and Integration Workflow

For coding tasks that will be committed and integrated, **the AI Agent must autonomously execute the required Git commands** (creating branches, committing, and merging to `dev`) using the terminal tool when appropriate, rather than expecting the user to perform these actions manually.

### Before editing

1. Run `git status` and preserve unrelated working-tree changes.
2. Run `git fetch origin`.
3. Determine whether `dev` exists locally or as `origin/dev`.
4. If only `origin/dev` exists, create local `dev` tracking it.
5. If neither exists, create `dev` from the repository's current valid base/default branch and push it to `origin` without rewriting history.
6. Update local `dev` with `git pull --ff-only`. Never force-pull or discard unrelated work.
7. Create a concise, dedicated task branch from `dev` before making task edits, for example `refactor/standardize-project-structure`.

All task changes must remain on the task branch until verification succeeds.

### After implementation

1. Inspect `git diff` and `git status`.
2. Stage only intended files; never stage unrelated user changes, secrets, temporary files, or generated build artifacts.
3. Create a clear task commit and push the task branch to `origin`.
4. Switch to `dev`, update it with `git pull --ff-only`, and merge the completed task branch with a normal merge that preserves task history.
5. Push `dev` to `origin`.

Never force-push, rewrite history, delete unrelated branches, merge automatically into `main`/`master`, bypass branch protection, or guess through ambiguous merge conflicts. If authentication, permissions, branch protection, remote divergence, or merge conflicts block integration, stop the integration step, keep the completed task branch intact and pushed when possible, and report the blocker.

## Source-of-truth order

```text
Actual source code and executable configuration
    ↓
Current architecture documentation in docs/
    ↓
Development conventions and accepted decisions
    ↓
Planning/proposal documents
```

- Never document a planned feature as implemented.
- If implementation is partial, label it `PARTIAL`, `TRANSITIONAL`, or `NOT IMPLEMENTED`.
- Historical or planning documents are non-canonical material and may contain proposed target architecture or refactoring ideas; never use them as authority for `CURRENT` runtime behavior.
- When planning material, canonical documentation, and implementation conflict: inspect the source code, treat source code as authoritative, update affected canonical documentation when necessary, and record a material inconsistency when useful.

## Documentation router

| Need to understand | Canonical document |
|---|---|
| Current system architecture and runtime flows | `docs/ARCHITECTURE.md` |
| Repository/project/folder placement | `docs/PROJECT_STRUCTURE.md` |
| Service responsibility and data ownership | `docs/SERVICE_CATALOG.md` |
| REST, YARP, gRPC, RabbitMQ status | `docs/COMMUNICATION.md` |
| DbContexts, database ownership, constraints, migrations | `docs/DATABASE.md` |
| Existing coding and layering conventions | `docs/CONVENTIONS.md` |
| Setup, build, run, test, ports, troubleshooting | `docs/DEVELOPMENT.md` |
| Accepted/transitional architecture decisions | `docs/DECISIONS.md` |
| Human-facing project overview | `README.md` |

## Confirmed architecture invariants

### CURRENT

- The repository is a monorepo with a React/Vite frontend and a .NET backend solution.
- The frontend calls a YARP API Gateway; Gateway routes to eight business APIs.
- Backend projects target `net8.0`.
- Each business API owns a separate EF Core `DbContext` and migration history.
- Cross-service references are identifiers, not cross-database foreign keys.
- Public client communication is REST/JSON; current internal service calls are synchronous gRPC, with one SocialAPI HTTP validator.
- Controllers and gRPC adapters call application/service abstractions rather than `DbContext` directly.
- Service internals are currently folder-based layered architecture inside one project per service. Clean Architecture project boundaries are not implemented.
- JWT Bearer authentication is configured at the Gateway and business APIs; authorization also exists on controllers.
- RabbitMQ and Redis containers exist in Docker Compose, but application messaging/cache integration is not implemented.
- n8n + LibreTranslate support text translation; Cloudinary, SMTP/Google Auth, and SePay/VietQR are used by specific services.
- CQRS command/query handlers and MediatR-based dispatch are being incrementally rolled out (currently implemented in `BannerAPI`).

### PLANNED OR PROPOSED, NOT CURRENT

- Physical `.API/.Application/.Domain/.Infrastructure` project splits.
- Rich DDD aggregates, Saga, and persisted state machines.
- RabbitMQ integration events, Outbox/Inbox, DLQ, or event correlation envelopes.
- OpenTelemetry-based distributed observability.
- Redis application caching.

Do not introduce these solely because they appear in a proposal or seem useful. Implement them only when the task requests them directly or the user/developer has clearly approved the plan. Otherwise, report or recommend the pattern without implementing it. Implementation documentation must reflect only completed behavior.

## Placement and dependency rules

- Preserve database-per-service ownership. A service must not access another service's database or `DbContext`.
- Do not create cross-service database foreign keys.
- Keep REST controllers thin: binding, claims, transport validation, calling a service, and mapping status/response.
- Keep gRPC server adapters transport-focused. Application behavior belongs in the application/service/use-case layer, not in transport adapters. Depending on the `CURRENT` architecture, this may be implemented as application services, use-case classes, or approved command/query handlers.
- In the `CURRENT` architecture, persistence queries, EF Core transactions, and `DbContext` access belong in repositories/data-access components. If an explicitly approved Clean Architecture/CQRS refactor introduces dedicated query infrastructure, update this rule and `docs/CONVENTIONS.md` together. `DbContext` must never leak into controllers or transport adapters.
- External providers and generated gRPC clients must not be called directly from controllers.
- Do not expose EF entities directly from public REST endpoints; use request/response DTOs.
- Preserve public REST paths, YARP transforms, JWT behavior, gRPC contracts, and frontend response expectations unless a breaking change is explicitly approved.
- Do not add generic repositories, MediatR, CQRS, domain events, or abstractions without a concrete use case; architecture-changing patterns also require direct task authorization or an explicitly approved plan.
- Keep `SharedKernel` technical and stable. Do not move service-owned domain entities into it.
- Preserve unrelated working-tree changes. Inspect `git status` before editing.

## Architecture Drift Prevention

Before creating a new HTTP client abstraction, API response wrapper, repository pattern, validation pattern, error-handling convention, logging abstraction, DI registration pattern, mapping approach, event envelope, messaging abstraction, or authentication helper:

1. Search the repository for an existing equivalent.
2. Reuse the established pattern when it is appropriate.
3. Do not create competing abstractions for the same responsibility.
4. Introduce a new pattern only when the existing pattern cannot satisfy the task or an approved task explicitly replaces it.

Avoid parallel wrappers such as `ApiResponse<T>`, `Result<T>`, and `ServiceResponse<T>` that express the same concept without a deliberate migration plan. Reuse the repository's existing logging and error-response conventions. Do not silently swallow exceptions or use fail-open behavior for authorization, ownership, payment validation, or other security/business-critical validation unless explicitly designed that way. If a public error contract changes, inspect frontend callers.

## Cross-Service Change Rules

Cross-service contract changes are incomplete until both the provider/producer and all known consumers have been inspected.

### gRPC

When changing a `.proto`, gRPC request/response, or service method, inspect:

- the server implementation;
- every generated/client usage and known consumer;
- DI registration;
- configuration/environment variables;
- `docs/COMMUNICATION.md` and `docs/SERVICE_CATALOG.md`.

### REST

When changing a public route, request/response DTO, HTTP status, or authorization behavior, inspect:

- the controller/API implementation;
- the YARP route and path transform;
- frontend callers and relevant frontend service modules;
- `README.md` and canonical docs when public behavior changes.

### RabbitMQ and integration events

If messaging is implemented or changed, inspect the publisher, every consumer, event version, routing configuration, serialization, idempotency/deduplication, and the relevant sections of `docs/COMMUNICATION.md`, `docs/SERVICE_CATALOG.md`, and `docs/ARCHITECTURE.md`. Do not assume RabbitMQ is active merely because its container exists.

### Authentication and claims

When changing a JWT claim, role, policy, or authentication behavior, inspect the ApiGateway, affected APIs, frontend authorization/route guards, and gRPC callers if claims are forwarded.

## Database Change Rules

When changing an EF Core model or schema:

1. Identify the owning microservice.
2. Never modify another service's database/schema to satisfy a local feature.
3. Update entity/domain/persistence configuration in the owner service only.
4. Create an EF Core migration in the owning service when the schema changes.
5. Inspect the generated migration before accepting it.
6. Do not use `EnsureCreated()` as a substitute for migrations.
7. Do not create cross-service foreign keys.
8. Preserve database-per-service ownership.
9. Update `docs/DATABASE.md` when an important ownership, constraint, index, relationship, or invariant changes.

For changes involving a unique index, `ReferenceId`, idempotency, financial transaction, purchase ownership, or mission reward, explicitly inspect duplicate-processing and partial-failure risks.

## Dependency Change Rules

Before adding a NuGet or npm package:

1. Check whether the repository already has a package/library solving the same concern.
2. Establish why the new dependency is needed.
3. Prefer existing platform/framework capability when sufficient.
4. Avoid two libraries for the same responsibility.
5. Inspect licensing, security, and version compatibility when relevant.
6. Update `docs/DEVELOPMENT.md` or `docs/CONVENTIONS.md` only when usage/setup materially changes.

Do not add MediatR, FluentValidation, AutoMapper, Polly, MassTransit, Serilog, OpenTelemetry packages, or another popular library solely because it is common.

## Configuration and Secrets

- Never commit secrets or place real credentials, tokens, passwords, or API keys in code or Markdown.
- Document configuration key names, not secret values.
- Prefer environment variables, user secrets, or secret stores according to existing project conventions.
- When adding a configuration key, inspect the relevant `appsettings*`, Docker Compose, launch configuration, deployment setup, and local setup documentation that may need the same key.

## Verification expectations

Backend changes normally require:

```powershell
dotnet restore backend/prn232_comic_api.sln
dotnet build backend/prn232_comic_api.sln --no-restore
```

Frontend changes normally require, from `frontend/`:

```powershell
npm run lint
npm run build
```

Run relevant tests when test projects exist. The repository currently has no committed automated test project; do not claim test coverage that does not exist.

Do not modify source merely to make an unrelated baseline failure pass. Report unrelated failures separately.

## Contract Verification

A successful build alone does not prove that a distributed flow still works. For changes affecting a contract or important business flow, smoke-check the relevant REST route, gRPC method, YARP transform, authentication behavior, RabbitMQ event when implemented, or database migration when practical.

Prioritize login/authentication, chapter reading, chapter purchase, deposit, wallet credit/debit, mission reward, and admin authorization. Full end-to-end testing is not required for every small local bug. The level of verification must be proportional to the risk and scope of the change.

## Final Change Review

Before finishing a coding task:

- Inspect `git diff` and `git status`.
- Verify that only intended files were changed and unrelated formatting was not introduced.
- Verify that generated build artifacts, secrets, temporary files, or unrelated files were not accidentally added.
- Preserve unrelated user changes that were already present in the working tree.

## Documentation update matrix

| Code change | Documentation to inspect/update |
|---|---|
| System architecture | `docs/ARCHITECTURE.md` |
| Project/folder structure | `docs/PROJECT_STRUCTURE.md` |
| Service responsibility/boundary | `docs/SERVICE_CATALOG.md` |
| REST/gRPC/RabbitMQ communication | `docs/COMMUNICATION.md` |
| Database ownership/schema/invariant | `docs/DATABASE.md` |
| Coding convention | `docs/CONVENTIONS.md` |
| Build/run/test/migration | `docs/DEVELOPMENT.md` |
| Important architectural decision | `docs/DECISIONS.md` |
| Public project overview changed | `README.md` |

Examples:

- Adding an integration event requires checking `COMMUNICATION.md`, `SERVICE_CATALOG.md`, and `ARCHITECTURE.md`.
- Adding or changing an index/migration requires checking `DATABASE.md`.
- Moving a project or folder requires checking `PROJECT_STRUCTURE.md`.
- A local bug fix that changes no contract, architecture, ownership, convention, or runbook needs no documentation churn.

## Documentation rules

- Link to the canonical owner instead of copying large sections across files.
- Use exact project, type, route, proto, environment-variable, and port names verified from source.
- Do not include secret values. Document configuration keys only.
- Distinguish `CURRENT`, `TRANSITIONAL`, `PLANNED`, and `NOT IMPLEMENTED` where ambiguity is possible.
- Update `docs/DECISIONS.md` only for a real architectural decision, not every implementation detail.
- Check all changed Markdown links before finishing.

## Definition of Done

A coding task is complete only when:

- the requested implementation is complete;
- affected projects restore/build successfully, or unrelated blockers are reported;
- relevant tests pass when tests exist;
- relevant public/cross-service contracts were smoke-checked when practical;
- there is no obvious service-boundary or layering violation;
- no new architecture pattern duplicates an existing equivalent;
- all known cross-service consumers were inspected when a contract changed;
- generated migrations were inspected when applicable;
- documentation impact has been assessed;
- affected canonical documentation has been updated;
- documentation describes actual code, including partial states accurately;
- `git diff` was inspected and only intended files were changed;
- no secrets, temporary files, or generated build artifacts were introduced;
- no unrelated user changes were overwritten.
