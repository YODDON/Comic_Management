# Databases and Data Ownership

This document is the canonical source for database ownership, important data constraints, and transaction boundaries. It is verified from current `DbContext` classes, entities, repositories, and migrations; it is not a complete schema reference.

## Current principles

- Every business microservice owns a `DbContext` and migration history.
- There is no shared `DbContext`, cross-service foreign key, or project reference used to access another service's database directly.
- IDs for data outside a bounded context are stored as scalar references. Application services validate them through gRPC/HTTP where implemented or accept eventual cross-service state.
- `SharedKernel` owns no database.
- ApiGateway does not access a database.

Do not read or write another service's database to bypass REST/gRPC. When a service needs additional data, add an appropriate communication contract or a read model owned by that service.

## Database ownership matrix

The database names below are logical bounded-context names. Physical catalog names belong in environment connection strings and are not hard-coded in the repository.

| Logical database | Owner service | DbContext | Main data | Important constraints/indexes |
|---|---|---|---|---|
| User DB | UserAPI | `UserDbContext` | Users, roles, user-role links, refresh tokens, verification codes | Token/code/user and user-role relationships; seeded `Admin`, `Guest`, and `Reader` roles |
| Comic DB | ComicAPI | `ComicDbContext` | Comics, categories, comic-category links, outstanding records | Comic/category many-to-many relationship; `Comic.Status` stored as string; `UnitPrice` is `decimal(18,2)` |
| Chapter DB | ChapterAPI | `ChapterDbContext` | Chapters, chapter pages, local purchase entitlements | One purchase per `(UserId, ChapterId)`; `UnitPrice` is `decimal(18,2)` |
| Social DB | SocialAPI | `SocialDbContext` | Comments, favorites, follows, reading history | Unique favorite `(UserId, ComicId)`, follow `(FollowerId, FollowingId)`, and history `(UserId, ComicId)`; parent-comment deletion is restricted |
| Mission DB | MissionAPI | `MissionDbContext` | Missions, user missions, activities, notifications, uploads | Unique user mission `(UserId, MissionId)`; unique activity `(UserId, MissionId, ActivityId)`; rewards use `decimal(18,2)` |
| Payment DB | PaymentAPI | `PaymentDbContext` | Payment transactions and payment-side purchase records | Unique non-empty `TransactionCode`; unique purchase `(UserId, ChapterId)`; amounts/prices use `decimal(18,2)` |
| Wallet DB | WalletAPI | `WalletDbContext` | Wallet balances, currency ledger, withdrawals | Unique wallet `UserId`; unique non-null ledger `ReferenceId`; at most one `Pending` withdrawal per wallet; monetary values use `decimal(18,2)` |
| Banner DB | BannerAPI | `BannerDbContext` | Banners and display order | Composite index `(IsActive, DisplayOrder)` |

## Three protection levels

- **Database-level protection:** a unique constraint/index prevents duplicate local records in one database.
- **Application-level idempotency:** application code uses a stable reference, current state, or lookup before repeating an operation.
- **Cross-service reliability:** a multi-service flow can retry and recover consistently after a timeout or crash.

The repository currently implements the first two levels for selected flows but has no shared durable mechanism for the third. A `UNIQUE` constraint prevents a duplicate local record; it does not create exactly-once processing or atomicity across PaymentDB, WalletAPI, and ChapterAPI.

## Important relationships and invariants

### UserAPI

`UserDbContext` is the source of truth for identity and roles. Other services store only a `UserId` or display data they need; they do not own `User`. Default roles are seeded with stable IDs in the current model.

### ComicAPI and ChapterAPI

ComicAPI owns comic metadata and categories. ChapterAPI owns chapters, page images, and local entitlements used to authorize reading. A `ComicId` in Chapter DB is a cross-service reference, not a foreign key to Comic DB.

PaymentAPI also defines `UserPurchase`, but its record is Payment's financial purchase history. The entity with the same name in ChapterAPI is the local reading entitlement. These records do not create shared ownership and are currently synchronized through gRPC.

