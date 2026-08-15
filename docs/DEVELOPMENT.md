# Development Runbook

This runbook is verified from the solution, project files, launch profiles, npm scripts, Docker Compose, and current configuration code.

## Prerequisites

- .NET SDK 8.x; every backend project targets `net8.0`.
- A Node.js/npm version compatible with the currently locked Vite dependencies.
- Docker Desktop or Docker Engine with Compose for local infrastructure.
- A SQL Server instance reachable from the host.
- PowerShell for the existing port-management script; `dotnet` and `npm` commands also work from other shells.

```powershell
dotnet --version
node --version
npm --version
docker compose version
```

## Configuration

Use [backend/.env.example](../backend/.env.example) as a local configuration inventory. ASP.NET Core does not automatically load a plain `.env` file when running `dotnet run`; export values through the shell, configure the IDE/launch profile, use user secrets, or use another supported configuration provider.

| Group | Main keys |
|---|---|
| Databases | `AUTH_DB_CONNECTION`, `COMIC_DB_CONNECTION`, `CHAPTER_DB_CONNECTION`, `SOCIAL_DB_CONNECTION`, `MISSION_DB_CONNECTION`, `PAYMENT_DB_CONNECTION`, `WALLET_DB_CONNECTION`, `BANNER_DB_CONNECTION` |
| JWT | `JwtSettings__Secret`, `JwtSettings__Issuer`, `JwtSettings__Audience`, `JwtSettings__ExpiryMinutes` |
| Internal services | `USER_API_URL`, `COMIC_API_URL`, `CHAPTER_API_URL`, `MISSION_API_URL`, `SOCIAL_API_URL`, `WALLET_API_URL`, plus selected `GrpcEndpoints__...` keys |
| Public URLs | `API_GATEWAY_URL` for SocialAPI comic validation, `FRONTEND_URL` where used by current code |
| Cloudinary | `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`; some services read `CloudinarySettings__...` instead |
| Email and Google | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS`, `GOOGLE_CLIENT_ID` |
| Payment | `TopUp__CoinRate`, `BankSettings__BankCode`, `BankSettings__AccountNumber`, `BankSettings__AccountName`, `BankSettings__SepayApiKey` |
| Translation | `N8nTranslation__WebhookUrl`, `N8nTranslation__Secret`, `N8nTranslation__TimeoutSeconds` |
| Docker infrastructure | `SQL_SA_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASS` |

`backend/.env.example` lists the current primary environment keys, including internal/public service URLs and the standardized `Cloudinary__...` keys. BannerAPI and MissionAPI still accept the legacy `CloudinarySettings__...` prefix as a compatibility fallback. Committed `appsettings.json` connection-string values are empty.

SocialAPI resolves the validator base address from `API_GATEWAY_URL`, then `ApiGateway:BaseUrl`, with `http://127.0.0.1:5028` as the local fallback. Configure the value as the Gateway origin; the validator appends `/comics/{id}` according to the public YARP contract.

The frontend uses [frontend/.env.example](../frontend/.env.example):

```env
VITE_API_URL=
```

Leave it empty in development so relative requests use the Vite proxy. Set it to the public API Gateway URL for deployment.

## Docker infrastructure

From `backend/`:

```powershell
docker compose up -d
docker compose ps
```

| Container | Host ports | Current application status |
|---|---:|---|
| SQL Server 2022 | 1433 | Used by services through configured connection strings |
| Redis | 6379 | Infrastructure only; no Redis client/application code |
| RabbitMQ management | 5672, 15672 | Infrastructure only; no publisher or consumer |
| LibreTranslate | 5000 | Used by the n8n text-translation workflow |
| n8n | 5678 | Called by ComicAPI when its translation webhook is configured |

The workflow is stored at `backend/n8n/workflows/comic-text-translation.json`.

```powershell
docker compose down
```

Do not add `-v` unless you intentionally want to delete local volumes/databases.

## Restore and build the backend

From the repository root:

```powershell
dotnet restore backend/prn232_comic_api.sln
dotnet build backend/prn232_comic_api.sln
dotnet build backend/prn232_comic_api.sln -c Release
```

## Database migrations

Each service owns its own migrations. Examples:

