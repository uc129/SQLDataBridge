# Trade Payable Pipeline Tool — DataBridge Implementation Plan

## Context

The user has a separate Trade Payables Reporting web app that processes SAP FAGLL03 Excel exports through a 14-step pipeline to produce quarterly trade payable reports (advance deductions, Hyperion classifications, MSME categorisation, ageing analysis). The goal is to port this pipeline into DataBridge as a first-class tool alongside the existing Export, Import, and Clean tools.

**Scope confirmed:**
- Full 14-step pipeline
- Master table management (13 static master tables, CRUD)
- No SNA approval workflow
- Step-by-step OR full pipeline run
- Excel download report for each step's output
- Revision numbers R01/R02/R03
- Dedicated FAGLL03 upload page
- Two extra connection strings (PO credit period + vendor/ICP cross-server sources)

---

## Architecture Overview

### Where code lives
All new code slots into the **existing four DataBridge projects** under a `TradePayable/` subdirectory in each:

```
DataBridge.Domain/TradePayable/
DataBridge.Application/TradePayable/
DataBridge.Infrastructure/TradePayable/
DataBridge.Api/Pages/TradePayable/   +   Controllers/TradePayableController.cs
```

No new projects. Namespaces: `DataBridge.Domain.TradePayable`, `DataBridge.Application.TradePayable`, etc.

### Execution model
Adapts the original session-based `ProcessState` to DataBridge's **jobId fire-and-forget** pattern:
- A `PipelineRun` record in SQL tracks: `RunId` (Guid), `QuarterDate`, `RevisionNumber`, `CurrentStepIndex`, `Status`
- The Razor page holds the active `RunId` in JS and uses it as the SignalR `jobId`
- Steps are idempotent: each step checks whether its result table already has rows for this `RunId` and skips if so
- Step-by-step: user triggers one step at a time, waits for completion, then triggers the next
- Full pipeline: a single job runs all steps sequentially, broadcasting progress after each

---

## Phase 1 — Foundation (Domain + Configuration)

### 1a. Domain entities
Port the aggregate hierarchy and master table entities from the concept files into `DataBridge.Domain/TradePayable/`.

**Aggregate chain** (each extends the previous via inheritance, as in source):
```
FAGLL03RAWEntity → FAGLL03Populated → FAGLL03JoinedAndMerged →
FAGLL03ProcessedGITLocal → FAGLL03NetLiability → FAGLL03NetCPFixed →
FAGLL03ProcessedResult
```

**Master table entities** (13 plain POCOs):
`AdvanceGLs`, `LiabilityGLs`, `NotDueGLs`, `MSMECompanyCodes`, `CapitalCreditorGLs`,
`InsuranceGLs`, `NonMSMEGLs`, `UnclaimedGLs`, `GLHyperionMap`, `ICPHyperionMap`,
`ICPVendorMap`, `ForexMonthEndMap`, `AgeingGroup`

**Pipeline model:**
```
DataBridge.Domain/TradePayable/Models/PipelineRun.cs
  RunId (Guid), QuarterDate, RevisionNumber, CurrentStepIndex, Status (enum), StartedBy, StartedAt, CompletedAt

DataBridge.Domain/TradePayable/Models/ProcessState.cs
  RunId, QuarterDate, CurrentQuarter, RevisionNumber, CurrentStepIndex, NextStepIndex, UserName
```

**Interfaces (contracts):**
```
IProcessStep          — int StepIndex; Task<ProcessState> ExecuteAsync(ProcessState)
IDataProcessingService — Task<ProcessState> RunStepsUpTo(int target, ProcessState)
IStepResultRepository  — SaveAndReplaceStepResultAsync, RetrieveStepResultAsync, RetrieveStepResultAsIEnumerableAsync<T>
IBackupTablesRepository
IMasterTableRepository<T>  — GetAllAsync, UpsertAsync, DeleteAsync
IPipelineRunRepository — CreateAsync, GetByRunIdAsync, UpdateStepIndexAsync, UpdateStatusAsync, GetAllAsync
```

