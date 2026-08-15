# Service Catalog

This catalog defines current service responsibilities and ownership. A copied ID, DTO, or proto does not transfer ownership of the referenced data.

## Summary

| Service | Responsibility | Owns | Database | Sync dependencies | Async dependencies |
|---|---|---|---|---|---|
| ApiGateway | Edge routing/auth/CORS/error normalization | YARP route configuration | None | All business APIs through HTTP proxy | None |
| UserAPI | Identity, authentication, profile, roles | Users, roles, refresh tokens, verification codes | User-owned DB via `UserDbContext` | None | None |
| ComicAPI | Comic catalog, categories, outstanding list, translation entry point | Comics, categories, comic-category links, outstanding records | Comic-owned DB via `ComicDbContext` | UserAPI, ChapterAPI | None |
| ChapterAPI | Chapters, pages, read entitlement | Chapters, chapter pages, local purchase entitlement records | Chapter-owned DB via `ChapterDbContext` | ComicAPI, MissionAPI | None |
| SocialAPI | Comments, favorites, follows, reading history | Social interactions and reading history | Social-owned DB via `SocialDbContext` | MissionAPI; Comic validation over HTTP | None |
| MissionAPI | Missions, progress/activity, rewards, notifications, uploads | Missions, user missions, mission activities, notifications, upload records | Mission-owned DB via `MissionDbContext` | ChapterAPI, SocialAPI, WalletAPI | None |
| PaymentAPI | Deposit, SePay processing, purchase/payment transaction history | Payment transactions and payment-side purchase records | Payment-owned DB via `PaymentDbContext` | ChapterAPI, WalletAPI | None |
| WalletAPI | Wallet balance, ledger, withdrawals | Wallets, currency entries, withdrawal requests | Wallet-owned DB via `WalletDbContext` | None | None |
| BannerAPI | Home-page banners | Banner records | Banner-owned DB via `BannerDbContext` | None | None |
| SharedKernel | Shared compile-time primitives | No runtime/domain data | None | None | None |

RabbitMQ is infrastructure-only. No service currently publishes or consumes integration events.

## ApiGateway

**Responsibility:** public backend entry point for the browser.

- **Owned domain:** none.
- **Owned data/database:** none.
- **Public REST responsibility:** receives frontend paths and proxies them using YARP.
- **gRPC provided/consumed:** none.
- **Events published/consumed:** none.
- **External integrations:** none beyond downstream HTTP routing.
- **Dependencies:** every business API as a reverse-proxy destination.

Gateway contains edge authentication/public-path logic but must not be treated as the owner of user authorization data.

## UserAPI

**Responsibility:** identity and account lifecycle.

- **Owned domain:** registration, login/logout, JWT/refresh token lifecycle, email verification, password reset, Google login, profile/avatar, user activation, and roles.
- **Owned data:** `User`, `Role`, `UserRole`, `RefreshToken`, `VerificationCode`.
- **Database:** `UserDbContext`; runtime connection primarily comes from `AUTH_DB_CONNECTION`.
- **Public REST responsibility:** `/auth/**`, including `/auth/admin/users/**`.
- **gRPC provided:** `UserService`:
  - `GetUserById`
  - `GetUsersByIds`
  - `ValidateToken`
- **gRPC consumed:** none.
- **Events published/consumed:** none.
- **External integrations:** SMTP/MailKit, Google token validation, Cloudinary avatar storage.
- **Dependencies:** no synchronous business-service client.

Other services may cache or display a user ID/name, but UserAPI remains the identity owner.

## ComicAPI

**Responsibility:** comic catalog and discovery metadata.

- **Owned domain:** comics, comic status/pricing, categories, category assignment, outstanding comics, cover upload, translation endpoint.
- **Owned data:** `Comic`, `Category`, `ComicCategory`, `Outstanding`.
- **Database:** `ComicDbContext`; runtime connection primarily comes from `COMIC_DB_CONNECTION`.
- **Public REST responsibility:** `/api/comics`, `/api/categories`, `/api/translations` (exposed without `/api` through Gateway transforms).
- **gRPC provided:** `ComicGrpc`:
  - `CheckComicExists`
  - `IncrementComicView`