```powershell
dotnet ef database update --project backend/UserAPI/UserAPI.csproj --startup-project backend/UserAPI/UserAPI.csproj
dotnet ef database update --project backend/ChapterAPI/ChapterAPI.csproj --startup-project backend/ChapterAPI/ChapterAPI.csproj
```

Select the owning service project and ensure its connection string is available. If the EF CLI is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

User, Chapter, Mission, Payment, Wallet, and Banner currently call `Migrate()` at startup. Comic and Social use `EnsureCreated()`. See [DATABASE.md](DATABASE.md).

## Run the backend and full system

Run an individual project with its HTTPS launch profile:

```powershell
dotnet run --project backend/ApiGateway/ApiGateway.csproj --launch-profile https
dotnet run --project backend/UserAPI/UserAPI.csproj --launch-profile https
```

The frontend npm launcher can start all eight APIs, the Gateway, and Vite:

```powershell
Set-Location frontend
npm install
npm run dev
```

`frontend/scripts/dev.mjs` waits for each backend process to become ready before starting Vite. Configure the required database, JWT, and integration settings first.

## Ports

| Process | HTTP | HTTPS |
|---|---:|---:|
| Frontend Vite | 5173 | — |
| ApiGateway | 5028 | 7023 |
| ComicAPI | 5023 | 7024 |
| UserAPI | 5054 | 7231 |
| ChapterAPI | 5115 | 7114 |
| BannerAPI | 5127 | 7053 |
| PaymentAPI | 5128 | 7071 |
| SocialAPI | 5197 | 7133 |
| MissionAPI | 5288 | 7224 |
| WalletAPI | 5091 | 7066 |

The Gateway development targets and Vite proxy use HTTP localhost/127.0.0.1 addresses. gRPC endpoints come from configuration/environment with local fallbacks in code.

```powershell
./backend/dev-ports.ps1 -Action Status
./backend/dev-ports.ps1 -Action Stop
```

The script stops only processes identified as belonging to this solution.

## Frontend

From `frontend/`:

```powershell
npm install
npm run dev:ui
npm run lint
npm run build
npm run preview
```

`dev:ui` starts only Vite and is appropriate when the backend is already running.

## Verification by change scope

Verification must be proportional to the scope and risk of the change:

| Change scope | Minimum verification |
|---|---|
| Backend-only change in one service | Restore/build the project or solution; smoke-check the affected REST/gRPC flow when practical |
| Cross-service contract | Build the provider and every consumer; verify proto generation/client usage, DI/configuration, and smoke-check both ends |
| Database model/schema | Inspect the generated migration; build the owning service; apply/update the migration in an appropriate local environment when practical |
| Frontend or frontend-consumed REST contract | Run `npm run lint` and `npm run build`; inspect the affected frontend API caller/service module |
| Authentication/authorization | Build the Gateway and affected APIs; check protected/public behavior and related route guards/callers |

A successful build does not replace contract verification for a distributed flow. Do not modify unrelated source code merely to make a pre-existing baseline failure pass; report the failure separately.

## Tests

The repository currently has no backend test project, and `package.json` has no frontend test script. The current verification baseline is:

```powershell
dotnet build backend/prn232_comic_api.sln
npm run lint --prefix frontend
npm run build --prefix frontend
```

When automated test projects/scripts are introduced, this file must become the canonical source for exact test commands, and the corresponding CI configuration must be updated.

## Troubleshooting

### A service cannot connect to its database

- Verify that the SQL container is running and port 1433 is available.
- Verify the service-specific connection variable; committed fallback values are empty.
- Account for the migration-strategy difference between Comic/Social and the other services.

### Gateway returns 502/503

- Verify that the destination service listens on the expected HTTP port.
- Run `./backend/dev-ports.ps1 -Action Status`.
- Compare the route against [COMMUNICATION.md](COMMUNICATION.md).

### gRPC is unavailable

- Verify that the configured URL uses the correct provider, scheme, and port.
- Verify that both provider and consumer are running.
- Current dependency cycles mean Mission/Chapter/Social must all be available for selected flows.

### Translation does not work

- Verify that n8n and LibreTranslate are running.
- Import/activate the workflow and configure the ComicAPI webhook URL correctly.
- The example timeout is 65 seconds.

### A Vite route is not proxied

`vite.config.js` does not currently proxy `/wallets`, `/currency`, or `/uploads`, although Gateway routes exist. Use an appropriate `VITE_API_URL` or update the proxy in a separate coding task.