### 1b. Configuration
**`appsettings.json`** — add alongside the existing `ConnectionStrings.Default`:
```json
"ConnectionStrings": {
  "Default": "...",
  "LtheInvoiceTracking": "...",
  "LntPoData": "..."
},
"TradePayable": {
  "DatabaseName": "TradeMSEDDetails",
  "StepTables": {
    "Step_00": "TP_Step_00", "Step_01": "TP_Step_01",
    "Step_02": "TP_Step_02", "Step_03": "TP_Step_03",
    "Step_04": "TP_Step_04", "Step_05": "TP_Step_05",
    "Step_06": "TP_Step_06", "Step_07": "TP_Step_07",
    "Step_08": "TP_Step_08", "Step_09": "TP_Step_09",
    "Step_10": "TP_Step_10", "Step_11": "TP_Step_11",
    "Step_12": "TP_Step_12",
    "Step_3_1": "TP_Step_03_AllGrouped",
    "Step_7_1": "TP_Step_07_AllGrouped"
  },
  "BackupTables": { ... same keys prefixed with Backup_ ... },
  "MasterTables": {
    "AdvanceGLs":        "TP_Master_AdvanceGLs",
    "LiabilityGLs":      "TP_Master_LiabilityGLs",
    "NotDueGLs":         "TP_Master_NotDueGLs",
    "MSMECompanyCodes":  "TP_Master_MSMECompanyCodes",
    "CapitalCreditorGLs":"TP_Master_CapitalCreditorGLs",
    "InsuranceGLs":      "TP_Master_InsuranceGLs",
    "NonMSMEGLs":        "TP_Master_NonMSMEGLs",
    "UnclaimedGLs":      "TP_Master_UnclaimedGLs",
    "GLHyperionMap":     "TP_Master_GLHyperionMap",
    "ICPHyperionMap":    "TP_Master_ICPHyperionMap",
    "ICPVendorMap":      "TP_Master_ICPVendorMap",
    "ForexMonthEndMap":  "TP_Master_ForexMonthEndMap",
    "AgeingGroup":       "TP_Master_AgeingGroup"
  }
}
```

Bind to a `TradePayableSettings` POCO; register as `services.Configure<TradePayableSettings>(config.GetSection("TradePayable"))`.

### 1c. SQL setup script
One idempotent SQL script (`DataBridge.Infrastructure/TradePayable/SqlSetup/create_tables.sql`) with `IF NOT EXISTS` guards for:
- `TP_PipelineRun` (pipeline state tracking)
- `TP_FAGLL03_Raw` (staging table for uploaded Excel data)
- `TP_Step_00` through `TP_Step_12` + their `TP_Backup_*` mirrors
- All 13 `TP_Master_*` tables with appropriate column definitions

---

## Phase 2 — Infrastructure (Repositories + DapperContext extension)

### 2a. Connection string resolver
`DataBridge.Infrastructure/TradePayable/TradePayableDbContext.cs`
Wraps `IConfiguration` to resolve the three connection strings by name. The existing `IDbConnectionFactory` pattern in DataBridge only handles `Default`; add a `GetTradePayableConnection()` and `GetCrossServerConnection(string name)` method.

### 2b. Repositories
All Dapper-based, following the existing `DapperImportRepository` / `DapperExportRepository` pattern.

```
DataBridge.Infrastructure/TradePayable/Repositories/
  PipelineRunRepository.cs       — CRUD on TP_PipelineRun
  FAGLL03StagingRepository.cs    — bulk insert raw data, get by RunId
  StepResultRepository.cs        — SaveAndReplace / Retrieve for TP_Step_* tables
                                    (uses DataTable bulk insert via SqlBulkCopy, same as ImportRepository)
  BackupTablesRepository.cs      — append-only writes to TP_Backup_* tables
  CrossServerPORepository.cs     — reads PO credit periods from LtheInvoiceTracking connection
  CrossServerVendorRepository.cs — reads vendor/ICP data from LntPoData connection
  MasterTableRepository.cs       — generic Dapper repo for all 13 TP_Master_* tables
                                    (one class, table name passed via IOptions<TradePayableSettings>)
```

