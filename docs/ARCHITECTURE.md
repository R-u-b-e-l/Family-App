# Family Management & Accounting System — Architecture & Design

**Status:** Approved design baseline (Phase 0 — no code yet)
**Target stack:** .NET 10 (LTS) · ASP.NET Core Web API · EF Core 10 · SQL Server · .NET MAUI (Android-first)
**Date:** 2026-10-11

---

## 1. Solution Structure

```text
FamilyManagement.sln

src/
    FamilyManagement.Domain            # Entities, enums, domain rules. Zero dependencies.
    FamilyManagement.Application       # Use cases, service interfaces, DTOs, validators,
                                       # authorization policies, accounting engine logic.
    FamilyManagement.Infrastructure    # EF Core DbContext, migrations, repositories,
                                       # Identity stores, JWT/OTP/email/SMS implementations,
                                       # caching implementation, audit interceptor.
    FamilyManagement.Shared            # Contracts shared with MAUI: request/response DTOs,
                                       # enums, ApiResponse<T>, PagedResult<T>. No server deps.
    FamilyManagement.API               # Thin controllers, middleware, DI composition root,
                                       # Swagger, health checks, rate limiting.
    FamilyManagement.Maui              # Android app. MVVM, Shell navigation, SQLite cache.

tests/
    FamilyManagement.UnitTests         # Accounting engine, validators, services (mocked infra).
    FamilyManagement.IntegrationTests  # WebApplicationFactory + real SQL (Testcontainers or
                                       # LocalDB). Tenant isolation tests live here.

DatabaseScripts/                       # Generated from EF migrations (idempotent scripts).
                                       # Never read at runtime. Deleting it changes nothing.
```

## 2. Project Dependency Diagram

```text
                 ┌─────────────────────┐
                 │  FamilyManagement   │
                 │       .API          │
                 └───────┬──────┬──────┘
                         │      │
          ┌──────────────▼┐   ┌─▼──────────────────┐
          │  Application  │◄──┤  Infrastructure    │
          └──────┬───┬────┘   └─────┬──────────────┘
                 │   │              │
          ┌──────▼┐ ┌▼──────────────▼┐
          │Domain │ │    Shared      │
          └───────┘ └───────▲────────┘
                            │
                 ┌──────────┴──────────┐
                 │ FamilyManagement    │
                 │       .Maui         │
                 └─────────────────────┘
```

Rules:
- **Domain** references nothing.
- **Application** references Domain + Shared only. All infrastructure concerns are interfaces here (`IEmailService`, `ISmsService`, `ICacheService`, `IDateTimeProvider`, `ICurrentUser`).
- **Infrastructure** implements Application interfaces; it is the only project referencing EF Core/SQL.
- **API** references Application + Infrastructure (composition root only).
- **Maui** references **Shared only** — it never sees entities or server code.

## 3. Multi-Tenant Strategy

Shared database, shared schema, **tenant discriminator (`FamilyId`)** — the right cost/complexity point for this system.

Enforcement is layered; no single layer is trusted alone:

1. **Token level** — `family_id` is a claim in the JWT, set at login, never accepted from request bodies or query strings.
2. **Request level** — `ICurrentUser` (scoped service) exposes `FamilyId`/`UserId` parsed from the validated token.
3. **Query level** — EF Core **global query filter** on every entity implementing `ITenantEntity`:
   `builder.HasQueryFilter(e => e.FamilyId == _currentUser.FamilyId)`. Applied automatically to every read.
4. **Write level** — a `SaveChangesInterceptor` stamps `FamilyId` on added entities and **throws** if a modified/added entity carries a foreign `FamilyId`. Cross-tenant writes are impossible even from buggy service code.
5. **Test level** — integration tests assert Family A tokens get 404/empty results for Family B resource IDs (404, not 403, to avoid existence leaks).

`IgnoreQueryFilters()` is forbidden outside a small audited set of system operations (login lookup, family registration).

## 4. Database Design / ERD