The unique `(UserId, ChapterId)` purchase constraint in Chapter DB only prevents duplicate local entitlements. It does not prove that the Payment record, Wallet debit, and Chapter entitlement completed atomically.

### SocialAPI

Unique indexes make favorite, follow, and reading-history pairs single logical records. Comment parent-child relationships remain inside Social DB. `ComicId`, `ChapterId`, and `UserId` are scalar references, not cross-database foreign keys.

### MissionAPI

`MissionActivity.ActivityId` is part of a unique key with user and mission to reduce duplicate activity counting. `UserMission` has one record per user/mission. These are database protections; application services remain responsible for completion and reward rules.

These unique keys do not provide durable retry for Wallet reward credit and do not make the Mission-to-Wallet flow exactly-once across services.

### PaymentAPI

A non-empty `TransactionCode` is unique, supporting deposit/webhook lookup. A purchase is unique by user/chapter, preventing duplicate Payment DB purchase records. There is no distributed transaction across Payment, Wallet, and Chapter.

The current paid-chapter flow uses a local Payment DB transaction plus best-effort compensation:

1. Create a pending transaction in Payment DB.
2. Debit WalletAPI through gRPC with a stable reference.
3. Store the Payment-side purchase and request ChapterAPI unlock.
4. If unlock fails, request a Wallet credit refund and mark the transaction failed.

A purchase transaction starts as `Pending`, becomes `Rejected` when debit is declined, becomes `Completed` when purchase and unlock finish, or becomes `Failed` on handled unlock/processing failures. These are current local transaction statuses, not a persisted cross-service state machine.

Refund and entitlement changes occur outside the Payment DB local transaction, so a crash or timeout may still require manual reconciliation. The current implementation is multi-step orchestration with best-effort compensation, not a durable Saga/state machine; there is no Outbox/Inbox.

### WalletAPI

Wallet and ledger data are the source of truth for balance. Important credit/debit operations run under local `Serializable` transactions. A unique, nullable `CurrencyEntry.ReferenceId` is the main local idempotency mechanism for repeated calls.

The unique `ReferenceId` protects repeated Wallet operations in Wallet DB. It does not guarantee Payment + Wallet + Chapter atomicity and cannot repair caller state when a gRPC response is lost after Wallet commits.

Mission rewards are treated as spent before real top-up funds when calculating withdrawable balance. The withdrawal fee is 5%, capped at 10,000 coins, and is deducted in addition to the amount the user receives. A filtered unique index permits only one `Pending` withdrawal per wallet.

## Transaction boundaries

| Operation | Current transaction | Outside the transaction |
|---|---|---|
| Wallet credit/debit | Wallet DB, `Serializable` | Caller service and caller database |
| Withdrawal create/update | Wallet DB, `Serializable` | Any external processing |
| Chapter purchase | Payment DB local transaction | Wallet debit/refund and Chapter unlock through gRPC |
| SePay deposit | Payment state stored locally | Wallet credit through gRPC |
| Mission reward | Mission state stored locally | Wallet credit through gRPC |

There is no distributed transaction. Stable references and unique keys reduce duplicate processing but do not eliminate all partial failures.

## Migrations and schema initialization

Migrations live in each owning service project. Current startup behavior is:

- `Database.Migrate()`: UserAPI, ChapterAPI, MissionAPI, PaymentAPI, WalletAPI, and BannerAPI.
- `Database.EnsureCreated()`: ComicAPI and SocialAPI.

This is a current inconsistency. `EnsureCreated()` does not apply migration history in the same way as `Migrate()` and may produce schema differences between environments. This document records the current state; it does not recommend treating `EnsureCreated()` as a migration strategy.

Service-specific connection variables are listed in [DEVELOPMENT.md](DEVELOPMENT.md). Committed `appsettings.json` connection-string values are empty, so runtime environments must supply real values.

## When schema changes

Update this document when a change affects database ownership, a `DbContext`, a boundary-relevant relationship, a unique/index/idempotency invariant, a transaction boundary, migration strategy, or cross-service ID storage. Do not list every column or purely mechanical migration.