**StepResultRepository key methods:**
```csharp
Task SaveAndReplaceStepResultAsync(DataTable data, Guid runId, int stepIndex)
Task<DataTable> RetrieveStepResultAsync(Guid runId, int stepIndex)
Task<IEnumerable<T>> RetrieveStepResultAsIEnumerableAsync<T>(Guid runId, int stepIndex)
```

Reuse `SqlBulkCopy` pattern from existing `DapperImportRepository` for bulk saves.

### 2c. DI extension
`DataBridge.Infrastructure/TradePayable/DependencyInjection.TradePayable.cs`
```csharp
public static IServiceCollection AddTradePayableInfrastructure(this IServiceCollection services, IConfiguration config)
```
Registers all repos as Scoped. Called from the main `AddDataBridgeInfrastructure` extension.

---

## Phase 3 — Application (Processing Engine + Steps)

### 3a. Port processing classes
These are direct ports of the concept files with minimal adaptation (namespace change, DI injection adjustment):

```
DataBridge.Application/TradePayable/Processing/
  HelperFunctions.cs            — master table lookups, date helpers, DataTable utilities
  GLHyperionMapper.cs           — static singleton cache; InitializeAsync called at app startup
  DataProcessor.cs              — main business logic (vendor/PO extraction, ageing, Hyperion, ERV, journal entry)
  GITHelper.cs                  — grouping, pivoting, cascaded join, unpivot
  GITProcessor.cs               — local currency GIT advance pipeline
  GITDocCurrHelper.cs           — doc currency variant helpers
  GITDocCurrProcessor.cs        — doc currency GIT advance pipeline
  ReportingLogicFunctions.cs    — summary generation helpers
```

**GLHyperionMapper startup call** — add to `Program.cs`:
```csharp
using var initScope = app.Services.CreateScope();
var glRepo = initScope.ServiceProvider.GetRequiredService<IGLHyperionMapRepository>();
await GLHyperionMapper.InitializeAsync(glRepo);
```

### 3b. Master table service
`DataBridge.Application/TradePayable/Services/StaticMasterTableService.cs`
Partial class pattern (one partial per master table type, exactly as in source). Injected into `HelperFunctions`.

`CrossServerMasterTablesService.cs` — wraps the two cross-server repos for Step 2 merge.

### 3c. DataProcessingService (orchestrator)
`DataBridge.Application/TradePayable/Services/DataProcessingService.cs`
Direct port — sorts registered `IProcessStep` implementations by `StepIndex`, runs `RunStepsUpTo(target, state)`.

### 3d. Process Steps
Each step is a direct port from the concept files. Steps 3/4/5 and 7/8/9 are each handled by a single `IProcessStep` class (as in source — one step class saves to multiple step indices).

```
DataBridge.Application/TradePayable/Steps/
  Step00_GetRawDataStep.cs
  Step01_PopulateRawDataStep.cs
  Step02_GetMergedDataStep.cs
  Step03_ProcessGITLocalStep.cs        (StepIndex=3; saves to indices 3,4,5)
  Step06_AppendTradeNetLiabilityStep.cs
  Step07_ProcessGITDocCurrStep.cs      (StepIndex=7; saves to indices 7,8,9)
  Step10_AppendTradeDocCurrNetLiabilityStep.cs
  Step11_MergeTradeAndSNADataStep.cs
  Step12_FixCPAgeingHyperionStep.cs
  Step13_SaveProcessSummaryStep.cs
```

### 3e. Use Cases

```
DataBridge.Application/TradePayable/UseCases/
  UploadFAGLL03UseCase.cs
    — Reads Excel via existing IExcelParser
    — Validates column structure
    — Bulk inserts to TP_FAGLL03_Raw with RunId + RevisionNumber
    — Creates PipelineRun record (status: Uploaded)

  RunPipelineStepUseCase.cs
    — Accepts RunPipelineStepCommand { RunId, TargetStepIndex, JobId }
    — Registers job in IJobRegistry (reuse existing)
    — Loads PipelineRun, builds ProcessState
    — Calls DataProcessingService.RunStepsUpTo(targetStepIndex, state)
    — Broadcasts SignalR progress after each step via IProgressNotifier
    — Updates PipelineRun.CurrentStepIndex in DB on completion
    — On complete: writes step result table to temp Excel via IExcelWriter, sends OutputFiles in final ProgressMessage

  RunFullPipelineUseCase.cs
    — Same as RunPipelineStepUseCase but TargetStepIndex = 13
    — Progress messages include step name + overall percent (step N of 13)

  DownloadStepReportUseCase.cs
    — Queries step result table for RunId
    — Writes to temp Excel file using IExcelWriter (reuse existing)
    — Returns file path

  GetPipelineRunsUseCase.cs
    — Returns list of PipelineRun records for the index page

  GetPipelineStateUseCase.cs
    — Returns current PipelineRun + which steps are complete (by checking row counts in step tables)

  MasterTableUseCase.cs
    — GetAllAsync<T>(tableName), UpsertAsync<T>(record, tableName), DeleteAsync(id, tableName)
```