```text
 Family 1──* AppUser            Family 1──* Account (self-ref parent)
 Family 1──* AppRole            Family 1──* AccountingPeriod
 Family 1──1 FamilySettings     Family 1──* Voucher 1──* VoucherDetail *──1 Account
 AppUser *──* AppRole (UserRole)                Voucher *──1 AccountingPeriod
 AppRole *──* Permission (RolePermission)
 AppUser 1──* RefreshToken      Family 1──* AuditLog
 AppUser 1──* OtpCode           Family 1──* Notification *──1 AppUser
 Family 1──* VoucherSequence (per type + fiscal year)
```

### Complete entity list

**Identity & tenancy**

| Entity | Key fields | Notes |
|---|---|---|
| `Family` | Id (GUID), Name, LogoPath, Status, CreatedDate | The tenant root |
| `FamilySettings` | FamilyId, Currency (default `BDT`), FinancialYearStartMonth, DateFormat, Timezone, Address, ContactInfo | 1:1 with Family |
| `AppUser` | Id, FamilyId, UserName, Email, Mobile, FullName, DateOfBirth, Gender, Address, ProfilePicturePath, Status, LastLoginDate | Extends `IdentityUser<Guid>` |
| `AppRole` | Id, FamilyId (null = system template), Name, Description, IsSystem | Extends `IdentityRole<Guid>`; roles are **per-family** |
| `Permission` | Id, Code (`Voucher.Create`), Group, Description | Global catalog, seeded |
| `RolePermission` | RoleId, PermissionId | |
| `UserRole` | UserId, RoleId | Identity join table |
| `RefreshToken` | Id, UserId, TokenHash, ExpiresAt, CreatedByIp, RevokedAt, ReplacedByTokenHash | Stored **hashed**; rotation with reuse detection |
| `OtpCode` | Id, UserId, Purpose (PasswordReset), CodeHash, ExpiresAt, AttemptCount, ConsumedAt, CreatedDate | Stored **hashed**; attempt + resend limits |

**Accounting**

| Entity | Key fields | Notes |
|---|---|---|
| `Account` | Id, FamilyId, ParentAccountId, Code, Name, AccountType (Asset/Liability/Equity/Income/Expense), NormalBalance (Debit/Credit), IsGroup, IsSystem, IsActive, Level, Path | Hierarchical CoA; `IsGroup` accounts cannot hold transactions |
| `AccountingPeriod` | Id, FamilyId, Name, StartDate, EndDate, Status (Open/Closed), ClosedBy, ClosedDate | Posting blocked into Closed periods |
| `Voucher` | Id, FamilyId, VoucherNumber, VoucherType (JV/PV/RV/CV), VoucherDate, PeriodId, Narration, TotalDebit, TotalCredit, Status (Draft/Posted/Cancelled/Reversed), ReversalOfVoucherId, CreatedBy/Date, UpdatedBy/Date, PostedBy/Date, RowVersion | Posted vouchers are immutable |
| `VoucherDetail` | Id, VoucherId, LineNo, AccountId, Debit, Credit, Narration | `decimal(18,2)`; exactly one of Debit/Credit > 0 per line |
| `VoucherSequence` | FamilyId, VoucherType, FiscalYear, NextNumber | Concurrency-safe number generation inside the posting transaction |

**Cross-cutting**

| Entity | Key fields |
|---|---|
| `AuditLog` | Id, FamilyId, UserId, Action, EntityName, EntityId, OldValueJson, NewValueJson, IpAddress, DeviceInfo, CreatedDate |
| `Notification` | Id, FamilyId, UserId (recipient), Type, Title, Body, IsRead, CreatedDate — channel-agnostic, Firebase-ready |

**There is no Ledger table.** The ledger, cash book, bank book, trial balance, P&L and balance sheet are all **projections over posted `VoucherDetail` rows** — one source of truth, no sync bugs. If scale ever demands it, a materialized balance table can be added behind the same report interfaces.

