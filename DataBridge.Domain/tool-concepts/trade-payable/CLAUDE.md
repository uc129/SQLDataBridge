# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the entire solution
dotnet build TradePayablesReporting_WebApp.sln

# Run the web application (from repo root)
dotnet run --project TradePayablesReporting_WebApp/TradePayablesReporting_WebApp.csproj

# Publish for Windows x86 deployment
dotnet publish TradePayablesReporting_WebApp/TradePayablesReporting_WebApp.csproj -c Release -r win-x86 --self-contained false
```

There are no automated tests in this repository.

## Solution Structure

Five projects with a clean architecture pattern:

| Project | Role |
|---|---|
| `Domain` | Entities, aggregates, contracts/interfaces, view models |
| `Application` | Business logic — process steps, data processors, services, AutoMapper |
| `Infrastructure` | Dapper-based repository implementations, DB context, SQL table config |
| `Shared` | Shared utilities and extension methods |
| `SPO` | SharePoint Online integration (file upload/retrieval) |
| `TradePayablesReporting_WebApp` | ASP.NET Core MVC web host — controllers, views, middleware, DI wiring |

## Key Architecture Concepts

### 14-Step Data Processing Pipeline
The core business logic runs FAGLL03 SAP data through 14 sequential `IProcessStep` implementations (Steps 0–13), orchestrated by `DataProcessingService.RunStepsUpTo(targetStep, state)`. Each step reads from prior step's SQL table, processes data, and writes results to named step tables (`Step_00_...` through `Step_12_...`). Backup tables mirror each step result.

Step registration order in `InjectDependencies.AddProcessSteps()` must match `StepIndex` values on each class — the orchestrator sorts by `StepIndex` at runtime.

**Step summary:**
- Steps 0–2: Load raw FAGLL03 data → populate → merge with ICP/PO data
- Steps 3–5: GIT Advance processing (local currency)
- Step 6: Append trade + net liability
- Steps 7–9: GIT Advance processing (document currency)
- Step 10: Append trade + doc-currency net liability
- Step 11: Merge trade and SNA balance data
- Step 12: Fix CP ageing + Hyperion classification (final result table)
- Step 13: Save process run summary

### ProcessState (Session-based)
`ProcessState` (in `Domain/Models/ProcessRun/`) is serialized into the ASP.NET session under key `CurrentProcessState`. It carries `ProcessId` (Guid), `CurrentStepIndex`, `CurrentQuarter`, and user info. `SessionInitMiddleware` creates it on first authenticated request; `DataController` reads/updates it per page navigation.

### Dual Authentication
Two auth schemes run in parallel:
1. **Azure AD SSO** (`OpenIdConnect`) — for corporate LTEH users; identity enriched via `CustomClaimsTransformer` which queries the SQL `TradePayables_Authorized_Users` table to add role claims (`FullName`, `PSNO`, `Id_IC`, `RoleCode`, etc.)
2. **Manual cookie auth** (scheme `"auth"`) — PSNO + hardcoded OTP `"111111"` via `AuthController.Login`

### Role-Based Access
Roles come from the SQL users table, not Azure AD groups. The `RoleRestrictionMiddleware` hard-redirects `SNA_APPROVER` role users to only the SNA balance approval pages. Other roles (`ADMIN`, `ADMIN_F&A`) are checked inline in controllers.

### GLHyperionMapper (Singleton Static Cache)
`GLHyperionMapper` initializes a static in-memory dictionary at startup (`Program.cs` startup scope call to `GLHyperionMapper.InitializeAsync`). All step logic calls `GLHyperionMapper.GetMapping(glCode)` — do not re-initialize at runtime.

### Database Access
All database access uses Dapper via `DapperContext`, which resolves named connection strings from `appsettings.json`. Three connection strings are used:
- `default` — primary `TradeMSEDDetails_UAT` database (all step tables, master tables, users)
- `lthe_invoice_tracking` — cross-server PO/invoice data source
- `lnt_po_data` — cross-server PO vendor data

Table names are never hardcoded — they are resolved via `IDataSettings` (bound from `DataConfiguration` in `appsettings.json`). Use `_datasettings.GetTableConfigDataByKey("Step_XX")` pattern to resolve table names.

### Master Tables
Static GL master tables (Advance GLs, Capital GLs, Insurance GLs, Liability GLs, MSME codes, etc.) are managed via `StaticMasterTableService` partial classes. Each partial file handles one master table type. The `MASTER_TradePayables_*` SQL tables store these.

### Excel Upload Flow
`ExcelUploadController` accepts FAGLL03 raw data Excel files, reads them via `ExcelReaderService.ReadExcelToDataTable`, then calls `ExcelUploadService.UploadFAGLL03RawData` to stage data, and finally uploads the source file to SharePoint via the `SPO` project.

## Configuration Notes

- `appsettings.json` contains actual connection strings and SharePoint credentials (not secrets-managed in dev) — use `appsettings.Development.json` overrides for local dev
- AzureAd `ClientSecret` must be set via user secrets or Azure Key Vault — the placeholder in `appsettings.json` is not functional
- Session timeout is 20 minutes (`Program.cs`)
- Default route goes to `Admin/Index` (the dashboard)
