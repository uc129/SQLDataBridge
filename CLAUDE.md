# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Run locally (from DataBridge.Api/ subdirectory)
cd DataBridge.Api && dotnet run

# Build entire solution
dotnet build DataBridge.sln

# Build for production
cd DataBridge.Api && dotnet publish -c Release -o ./publish

# Restore packages (including libman client libs)
dotnet restore DataBridge.sln
```

Default launch URLs: `https://localhost:55718` / `http://localhost:55719`.

There are no automated tests in this project.

## Solution Structure

The `ddd-migration` branch restructured the original single-project app into four projects following Clean Architecture:

```
DataBridge.Domain          — Entities, domain services, policies (no framework deps)
DataBridge.Application     — Use cases, commands, repository interfaces
DataBridge.Infrastructure  — Concrete implementations (Dapper, ClosedXML, SignalR, Azure AD)
DataBridge.Api             — ASP.NET Core 9 host: API controllers + Razor Pages UI
```

Dependency direction: `Api → Application → Domain`; `Infrastructure → Application + Domain`.

## Architecture

**Data flows:**

- **Export** (`POST /api/v1/export/run`): Controller builds `ExportCommand` → `ExportDataUseCase` calls `IExportRepository` (Dapper) + `IExcelWriter` (ClosedXML), writes to a temp folder scoped to `jobId`, sends progress via `IProgressNotifier` (SignalR). Client downloads the result via `GET /api/v1/export/download?jobId=&file=`.
- **Import** (`POST /api/v1/import/run`, multipart): Controller buffers uploaded files to `MemoryStream` snapshots → `ImportDataUseCase` calls `IExcelParser` (ExcelDataReader) to merge schemas across files, then `IImportRepository` to drop/recreate or extend the target table and `SqlBulkCopy`-insert all rows.
- **Clean** (`POST /api/v1/clean/run` or `/import-and-run`): Runs vendor and PO extraction logic defined in `DataBridge.Domain/Services/DataCleaningEngine.cs`. Column roles (vendor, PO, etc.) are resolved via `ColumnMappingPolicy` (auto-detect) with optional `ColumnMap` override passed per-request.

**Real-time progress:** `ProgressHub` (SignalR) at `/progressHub`. Jobs use `IJobRegistry` (`InMemoryJobRegistry`, singleton) to register a `CancellationTokenSource` per `jobId` and expose cancellation via `POST /api/v1/{export|import|clean}/cancel`.

**Key files:**
- `DataBridge.Application/UseCases/` — one file per use case; all business flow lives here
- `DataBridge.Application/Interfaces/` — all abstractions (repositories, parser, writer, notifier, job registry)
- `DataBridge.Domain/Services/DataCleaningEngine.cs` — pure static cleaning logic (vendor + PO extraction)
- `DataBridge.Domain/Policies/TableWhitelistPolicy.cs` — allowed table/view names for non-Admin users
- `DataBridge.Infrastructure/DependencyInjection.cs` — all infrastructure registrations in one `AddDataBridgeInfrastructure` extension
- `DataBridge.Api/Program.cs` — auth setup, use-case DI registration, hub mapping

## Configuration

`DataBridge.Api/appsettings.json`:

```json
{
  "AzureAd": { "TenantId": "...", "ClientId": "...", "ClientSecret": "..." },
  "DataBridge": {
    "DefaultOutputFolder": "C:\\DataBridge\\Output",
    "MaxRowsPerFile": 1000000,
    "FetchChunkSize": 50000,
    "MetricsViewName": "...",
    "ProxyOtp": "..."
  },
  "ConnectionStrings": { "Default": "..." }
}
```

Store the `AzureAd:ClientSecret` in **user secrets** (`dotnet user-secrets`), not in `appsettings.json`.

`web.config` sets the IIS upload limit to 500 MB. SignalR client (`@microsoft/signalr@10.0.0`) is managed via `libman.json`.

## Auth & Authorization

Authentication is Azure AD OIDC via `Microsoft.Identity.Web`. Cookie scheme is `DataBridgeAuth` (8-hour session, no sliding expiration). A `ProxyLogin` page also exists for internal proxy-based sign-in.

Authorization rules:
- All routes require authenticated user (fallback policy).
- Admin role (`ClaimTypes.Role == "Admin"`) unlocks: custom connection strings per-request, raw SQL queries on export, unrestricted table names on import.
- Non-Admin users are restricted to tables/views listed in `TableWhitelistPolicy`.
- `/api/*` paths return 401/403 status codes instead of redirecting to the login page.

## Key Conventions

- All imported data is stored as `NVARCHAR(MAX)` — intentional, to avoid type coercion across heterogeneous Excel files.
- Column names are normalized: non-alphanumeric/underscore characters replaced with `_`, lowercased, digit-prefixed names get an `_` prefix (`ColumnNameSanitizer`).
- Connection strings come in per-request from the UI for Admin users, or from `ConnectionStrings:Default` for regular users — never logged or persisted server-side.
- Long-running jobs are fire-and-forget (`_ = Task.Run(...)`) from the controller; progress is pushed over SignalR.
- Export files land in `Path.GetTempPath()/DataBridge/{jobId}/` and are deleted after the download response is sent.
- Excel export styling: bold headers in `#1F4E79` blue, alternating row shading, frozen header row, auto-filter enabled.
- `SqlBulkCopy` batch size is 500 rows; progress events fire every 5K rows on import.