## 5. Authentication Architecture

**ASP.NET Core Identity** (custom `AppUser`/`AppRole`, GUID keys) for user storage, PBKDF2 password hashing, and lockout — plus a custom JWT layer:

```text
Login (username | email | mobile + password)
  ↓ Identity validates password (+ lockout on failures)
  ↓ Issue Access Token (JWT, ~15 min)   claims: sub, family_id, name, security_stamp
  ↓ Issue Refresh Token (random 64B, ~14 days) → stored hashed in DB, returned once
  ↓
Access token expires → POST /auth/refresh with refresh token
  ↓ Validate hash, not revoked, not expired → ROTATE (old revoked, new issued)
  ↓ Reuse of a revoked token ⇒ revoke the whole token family (theft detection)
  ↓
Logout → revoke refresh token server-side, clear SecureStorage on device
```

**Password recovery (OTP flow):**

```text
POST /auth/forgot-password { emailOrMobile }
  → always 200 (no user enumeration)
  → if user exists: generate 6-digit OTP, store SHA-256 hash, 5-min expiry,
    send via IEmailService / ISmsService (both abstractions; providers pluggable)
POST /auth/verify-otp { emailOrMobile, otp } → short-lived reset token (JWT, 10 min, single purpose)
POST /auth/reset-password { resetToken, newPassword } → consume, revoke all refresh tokens, audit
Limits: max 5 verify attempts per OTP, 60-sec resend cooldown, rate-limited endpoints.
```

**Family registration** is one transactional unit: create Family → FamilySettings (defaults, BDT) → copy system role templates into family roles → create admin user → assign FamilyAdmin → seed default Chart of Accounts → create current AccountingPeriod.

## 6. Role / Permission Architecture