- **gRPC consumed:**
  - UserAPI `UserService` for author/user lookup.
  - ChapterAPI `ChapterGrpc` for chapter counts and purchased comic IDs.
- **Events published/consumed:** none.
- **External integrations:** Cloudinary for covers; n8n webhook for text translation (workflow uses LibreTranslate).
- **Dependencies:** UserAPI and ChapterAPI.

ComicAPI owns comic metadata. It does not own chapter pages or payment entitlement.

## ChapterAPI

**Responsibility:** chapter content and chapter-side read entitlement.

- **Owned domain:** chapter metadata/status/price, ordered page images, per-user chapter unlock records.
- **Owned data:** `Chapter`, `ChapterPage`, `UserPurchase` (a ChapterDB entitlement record, not the PaymentDB transaction owner).
- **Database:** `ChapterDbContext`; runtime connection primarily comes from `CHAPTER_DB_CONNECTION`.
- **Public REST responsibility:** `/api/chapters` and page-management/read endpoints.
- **gRPC provided:** `ChapterGrpc`:
  - `UnlockChapter`
  - `IsChapterPurchased`
  - `GetChapterInfo`
  - `GetChapterCount`
  - `GetPurchasedComicIds`
  - `GetUserPurchaseActivities`
- **gRPC consumed:**
  - ComicAPI `ComicGrpc` for comic validation/view operations.
  - MissionAPI `MissionProgress` for synchronous activity recording.
- **Events published/consumed:** none.
- **External integrations:** Cloudinary for chapter-page images.
- **Dependencies:** ComicAPI and MissionAPI.

The `UserPurchase` entity here is an entitlement projection/local record. PaymentAPI separately owns financial purchase history.

## SocialAPI

**Responsibility:** reader social interactions and reading history.

- **Owned domain:** nested comments, favorites, follows, latest reading position/history.
- **Owned data:** `Comment`, `Favorite`, `Follow`, `ReadingHistory`.
- **Database:** `SocialDbContext`; runtime connection primarily comes from `SOCIAL_DB_CONNECTION`.
- **Public REST responsibility:** `/api/comments`, `/api/favorites`, `/api/follows`, `/api/reading-history`.
- **gRPC provided:** `SocialActivity.GetUserActivities` for MissionAPI snapshot synchronization.
- **gRPC consumed:** `MissionProgress.RecordActivity` for comment/read activity notification.
- **Events published/consumed:** none.
- **External integrations:** an HTTP `ComicValidator` intended to validate comic IDs through an API endpoint.
- **Dependencies:** MissionAPI; ComicAPI indirectly through the HTTP validator.

Current limitation: the validator's default base URL/path is inconsistent with the current Gateway configuration and returns success on exceptions.

## MissionAPI

**Responsibility:** mission definitions, activity-based progress, completion rewards, notifications, and generic uploads.

- **Owned domain:** missions, per-user mission state, counted activity IDs, notifications, upload metadata.
- **Owned data:** `Mission`, `UserMission`, `MissionActivity`, `Notification`, `Upload`.
- **Database:** `MissionDbContext`; runtime connection primarily comes from `MISSION_DB_CONNECTION`.
- **Public REST responsibility:** `/api/missions`, `/api/notifications`, `/api/uploads`.
- **gRPC provided:** `MissionProgress.RecordActivity`.
- **gRPC consumed:**
  - ChapterAPI `ChapterGrpc.GetUserPurchaseActivities`.
  - SocialAPI `SocialActivity.GetUserActivities`.
  - WalletAPI `WalletService.AddCoin` through `WalletGrpcClient`.
