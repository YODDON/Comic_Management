# Project Structure

This document describes where code currently lives. It does not prescribe a future project split.

## Repository root

```text
Comic_Management/
├── AGENTS.md                    # AI workflow and documentation router
├── README.md                    # Human-facing overview and quick start
├── PROMP.EXAMPLE.md              # Reusable task prompt template
├── docs/                        # Canonical current-state documentation
├── backend/                     # .NET solution and local infrastructure
└── frontend/                    # React/Vite SPA
```

Generated/output folders such as `bin`, `obj`, `node_modules`, and `dist` are not part of the maintained structure.

## Backend solution

`backend/prn232_comic_api.sln` contains ten .NET 8 projects:

```text
backend/
├── ApiGateway/
├── UserAPI/
├── ComicAPI/
├── ChapterAPI/
├── SocialAPI/
├── MissionAPI/
├── PaymentAPI/
├── WalletAPI/
├── BannerAPI/
├── SharedKernel/
├── n8n/
├── docker-compose.yml
├── dev-ports.ps1
└── prn232_comic_api.sln
```

Every executable backend project currently targets `net8.0`. Each business API references `SharedKernel`; business APIs do not have direct project references to each other. Cross-service gRPC clients are generated from local copies of `.proto` contracts instead.

### Current project boundary

Each service is one project. Folder names provide logical layering, but there are no separate `.Domain`, `.Application`, `.Infrastructure`, or `.API` projects.

This layout describes `CURRENT` code placement. Do not create `.API`, `.Application`, `.Domain`, or `.Infrastructure` projects unless the task explicitly performs an approved migration for that service. When a service is actually migrated, update this file in the same task; target trees that are not implemented belong in planning material rather than this canonical structure document.

Typical current structure:

```text
SomeAPI/
├── Controllers/                # REST transport adapters
├── GrpcServices/               # gRPC server transport adapters when the service provides gRPC
├── DTOs/                       # Request/response and service result models
├── Entities/                   # EF/domain data models
├── Interfaces/                 # Service, repository, and external-port abstractions
├── Services/                   # Use-case logic, gRPC adapters/clients, external adapters
├── Repositories/               # EF Core persistence implementations
├── Data/                       # DbContext
├── Mappings/                   # AutoMapper profiles where present
├── Protos/                     # Local gRPC contract copies
├── Settings/                   # Options models where present
├── Migrations/                 # EF Core migrations and model snapshot
├── Properties/launchSettings.json
├── Program.cs                  # Composition root and middleware pipeline
├── appsettings.json
└── SomeAPI.csproj
```

Folder presence differs by service; do not assume every project has every folder.

## Backend projects

### ApiGateway

```text
ApiGateway/
├── Program.cs
├── appsettings.json            # YARP routes/clusters
├── Properties/launchSettings.json
└── ApiGateway.csproj
```

There are no controllers, entities, repositories, migrations, or database. Gateway behavior is configured directly in its composition root and YARP configuration.

### UserAPI

Notable folders:

- `Controllers/`: auth and admin-user REST endpoints.
- `Services/`: authentication, email, avatar storage, admin-user, token, and query services.
- `Services/Auth`, `Services/Admin`, and `Services/User`: feature-oriented application services; provider adapters such as email/avatar/token remain directly under `Services/`.
- `Repositories/User` and `Repositories/Admin`: user persistence implementations.
- `Interfaces/User` and `Interfaces/Admin`: user/admin abstractions; other provider abstractions remain directly under `Interfaces`.
- `GrpcServices/`: `UserGrpcService` server adapter. All current gRPC providers use this folder convention.
- `Protos/user.proto`: gRPC server contract.
- `Data/UserDbContext.cs`, `Entities/`, `Migrations/`: UserDB ownership.

### ComicAPI

**Note**: `ComicAPI` has been migrated to a Clean Architecture structure:
- `ComicAPI.Domain`: Core entities and interfaces.
- `ComicAPI.Application`: Application services, DTOs, Mapping profiles, and Client Protos (`user.proto`, `chapter.proto`).
- `ComicAPI.Infrastructure`: DbContext, EF Migrations, Repositories implementations, and external service implementations (Cloudinary, N8n).
- `ComicAPI.API`: Controllers, Server Protos (`comic.proto`), gRPC server adapter, DI composition root.

### ChapterAPI

Notable folders:

- `Controllers/ChaptersController.cs`.
- `Services/Chapter`: chapter use cases.
- `GrpcServices/ChapterGrpcService.cs`: Chapter gRPC server adapter.
- `Services/CloudinaryService.cs`: chapter-page image storage.
- `Services/MissionProgressNotifier.cs`: MissionAPI gRPC client wrapper.
- `Repositories/Chapter`, `Interfaces/Chapter`: persistence and use-case boundaries.
- `Protos/`: Comic client, Mission client, and Chapter server contracts.