- **Roles are per-family** (`AppRole.FamilyId`). At registration, system templates (`FamilyAdmin`, `FamilyMember`) are copied in; admins can later create roles like `Accountant`, `Viewer`.
- **Permissions are a fixed, seeded catalog** of codes (`Dashboard.View`, `Member.Create`, `Voucher.Post`, `Ledger.View`, `Settings.Edit`, …) grouped by module. New modules add permissions via migration/seed.
- Roles map to permissions via `RolePermission`; a user's effective permission set = union across their roles.
- **Enforcement:** permissions are *not* embedded in the JWT (they'd go stale). A dynamic policy provider turns `[HasPermission("Voucher.Post")]` into a policy; the authorization handler resolves the user's permission set from DB **via cache** (keyed `perm:{familyId}:{userId}`, invalidated on any role/permission change). The mobile app only uses permissions to *hide* UI; the API is the authority.
- A `security_stamp` claim lets the server invalidate outstanding access tokens on password change / deactivation.

## 7. Chart of Accounts Structure

Five fixed root types with a numeric code scheme (per family, seeded, then extendable):

```text
1000 Assets (Dr)                 3000 Equity (Cr)         4000 Income (Cr)
  1100 Current Assets              3100 Family Capital      4100 Salary Income
    1110 Cash                      3200 Retained Earnings   4200 Business Income
    1120 Bank                                               4900 Other Income
      1121 Bank Account 1        5000 Expenses (Dr)
    1130 Accounts Receivable       5100 Food
  1200 Fixed Assets                5200 Education
    1210 Property                  5300 Medical
    1220 Vehicle                   5400 Utilities
    1230 Equipment                 5500 House Rent
2000 Liabilities (Cr)              5600 Transportation
  2100 Current Liabilities         5900 Other Expense
    2110 Accounts Payable
    2120 Loans
```

- Code generation rule: child code = next free slot under parent's range (configurable step); unique per family (`UNIQUE(FamilyId, Code)`).
- Only **leaf (non-group)** accounts accept voucher lines. Deleting/demoting an account with transactions is blocked (deactivate instead).
- `NormalBalance` drives report sign logic (Asset/Expense = Debit-normal; Liability/Equity/Income = Credit-normal).

## 8. Voucher / Accounting Engine

**Invariants enforced in the Application layer (and re-checked at DB level):**

1. Every voucher line has exactly one of Debit/Credit > 0.
2. `SUM(Debit) == SUM(Credit)` and > 0 — validated before posting; `CHECK` constraints guard line-level rules.
3. Vouchers post only into an **Open** accounting period whose date range contains `VoucherDate`.
4. **Posted vouchers are immutable.** Corrections happen via **Reversal** (auto-generated mirror voucher, linked via `ReversalOfVoucherId`) — never by editing history.
5. Status flow: `Draft → Posted → (Cancelled | Reversed)`. Drafts are editable/deletable; Posted requires `Voucher.Post` permission; cancellation of posted vouchers requires `Voucher.Approve`-level permission and creates an audit entry.

**Posting is one serializable unit of work:**

```text
BEGIN TRANSACTION
    lock & increment VoucherSequence (FamilyId, Type, FiscalYear)  → voucher number
    validate: balanced, open period, active leaf accounts, tenant match
    insert Voucher + VoucherDetails, Status = Posted
    write AuditLog
COMMIT        → then invalidate dashboard/report cache keys for the family
(any failure → ROLLBACK, nothing partial ever persists)
```

**Voucher types** constrain line composition: PV (credit = Cash/Bank), RV (debit = Cash/Bank), CV (both sides Cash/Bank), JV (free-form). Opening balances are a special JV against `Family Capital` at period start.

**Reports (all server-side SQL over posted details):**
- **Ledger** — opening balance (aggregate before `DateFrom`) + paged rows with running balance (`SUM() OVER (ORDER BY date, voucherNo)`).
- **Cash Book / Bank Book** — ledger filtered to cash/bank accounts with receipt/payment presentation.
- **Trial Balance** — grouped sums per account as of a date; API asserts Dr == Cr.
- **P&L** — Income minus Expense sums over the period.
- **Balance Sheet** — Assets vs Liabilities + Equity (Equity includes computed retained earnings = cumulative P&L).

## 9. API Endpoint Structure (`/api/v1`)

```text
auth/          register-family, login, refresh, logout, forgot-password,
               verify-otp, reset-password, change-password
families/      GET current, PUT current, GET/PUT settings
users/         GET (paged/search), POST, GET {id}, PUT {id}, PATCH {id}/status,
               POST {id}/reset-password, PUT {id}/roles
roles/         CRUD + GET {id}/permissions, PUT {id}/permissions
permissions/   GET (catalog), GET mine
profile/       GET, PUT, PUT picture
accounts/      GET tree, GET (paged/search), POST, GET {id}, PUT {id},
               PATCH {id}/status, GET {id}/balance
periods/       GET, POST, POST {id}/close, POST {id}/reopen
vouchers/      GET (paged, filter: type/status/date/search), POST (draft),
               GET {id}, PUT {id} (draft only), DELETE {id} (draft only),
               POST {id}/post, POST {id}/cancel, POST {id}/reverse
reports/       ledger, cashbook, bankbook, trial-balance, profit-loss,
               balance-sheet   (all with date filters; ledger paged)
dashboard/     summary, charts/income-expense, charts/cash-flow,
               charts/expense-by-category, charts/income-by-category, recent-transactions
notifications/ GET (paged), PATCH {id}/read, PATCH read-all
audit-logs/    GET (paged, admin only)
health/        liveness + SQL readiness
```

All list endpoints: `?page=&pageSize=&search=&sortBy=&sortDir=` → `PagedResult<T>`. Every response wrapped in `ApiResponse<T> { success, message, data, errors }` with correct HTTP status codes. Versioning via URL segment (`Asp.Versioning`).

## 10. MAUI Architecture & Navigation

```text
App start → Splash → token check (SecureStorage)
   ├─ valid/refreshable token → AppShell (authenticated zone)
   └─ none/expired            → LoginPage (modal root)

AppShell (Flyout + TabBar)
   Dashboard │ Accounts │ Transactions │ Reports │ More(Family·Profile·Settings)

Transactions → VoucherListPage → VoucherDetailPage / VoucherEntryPage
Reports → report hub → Ledger / CashBook / BankBook / TrialBalance / P&L / BalanceSheet
```

- **MVVM** with CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`); one ViewModel per page; DI throughout.
- **Networking:** `IHttpClientFactory` + typed API clients over `FamilyManagement.Shared` DTOs; a `DelegatingHandler` attaches the access token and, on 401, performs a **single-flight refresh** (semaphore) then retries; refresh failure ⇒ clear session ⇒ navigate to Login.
- **Storage:** `SecureStorage` for tokens only (never passwords); **SQLite** for read-cache (CoA, recent transactions, profile, last dashboard snapshot) with staleness timestamps; `Preferences` for theme/non-sensitive flags.
- **Offline v1:** cached data is readable offline with an "offline / data as of …" banner; all financial posting requires connectivity (no offline voucher queue in v1).
- **UI:** central resource dictionary (colors, typography, spacing tokens), Light/Dark themes, card-based layouts, skeleton loaders, empty/error states, toast via CommunityToolkit.Maui, confirmation dialogs for destructive actions, debounced (~400 ms) server-side search.
- Permission-aware UI: nav items/buttons hidden per `GET permissions/mine` (cosmetic only — API re-checks everything).

## 11. Caching Strategy

Server-side behind `ICacheService` (so `IMemoryCache` → Redis is a config swap later). **Every key is prefixed with FamilyId** — cache can never leak across tenants.

| Data | Key pattern | TTL | Invalidated by |
|---|---|---|---|
| Permission set | `perm:{fid}:{uid}` | 15 min | role/permission/user change |
| CoA tree | `coa:{fid}` | 1 h | account create/update |
| Family settings | `settings:{fid}` | 1 h | settings update |
| Dashboard summary | `dash:{fid}` | 5 min | **any voucher post/cancel/reverse** |

Never cached: ledger rows, trial balance, any report used for accounting decisions — always computed from the database. Voucher posting invalidates `dash:{fid}` inside the same request after commit.

Client-side (SQLite): display cache only; never used as a source for calculations or posting.

## 12. Security Strategy

| Layer | Measures |
|---|---|
| Transport | HTTPS enforced, HSTS, secure headers middleware (X-Content-Type-Options, X-Frame-Options, Referrer-Policy) |
| AuthN | Identity PBKDF2 hashing, account lockout, short JWT (15 min), rotated+hashed refresh tokens with reuse detection, security stamp |
| AuthZ | Permission policies on every endpoint; nothing trusted from the client |
| Tenancy | Query filters + write interceptor + 404-not-403 + isolation tests (see §3) |
| OTP | Hashed at rest, 5-min expiry, 5 attempts, resend cooldown, per-identity + per-IP rate limits, no user enumeration |
| Rate limiting | ASP.NET Core rate limiter: tight on `auth/*`, standard elsewhere |
| Input | FluentValidation on all commands; EF parameterization (no raw SQL concatenation) |
| Output | DTOs only — `PasswordHash`, refresh tokens, internal IDs of other tenants never serialized |
| Errors | Global exception middleware → friendly message + correlation ID; full detail only to Serilog |
| Logging | Structured (Serilog → console + rolling file; sink pluggable); enriched with UserId/FamilyId/RequestId/duration; passwords/OTPs/tokens never logged |
| Config | Connection strings, JWT secret, SMTP/SMS creds via user-secrets (dev) and environment variables (prod); nothing hard-coded |
| Audit | Interceptor + explicit service for the actions listed in requirements §24 |

## 13. Database Indexing Strategy

Composite indexes lead with `FamilyId` (every tenant query filters on it):

```text
UNIQUE  Account(FamilyId, Code)
INDEX   Account(FamilyId, ParentAccountId)
UNIQUE  Voucher(FamilyId, VoucherNumber)
INDEX   Voucher(FamilyId, VoucherDate, Status)        -- lists & report ranges
INDEX   Voucher(FamilyId, VoucherType, Status)
INDEX   VoucherDetail(AccountId, VoucherId)           -- ledger per account
INDEX   VoucherDetail(VoucherId)                      -- detail load
INDEX   AppUser(FamilyId); UNIQUE Email; UNIQUE UserName; INDEX Mobile
INDEX   RefreshToken(UserId, ExpiresAt); INDEX RefreshToken(TokenHash)
INDEX   OtpCode(UserId, Purpose, ExpiresAt)
INDEX   AuditLog(FamilyId, CreatedDate); INDEX AuditLog(FamilyId, EntityName, EntityId)
INDEX   Notification(UserId, IsRead, CreatedDate)
UNIQUE  AccountingPeriod(FamilyId, StartDate)
UNIQUE  VoucherSequence(FamilyId, VoucherType, FiscalYear)
```

Plus: `decimal(18,2)` money, `RowVersion` concurrency token on Voucher/Account, `AsNoTracking()` on all reads, projection to DTOs in queries, paging pushed to SQL.

`DatabaseScripts/` is populated per phase via `dotnet ef migrations script --idempotent` (numbered files). Runtime never touches the folder.

## 14. Implementation Phases

| Phase | Scope | Exit criteria |
|---|---|---|
| **1 Foundation** | Solution, projects, DbContext, base entities, Serilog, exception middleware, ApiResponse, Swagger, health checks, config | Builds; `/health` green; Swagger up; script 001 |
| **2 Authentication** | Identity, family registration, login, JWT + refresh rotation, logout, OTP forgot-password, email/SMS abstractions | Auth flows pass integration tests |
| **3 Family & Users** | Member CRUD, per-family roles, permission catalog + policies, profile, family settings, audit log | Permission matrix tests pass; tenant isolation tests pass |
| **4 Accounting Foundation** | CoA (hierarchy, codes, seed), accounting periods, opening balances | CoA tree API; period open/close works |
| **5 Transactions** | Voucher CRUD (draft), posting engine, sequences, reversal, cancellation | Dr=Cr enforced; posting transactional; immutability proven by tests |
| **6 Reports** | Ledger (paged, running balance), cash/bank book, trial balance, P&L, balance sheet | Trial balance balances; BS equation holds in tests |
| **7 Dashboard** | Summary + chart endpoints (SQL aggregation), recent transactions | Single-query summaries; cache + invalidation wired |
| **8 MAUI** | Shell, auth flow, all pages, theming, API clients, refresh handler | App runs on Android emulator end-to-end |
| **9 Optimization** | SQLite cache, index review, query tuning, debounce, loading states | No N+1s; lists virtualized; cold start acceptable |
| **10 Hardening** | Full test pass, rate-limit tuning, security review, prod config, final DB scripts | All tests green; isolation suite green; prod checklist done |

Each phase ends with: build clean → migrations verified → endpoints smoke-tested → tests green → SQL script exported → then proceed.

---

### Deliberate decisions & trade-offs (for the record)

1. **ASP.NET Core Identity over fully custom auth** — battle-tested hashing/lockout/stamps; custom JWT+refresh layered on top. Custom-only would re-implement solved security problems.
2. **No persisted Ledger table** — reports project from posted voucher details. Guarantees consistency; a balance snapshot table can be added later behind the same interfaces if volume demands.
3. **Per-family roles with system templates** — satisfies "admin creates roles later" without cross-tenant role bleed.
4. **Permissions resolved server-side per request (cached), not in JWT** — instant permission revocation, small tokens.
5. **Reversal over edit/delete for posted vouchers** — standard accounting integrity; the audit trail is the ledger.
6. **Shared-schema tenancy with layered enforcement** — right scale for family workloads; defense in depth makes frontend trust irrelevant.