- **Events published/consumed:** none.
- **External integrations:** Cloudinary uploads.
- **Dependencies:** ChapterAPI, SocialAPI, WalletAPI.

MissionAPI owns progress/reward state. Source activity objects remain owned by ChapterAPI or SocialAPI.

**Critical risks:** activity synchronization forms synchronous cycles with ChapterAPI/SocialAPI, and reward completion depends on a synchronous WalletAPI credit without durable retry. Database safeguards do not remove this cross-service risk; see [DATABASE.md](DATABASE.md).

## PaymentAPI

**Responsibility:** financial transaction records, deposit orders/webhooks, and paid-chapter purchase orchestration.

- **Owned domain:** payment transactions, deposit transaction codes/status, payment-side purchase records and compensation attempts.
- **Owned data:** `Transaction`, `UserPurchase` (financial purchase record).
- **Database:** `PaymentDbContext`; runtime connection primarily comes from `PAYMENT_DB_CONNECTION`.
- **Public REST responsibility:** `/api/payments`, including chapter purchase, transaction queries, deposit creation, and SePay webhook.
- **gRPC provided:** none.
- **gRPC consumed:**
  - ChapterAPI `ChapterGrpc` for info, unlock, and purchase checks.
  - WalletAPI `WalletService` for debit and credit/refund.
- **Events published/consumed:** none.
- **External integrations:** VietQR image endpoint and SePay webhook authentication/payload.
- **Dependencies:** ChapterAPI and WalletAPI.

PaymentAPI owns financial history; it does not own wallet balance or chapter content. Cross-service purchase compensation is best-effort and synchronous.

**Critical risks:** a purchase crosses PaymentDB, WalletAPI, and ChapterAPI without a distributed transaction. Refund compensation is best effort, so timeouts/crashes can require reconciliation; see [DATABASE.md](DATABASE.md).

## WalletAPI

**Responsibility:** balance and ledger source of truth plus withdrawal workflow.

- **Owned domain:** wallets, credit/debit ledger, mission-credit withdrawability, fees, pending/approved/rejected withdrawals.
- **Owned data:** `Wallet`, `CurrencyEntry`, `Withdraw`.
- **Database:** `WalletDbContext`; runtime connection comes from `WALLET_DB_CONNECTION` or configured wallet/default connection.
- **Public REST responsibility:** `/currency` and `/withdraws`. Gateway also has a `/wallets/**` route, but no corresponding REST controller currently exists.
- **gRPC provided:** `WalletService`:
  - `AddCoin`
  - `DebitCoin`
- **gRPC consumed:** none.
- **Events published/consumed:** none.
- **External integrations:** none.
- **Dependencies:** none.

WalletAPI is the balance/ledger owner. PaymentAPI computes one legacy-style transaction-derived balance method internally, but that does not make PaymentAPI the wallet owner.

**Critical invariants:** WalletAPI is the balance/ledger source of truth; unique `ReferenceId` protects repeated local ledger operations; and the database permits at most one pending withdrawal per wallet. These local protections do not guarantee cross-service exactly-once processing; see [DATABASE.md](DATABASE.md).

## BannerAPI

**Responsibility:** ordered, active/inactive homepage banners.

- **Owned domain/data:** `Banner`.
- **Database:** `BannerDbContext`; connection comes from `BANNER_DB_CONNECTION` or banner/default configuration.
- **Public REST responsibility:** `/banners`, with anonymous active-banner reads and admin CRUD/upload endpoints.
- **gRPC provided/consumed:** none.
- **Events published/consumed:** none.
- **External integrations:** Cloudinary image upload/delete.
- **Dependencies:** none.

## SharedKernel

**Responsibility:** compile-time shared primitives.

- **Owned data/database:** none.
- **Contains:** `BaseEntity`, `ApiResponse`, `PagedResult`, `SlugGenerator`, and shared enums.
- **Runtime communication:** none.

SharedKernel must not be described as a microservice. Its current shared enums create compile-time coupling but no shared database ownership.