### SocialAPI

Social code is organized by feature under `Services`, `Repositories`, and `Interfaces`:

- `Comment`, `Favorite`, `Follow`, `ReadingHistory`, and `SocialActivity`.
- `Controllers/` exposes REST for comments, favorites, follows, and reading history.
- `GrpcServices/SocialActivityGrpcService.cs` provides snapshot data to MissionAPI.
- `MissionProgressNotifier.cs` consumes MissionAPI gRPC.
- `ComicValidator.cs` is an HTTP-based cross-service validator.

### MissionAPI

Mission code is grouped by `Mission`, `Notification`, and `Upload` under service/repository/interface folders.

- `GrpcServices/MissionProgressGrpcService.cs` receives activity notifications.
- `MissionActivitySyncService` calls ChapterAPI and SocialAPI snapshot gRPC endpoints.
- `WalletGrpcClient` credits mission rewards.
- `CloudinaryService` backs uploads.

### PaymentAPI

PaymentAPI currently has a relatively flat structure:

- One `PaymentsController`.
- `PaymentService` contains purchase, deposit, transaction-query, webhook, gRPC orchestration, and compensation behavior.
- `PaymentRepository` owns PaymentDB queries and local EF transactions.
- `Settings/` contains bank and top-up options.
- `Protos/` contains ChapterAPI and WalletAPI client contracts.

### WalletAPI

WalletAPI groups its three persistence/use-case areas consistently under `DTOs`, `Interfaces`, `Repositories`, and `Services`:

- Currency: `CurrencyController` → `Services/Currency/CurrencyService` → `Repositories/Currency/CurrencyRepository`.
- Withdrawals: `WithdrawsController` → `Services/Withdraw/WithdrawService` → `Repositories/Withdraw/WithdrawRepository`.
- gRPC wallet operations: `GrpcServices/WalletGrpcService` → `Services/Wallet/WalletApplicationService` → `Repositories/Wallet/WalletRepository`.

`Services/Wallet/WalletRules.cs` contains current fee, withdrawable-credit, and credit-type rules. All code remains in one project.

### BannerAPI

BannerAPI is the smallest business API and uses a straightforward layered structure:

```text
BannersController -> BannerService -> BannerRepository -> BannerDbContext
                           |
                           +-> CloudinaryService
```

It does not provide or consume gRPC.

### SharedKernel

```text
SharedKernel/
├── Entities/BaseEntity.cs
├── Enums/                      # Shared business enums currently used by several APIs
├── Responses/                  # ApiResponse and PagedResult
├── Utilities/SlugGenerator.cs
└── SharedKernel.csproj
```

It is a class library, not a runtime service. It currently includes business-flavored enums as well as technical primitives; this is current structure, even though reducing coupling is discussed in the planning document.

## Migrations and persistence

Every business API has its own `Data/*DbContext.cs` and `Migrations/` folder. Migration files belong to that service only. See [DATABASE.md](DATABASE.md) for contexts and constraints.

## gRPC contracts

Proto contracts are copied into both provider and consumer projects rather than shared through a common contract package. The corresponding `.csproj` marks each proto as `GrpcServices="Server"` or `GrpcServices="Client"`.

See [COMMUNICATION.md](COMMUNICATION.md) for the authoritative provider/consumer matrix.

## Frontend

```text
frontend/
├── public/
│   └── images/
├── scripts/
│   └── dev.mjs                 # Starts backend services then Vite
├── src/
│   ├── components/
│   │   └── admin/
│   ├── contexts/
│   ├── hooks/
│   ├── pages/
│   │   └── admin/
│   ├── services/               # Domain-oriented fetch wrappers
│   ├── utils/
│   ├── App.jsx                 # Client routes
│   ├── main.jsx                # Browser entry point
│   └── styles.css
├── index.html
├── vite.config.js              # Dev proxy to Gateway
├── eslint.config.js
├── package.json
└── package-lock.json
```

The frontend has no separate automated test setup. It uses one global stylesheet and local component/page state plus a language context and `localStorage`.

## Scripts and infrastructure

- `backend/docker-compose.yml`: SQL Server, Redis, RabbitMQ, LibreTranslate, and n8n containers.
- `backend/dev-ports.ps1`: reports or stops processes using the known service ports.
- `frontend/scripts/dev.mjs`: starts all eight APIs and Gateway with the HTTPS launch profiles, waits for readiness, then launches Vite.
- `backend/n8n/workflows/comic-text-translation.json`: translation workflow definition.

## Tests

No committed `*.Tests.csproj`, frontend test runner, or test files were found at the time this document was generated. Build and lint are the current automated verification commands; see [DEVELOPMENT.md](DEVELOPMENT.md).