**Commands:**
```
UploadFAGLL03Command  { JobId, QuarterDate, RevisionNumber, Stream FileName }
RunPipelineStepCommand { RunId, TargetStepIndex, JobId }
RunFullPipelineCommand { RunId, JobId }
```

---

## Phase 4 — API Layer

### 4a. Controller
`DataBridge.Api/Controllers/TradePayableController.cs`

```
POST /api/v1/tradepayable/upload         — upload FAGLL03 Excel
POST /api/v1/tradepayable/run-step       — run one step (body: { runId, stepIndex, jobId })
POST /api/v1/tradepayable/run-pipeline   — run all steps (body: { runId, jobId })
POST /api/v1/tradepayable/cancel         — cancel running job
GET  /api/v1/tradepayable/download       — ?runId=&step=  → Excel download, delete after send
GET  /api/v1/tradepayable/state          — ?runId=  → pipeline state JSON
```

Auth: existing `[Authorize]` attribute; Admin check for any destructive master table writes (same pattern as CleanController).

### 4b. Razor Pages

**`DataBridge.Api/Pages/TradePayable/Index.cshtml`**
- Lists all pipeline runs (RunId, Quarter, Revision, Status, Steps Complete)
- "Start New Run" button → opens Run page

**`DataBridge.Api/Pages/TradePayable/Run.cshtml`**
Layout:
```
┌─────────────────────────────────────────────────────┐
│  Trade Payable Pipeline                             │
│  Quarter: [date picker]  Revision: [R01▾]  [Upload] │
├─────────────────────────────────────────────────────┤
│  Steps                    │  Progress / Output      │
│  ○ Step 0: Get Raw Data   │  [SignalR progress bar] │
│  ○ Step 1: Populate       │  [Step log messages]    │
│  ○ Step 2: Merge ICP/PO   │                         │
│  ✓ Step 3-5: GIT Local    │  [▼ Download Step 3]    │
│  ...                      │                         │
│  [Run Next Step] [Run All] │                        │
└─────────────────────────────────────────────────────┘
```

JS behaviour:
- Step list shows ✓ (complete) / ● (active) / ○ (pending) per step
- "Run Next Step" fires `run-step` with `targetStepIndex = currentStepIndex + 1`
- "Run All" fires `run-pipeline`
- Both use same SignalR setup as existing pages: `setupSignalR(jobId)` → `connection.on('progress', ...)`
- On `isComplete`: show download button for that step + unlock next step button
- Step progress percent = `(stepsCompleted / 13) * 100` for full pipeline; `0→100` for single step

**`DataBridge.Api/Pages/TradePayable/Masters.cshtml`**
- Tabbed interface, one tab per master table (13 tabs)
- Each tab: table grid showing current rows + Add row form + Delete button per row
- Thin JSON API endpoints (part of TradePayableController or a MastersController) for CRUD
- No complex validation needed — admin-only page

### 4c. Nav addition
Add "Trade Payable" link to the existing nav in `DataBridge.Api/Pages/Shared/_Layout.cshtml`.

---

## Phase 5 — DI Wiring

