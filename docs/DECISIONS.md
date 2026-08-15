# Architecture Decision Log

This file records decisions supported by current source code. “Reason inferred” means the reason is inferred from the implemented structure rather than quoted from a historical ADR.

## ADR-001 — Database per service

Status: Accepted

Context:
Eight business services each have their own `DbContext`, entities, migrations, and connection string.

Decision:
Each service owns and directly accesses only its own database. There is no shared `DbContext`, cross-service foreign key, or cross-service database query.

Reason:
Reason inferred from current architecture: preserve data ownership by bounded context and allow services to evolve their schemas independently.

Consequences:
Cross-service consistency is handled in the application layer through gRPC. Some flows risk partial failure because no distributed transaction exists.

Do not:
Do not read another service's database or create cross-database foreign keys to bypass communication contracts.

## ADR-002 — YARP as the public API Gateway

Status: Accepted

Context:
The frontend calls public paths that YARP routes to eight backend services.

Decision:
ApiGateway is the public REST entry point and performs routing/path transforms, CORS, edge JWT validation, and selected proxy-error normalization.

Reason:
Reason inferred from current architecture: the frontend needs one base URL, and internal service topology remains hidden.

Consequences:
Adding or changing a public route requires coordinating Gateway configuration, the service controller, and the Vite proxy when development uses relative URLs.

Do not:
Do not place domain logic or database access in the Gateway.

## ADR-003 — gRPC for synchronous internal communication

Status: Accepted

Context:
Services generate clients and servers from `.proto` files for lookup, validation, debit/credit, unlock, and activity-recording operations.

Decision:
Current service-to-service use cases that require an immediate response use gRPC.

Reason:
Reason inferred from current architecture: typed contracts and synchronous responses support decisions made within a request.

Consequences:
Provider availability and latency affect callers. Current cycles include Comic↔Chapter, Chapter↔Mission, and Social↔Mission.

Do not:
Do not reference another service's classes/repositories through a project reference. Do not remove gRPC merely to label the system “event-driven.”

## ADR-004 — One project per service with folder-based layering

Status: Transitional

Context:
Each microservice is currently one ASP.NET Core project containing Controllers, Services, Interfaces, Repositories, Data, Entities, and integration-specific folders.

Decision:
The current architecture keeps a boundary at the service/project level and uses the logical dependency path `Controller/gRPC adapter → Service → Repository → DbContext`.

Reason:
Reason inferred from current architecture: the structure is direct for the current codebase and avoids creating many subprojects.

Consequences:
The compiler does not enforce Domain/Application/Infrastructure boundaries. Clean Architecture, DDD, and CQRS are not general implemented architecture.

Supersession condition:
This decision remains authoritative for `CURRENT` code until an approved per-service architecture migration is actually implemented. When a service is separated into API/Application/Domain/Infrastructure boundaries, update this ADR together with `PROJECT_STRUCTURE.md`, `ARCHITECTURE.md`, `CONVENTIONS.md`, and affected service documentation in that migration task. A service is not considered migrated until source code and project references enforce the new dependencies.

Do not:
Do not describe every service as a four-project Clean Architecture implementation or require a split without an approved migration task.

## ADR-005 — SharedKernel contains shared technical/common primitives

Status: Accepted

Context:
Business APIs reference `SharedKernel`. It contains a base entity, responses, paging, enums, and utilities, but no service-owned `DbContext` or aggregate.

Decision:
Do not move entities owned by User, Comic, Chapter, Mission, Payment, or Wallet into SharedKernel.

Reason:
Reason inferred from current architecture: avoid a shared domain model that obscures data ownership.

Consequences:
gRPC contracts remain in/generated within services. Some current shared enums create compile-time coupling and must be evaluated when changed.

Do not:
Do not turn SharedKernel into a home for shared business logic merely because multiple services have similar fields.

## ADR-006 — RabbitMQ and Redis are not runtime application architecture

Status: Transitional

Context:
Docker Compose declares RabbitMQ and Redis, but application code has no client package, publisher, consumer, cache registration, Outbox, or Inbox.

Decision:
Current documentation treats both components as available infrastructure only, not implemented application capabilities.

Reason:
This conclusion is directly supported by current source code.

Consequences:
Business communication is REST/gRPC apart from external HTTP webhooks/integrations. Documentation must not promise messaging or caching reliability that does not exist.

Do not:
Do not document events, consumers, or active Redis caching before real implementation, configuration, and consumers exist.

## ADR-007 — Local transactions and local idempotency; no distributed transaction

Status: Transitional

Context:
Payment purchase/deposit and Mission reward flows call Wallet/Chapter through gRPC. Wallet ledger entries have unique `ReferenceId` values, Payment has unique transaction/purchase keys, and selected repositories use `Serializable` transactions.

Decision:
Each database currently maintains its own local transaction. Cross-service flows rely on stable references, unique constraints, and best-effort compensation.

Reason:
Reason inferred from current implementation: the repository has no Saga, Outbox, or Inbox.

Consequences:
A timeout or crash between steps may require retry or reconciliation. Purchase refunds are best effort and are not a durable state machine.

Do not:
Do not describe current flows as exactly-once or atomic across services.

## ADR-008 — Authorization is enforced by backend components

Status: Accepted

Context:
The Gateway authenticates JWTs for non-public routes, and service controllers use `[Authorize]` and roles. Frontend route guards can read role/token information from local storage or JWT data.

Decision:
Frontend guards support navigation and user experience only; the Gateway and services are security boundaries.

Reason:
This is confirmed by current middleware and controller implementation.

Consequences:
Gateway public-route rules and service authorization attributes must be reviewed together. JWT validation is not currently identical across all services.

Do not:
Do not treat hiding a button or using a frontend route guard as authorization.
