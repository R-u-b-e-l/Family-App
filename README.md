# Family Management & Accounting System

Multi-family financial management with proper double-entry accounting.
**Backend:** ASP.NET Core Web API (.NET 10) + EF Core + SQL Server · **Mobile:** .NET MAUI (Android).

Full design decisions: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

## Solution layout

```
src/
  FamilyManagement.Domain          Entities (no dependencies)
  FamilyManagement.Shared          DTOs / enums / ApiResponse — shared with MAUI
  FamilyManagement.Application     Business services, validators, accounting engine
  FamilyManagement.Infrastructure  EF Core, Identity, JWT, migrations, seed data
  FamilyManagement.API             Controllers, middleware, Swagger, rate limiting
  FamilyManagement.Maui            Android app (MVVM, Shell, SQLite cache)
tests/
  FamilyManagement.UnitTests        Accounting engine + tenant guard tests
  FamilyManagement.IntegrationTests Real-API tests incl. tenant isolation
DatabaseScripts/                    Idempotent SQL per migration (never read at runtime)
```

## Run the API

Requirements: .NET 10 SDK, SQL Server (local default instance).

```powershell
# create/update the database (uses ConnectionStrings:DefaultConnection)
dotnet ef database update --project src/FamilyManagement.Infrastructure --startup-project src/FamilyManagement.API

dotnet run --project src/FamilyManagement.API
# Swagger: http://localhost:5005/swagger   Health: http://localhost:5005/health
```

Configuration lives in `appsettings.json`; the dev JWT secret is in
`appsettings.Development.json`. In production set `Jwt__Secret` and
`ConnectionStrings__DefaultConnection` through environment variables.

## Run the Android app

Requirements: MAUI Android workload, an Android emulator or device.

```powershell
dotnet build src/FamilyManagement.Maui -f net10.0-android -t:Run
```

- The app defaults to `http://10.0.2.2:5005` (emulator → host machine).
  Change it anytime under **More → API Server** (e.g. your PC's LAN IP for a real device).
- First screen: register a family — the first user becomes **FamilyAdmin**
  with a seeded Chart of Accounts and the current-year accounting period.

## Tests

```powershell
dotnet test            # unit + integration (integration uses FamilyManagementDb_IntegrationTests)
```

## Key rules baked into the engine

- Every voucher must balance (`ΣDebit == ΣCredit`) — enforced in validation,
  service re-checks, and database CHECK constraints.
- Posted vouchers are immutable; corrections go through reversal vouchers.
- Posting runs in one DB transaction incl. concurrency-safe voucher numbering.
- Tenant isolation: `FamilyId` claim → EF global query filters → write-guard
  interceptor; cross-family IDs return **404**.
- OTP password reset: hashed codes, 5-minute expiry, 5 attempts, resend cooldown.
- Email/SMS are behind `IEmailService`/`ISmsService` (dev default logs to console —
  plug in your SMTP/SMS provider in `FamilyManagement.Infrastructure/Common/CommonServices.cs`).