**`DataBridge.Api/Program.cs`** additions:
```csharp
// Config binding
builder.Services.Configure<TradePayableSettings>(builder.Configuration.GetSection("TradePayable"));

// Application use cases
builder.Services.AddScoped<UploadFAGLL03UseCase>();
builder.Services.AddScoped<RunPipelineStepUseCase>();
builder.Services.AddScoped<RunFullPipelineUseCase>();
builder.Services.AddScoped<DownloadStepReportUseCase>();
builder.Services.AddScoped<GetPipelineRunsUseCase>();
builder.Services.AddScoped<GetPipelineStateUseCase>();
builder.Services.AddScoped<MasterTableUseCase>();

// Processing engine
builder.Services.AddScoped<DataProcessingService>();
builder.Services.AddScoped<DataProcessor>();
builder.Services.AddScoped<HelperFunctions>();
builder.Services.AddScoped<GITProcessor>();
builder.Services.AddScoped<GITHelper>();
builder.Services.AddScoped<GITDocCurrProcessor>();
builder.Services.AddScoped<GITDocCurrHelper>();
builder.Services.AddScoped<StaticMasterTableService>();
builder.Services.AddScoped<CrossServerMasterTablesService>();

// Steps (all registered as IProcessStep so DataProcessingService gets IEnumerable<IProcessStep>)
builder.Services.AddScoped<IProcessStep, Step00_GetRawDataStep>();
builder.Services.AddScoped<IProcessStep, Step01_PopulateRawDataStep>();
// ... all 10 step classes

// GLHyperionMapper startup init (after app.Build())
using var initScope = app.Services.CreateScope();
await GLHyperionMapper.InitializeAsync(
    initScope.ServiceProvider.GetRequiredService<IGLHyperionMapRepository>());
```

**`DataBridge.Infrastructure/DependencyInjection.cs`** — call `AddTradePayableInfrastructure(config)` at end of `AddDataBridgeInfrastructure`.

---

## Implementation Order

1. **Domain entities + contracts + config POCO** — no dependencies, builds immediately
2. **SQL setup script** — run once to create all TP_ tables
3. **Infrastructure repos** — Dapper implementations, test connection strings
4. **Processing engine** — port HelperFunctions, GLHyperionMapper, DataProcessor, GITProcessor, GITDocCurrProcessor
5. **StaticMasterTableService + steps** — depend on processing engine + repos
6. **Use cases** — depend on everything above
7. **Controller + Razor pages** — UI layer last
8. **DI wiring + GLHyperionMapper startup init**

---

## Files Modified (existing)

| File | Change |
|---|---|
| `DataBridge.Api/Program.cs` | Add ~30 DI registrations + GLHyperionMapper init call |
| `DataBridge.Infrastructure/DependencyInjection.cs` | Call `AddTradePayableInfrastructure` |
| `DataBridge.Api/Pages/Shared/_Layout.cshtml` | Add nav link |
| `DataBridge.Api/appsettings.json` | Add 2 connection strings + TradePayable config section |

All other changes are **new files only**.

---

## Reused DataBridge Infrastructure (do not reimplement)

| Existing | Reused for |
|---|---|
| `IExcelParser` / `ExcelDataReaderParser` | Reading uploaded FAGLL03 Excel |
| `IExcelWriter` / `ClosedXmlExcelWriter` | Writing step result Excel downloads |
| `IProgressNotifier` / `SignalRProgressNotifier` | Step progress broadcasts |
| `IJobRegistry` / `InMemoryJobRegistry` | Job cancellation |
| `ProgressHub` + SignalR client JS | Progress UI on Run page |
| `SqlBulkCopy` pattern in `DapperImportRepository` | Bulk saving step results |
| `CancelJobUseCase` | Cancel running step/pipeline |

---

## Verification

1. Upload a FAGLL03 Excel → row appears in `TP_FAGLL03_Raw`, `TP_PipelineRun` created
2. Run Step 0 → `TP_Step_00` populated, SignalR shows 100%, download button appears, Excel downloads correctly
3. Run Step 3 → `TP_Step_03`, `TP_Step_04`, `TP_Step_05` all populated (three tables from one step)
4. Run full pipeline from fresh upload → all 13 step tables populated in sequence, final summary in Step 13
5. Re-run a completed step → step is skipped (idempotent check), state unchanged
6. Navigate to Masters page → all 13 tables editable; add/delete row persists in `TP_Master_*`
7. Cancel mid-run → job stops, `TP_PipelineRun.Status` set to Cancelled
