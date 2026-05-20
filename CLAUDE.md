# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Run locally (from DataBridge/ subdirectory)
dotnet run

# Build for production
dotnet publish -c Release -o ./publish

# Restore packages (including libman client libs)
dotnet restore
```

Default launch URLs: `https://localhost:61900` / `http://localhost:61901` (IIS Express profile).

There are no automated tests in this project.

## Architecture

DataBridge is an ASP.NET Core 9 Razor Pages app that moves data between SQL Server and Excel files. Two core services do the heavy lifting; the pages are thin wrappers around them.

**Data flow:**

- **Export** (`/Export`): User submits a SQL query or view name → `SqlExportService` streams rows in configurable chunks (default 50K), writes to ClosedXML workbook, auto-splits at 1M rows per file, sends real-time progress via SignalR.
- **Import** (`/Import`): User uploads `.xlsx`/`.xls` files → `ExcelImportService` merges schemas across all files, normalizes column names for SQL safety, drops/recreates or appends to the target table, bulk-inserts via `SqlBulkCopy`.
- **Connection test**: `POST /api/test-connection` (in `Pages/ApiController.cs`) validates a connection string before long-running jobs start.

**Real-time progress:** `ProgressHub` (SignalR) at `/progressHub`. Each job gets a unique `jobId`; the client joins group `{jobId}` and receives `ProgressMessage` objects with stage, percent, row counts, and completion/error flags. Cancellation tokens are tracked in a `ConcurrentDictionary<string, CancellationTokenSource>` on both page models.

**Key files:**
- `Services/SqlExportService.cs` — export engine (chunked streaming, Excel formatting, SignalR progress)
- `Services/ExcelImportService.cs` — import engine (schema merge, column normalization, bulk copy)
- `Hubs/ProgressHub.cs` — SignalR hub (join/leave group methods only)
- `Models/Models.cs` — DTOs: `ExportRequest`, `ImportRequest`, `ProgressMessage`, `JobResult`
- `Program.cs` — DI registration, middleware, hub route mapping
- `appsettings.json` — `DataBridge:DefaultOutputFolder`, `MaxRowsPerFile` (1M), `FetchChunkSize` (50K)

## Configuration

```json
{
  "DataBridge": {
    "DefaultOutputFolder": "C:\\DataBridge\\Output",
    "MaxRowsPerFile": 1000000,
    "FetchChunkSize": 50000
  }
}
```

`web.config` sets the IIS upload limit to 500 MB. SignalR client library (`@microsoft/signalr@10.0.0`) is managed via `libman.json` and lives in `wwwroot/lib/microsoft/signalr/`.

## Key Conventions

- All imported data is stored as `NVARCHAR(MAX)` — intentional, to avoid type coercion failures across heterogeneous Excel files.
- Column names are normalized: non-alphanumeric/underscore characters replaced with `_`, lowercased, digit-prefixed names get an `_` prefix.
- Connection strings are treated as secrets: never log them, never persist them server-side (they come in per-request from the UI).
- Excel export styling: bold headers in `#1F4E79` blue, alternating row shading, frozen header row, auto-filter enabled.
- `SqlBulkCopy` batch size is 500 rows; progress events fire every 5K rows on import, every 10K rows on export.
