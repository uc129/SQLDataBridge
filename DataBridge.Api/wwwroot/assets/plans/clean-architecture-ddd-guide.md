# Clean Architecture & DDD — Practical Reference Guide

> Written against the DataBridge codebase. Every example is a real file in this repo.
> Generic enough to copy-paste the rules into any new project.

---

## TL;DR — The One-Line Rule Per Layer

| Layer | One-line rule | Depends on |
|---|---|---|
| **Domain** | Pure business logic. Zero external dependencies. | Nothing |
| **Application** | Defines *what* the app needs (interfaces) and *how* each use case flows (orchestration). | Domain only |
| **Infrastructure** | Implements interfaces using real technology (DB, files, NuGet). | Application + Domain |
| **API** | Handles HTTP in/out — routing, auth, request mapping. | Application + Infrastructure (startup only) |

---

## The One Hard Rule: Dependency Direction

Inner layers **never** reference outer layers.

```
┌─────────────────────────────────────┐
│  API  (Controllers, Program.cs)     │
│  ↓ references                       │
│  Application  (Use Cases, IXxx)     │
│  ↓ references                       │
│  Domain  (Engine, Policies, Rules)  │
└─────────────────────────────────────┘
          ↑ also references both
┌─────────────────────────────────────┐
│  Infrastructure  (Dapper, ClosedXML)│
└─────────────────────────────────────┘
```

Infrastructure is the odd one out: it sits *beside* the inner layers, not above them. It implements Application interfaces, so it must reference Application (and transitively Domain), but nothing references Infrastructure except at the DI wiring point in `Program.cs`.

---

## Layer 1 — Domain

### What belongs here
- Business algorithms and rules that have no dependency on *how* data is stored or delivered
- Entities (objects with identity and lifecycle)
- Value Objects (immutable data containers)
- Domain Services (stateless algorithms operating on domain concepts)
- Policies (static rules and whitelists)

### What it must NOT contain
- Any `using` statement for a NuGet package
- Any reference to file systems, databases, HTTP, or SignalR
- Any reference to Application or Infrastructure

### The test
> "Can I copy this file into a completely different app — say, a console tool or a mobile backend — and have it compile and run with zero changes?"

If yes → Domain. If no → it belongs higher up.

### DataBridge examples

**`DataBridge.Domain/Services/DataCleaningEngine.cs`**
Pure vendor and PO extraction logic. Takes a raw string, returns a cleaned value. Uses only `System.Text.RegularExpressions`. No database, no file I/O, no injected services.

**`DataBridge.Domain/Services/ColumnNameSanitizer.cs`**
Converts arbitrary column names to safe SQL identifiers. Pure string manipulation. Works identically in any project.

**`DataBridge.Domain/Policies/TableWhitelistPolicy.cs`**
A `static readonly` list of allowed table and view names for non-Admin users. Purely declarative — no logic, no I/O. Used by both the API layer (controller validation) and Application layer (use case checks).

**`DataBridge.Domain/Entities/DataBridgeUser.cs`**
Has identity (`Id`), state (`IsActive`), and a `Create()` factory that enforces invariants (e.g. email must not be empty). No persistence awareness.

### Folder conventions
```
DataBridge.Domain/
  Entities/          ← Aggregates with identity and factory methods
  Services/          ← Stateless domain algorithms
  Policies/          ← Static rules and whitelists
  Models/            ← Value objects, read models
  TradePayable/      ← Subdomain with its own Aggregates/, Contracts/, Models/
```

---

## Layer 2 — Application

### What belongs here
- **Interfaces** (contracts): the Application layer describes *what it needs* from the outside world
- **Use Cases**: one class per user-facing operation; orchestrates Domain + interfaces
- **Commands**: typed input objects for use cases (replaces loose method parameters)
- **No implementations of anything**

### What it must NOT contain
- Any NuGet package reference (only `DataBridge.Domain` as a project reference)
- Any concrete class that touches a database, file, or network
- Any `new SqlConnection(...)`, `new XLWorkbook()`, etc.

### Why interfaces live HERE, not in Domain

Domain does not know that Excel files or SQL databases exist. If `IExcelParser` were in Domain, then Domain would be saying "I need something file-related" — which breaks the zero-external-dependency rule (even an interface name implies a concern).

Application is the right home because Application *uses* the interface: `ImportDataUseCase` calls `excelParser.ParseAsync(...)`. The interface lives next to the code that depends on it.

### Why interfaces don't live in Infrastructure

If the interface lived in Infrastructure, then Application would have to reference Infrastructure to call it — reversing the dependency direction and creating a circular reference. The entire testability benefit would be lost.

```
// The pattern:
Application defines:   interface IExcelParser { Task<DataTable> ParseAsync(...); }
Infrastructure implements: class ExcelDataReaderParser : IExcelParser { ... }  ← uses ExcelDataReader NuGet
DI wires:              services.AddScoped<IExcelParser, ExcelDataReaderParser>();
```

Application never knows that `ExcelDataReader` exists. You can swap it for a `MiniExcel` implementation without touching a single line of Application code.

### The test
> "Does this describe *what* should happen, without specifying *how*?"

A use case says "fetch rows, write them to Excel, notify progress". It never says "use Dapper", "use ClosedXML", or "push to SignalR channel X". Those are Infrastructure choices.

### DataBridge examples

**`DataBridge.Application/Interfaces/IExcelParser.cs`**
Declares `ParseAsync(IEnumerable<(string name, Stream data)>)`. No mention of ExcelDataReader anywhere.

**`DataBridge.Application/Interfaces/IExportRepository.cs`**
Declares `StreamQueryAsync(connectionString, sql, ct)`. No mention of Dapper, SqlConnection, or SqlDataReader.

**`DataBridge.Application/UseCases/ExportDataUseCase.cs`**
Constructor takes four interfaces. Body calls those interfaces. Knows about the *flow* (fetch → buffer → write → notify) but has no idea how any step is physically executed.

**`DataBridge.Application/UseCases/ImportDataUseCase.cs`**
Calls `excelParser.ParseAsync(files)` then `importRepo.BulkInsertAsync(table)`. Zero awareness of ExcelDataReader or SqlBulkCopy.

### Folder conventions
```
DataBridge.Application/
  Interfaces/        ← All IXxx contracts (one interface per file)
  UseCases/          ← One file per user operation (ExportDataUseCase, ImportDataUseCase…)
  Commands/          ← Typed input objects for use cases
  Models/            ← Result types returned by use cases
```

---

## Layer 3 — Infrastructure

### What belongs here
- Concrete implementations of Application interfaces
- Anything that touches a NuGet package, database driver, file system, or external API
- The DI wiring file (`DependencyInjection.cs`)

### What it must NOT contain
- Business logic (that belongs in Domain or Application)
- HTTP routing or middleware (that belongs in API)

### The test
> "Does this file reference a NuGet package, a database driver, a file path, or a specific external service?"

If yes → Infrastructure.

### DataBridge examples

**`DataBridge.Infrastructure/Excel/ExcelDataReaderParser.cs`** → implements `IExcelParser`
Uses `ExcelDataReader` NuGet. Two-pass algorithm: collect headers, then load rows. If you wanted to switch to `MiniExcel`, you'd write a new class here and change one line in DI — Application is untouched.

**`DataBridge.Infrastructure/Excel/ClosedXmlExcelWriter.cs`** → implements `IExcelWriter`
Uses `ClosedXML` NuGet. Applies header styling, alternating row colours, freeze pane. Presentation decisions that are Infrastructure's responsibility, not the domain's.

**`DataBridge.Infrastructure/Repositories/DapperExportRepository.cs`** → implements `IExportRepository`
Opens a `SqlConnection`, executes a `SqlCommand`, streams rows via `IAsyncEnumerable`. Uses Dapper and `Microsoft.Data.SqlClient`. Handles connection lifetime and disposal. None of this concerns Application.

**`DataBridge.Infrastructure/Jobs/InMemoryJobRegistry.cs`** → implements `IJobRegistry`
Stores `CancellationTokenSource` objects in a `ConcurrentDictionary`. "In-memory" is an infrastructure detail — you could replace it with a Redis-backed registry without changing a single use case.

**`DataBridge.Infrastructure/DependencyInjection.cs`**
The single file that knows which concrete class satisfies each interface. This is the seam between Application and Infrastructure.

```csharp
public static IServiceCollection AddDataBridgeInfrastructure(
    this IServiceCollection services, IConfiguration config)
{
    services.AddSingleton<IJobRegistry,         InMemoryJobRegistry>();
    services.AddScoped<IExportRepository,        DapperExportRepository>();
    services.AddScoped<IExcelWriter,             ClosedXmlExcelWriter>();
    services.AddScoped<IExcelParser,             ExcelDataReaderParser>();
    services.AddScoped<IProgressNotifier,        SignalRProgressNotifier>();
    // ...
}
```

Called once from `Program.cs`. The API layer never sees any of these concrete types.

### Folder conventions
```
DataBridge.Infrastructure/
  Excel/             ← IExcelParser and IExcelWriter implementations
  Repositories/      ← IXxxRepository implementations (Dapper)
  Jobs/              ← IJobRegistry implementation
  SignalR/           ← IProgressNotifier implementation
  Auth/              ← IProxyAuthService, Azure AD enricher
  DependencyInjection.cs  ← The only file that names concrete classes
```

---

## Layer 4 — API

### What belongs here
- ASP.NET controllers: translate HTTP requests into Application commands, return HTTP responses
- `Program.cs`: configure middleware, authentication, and call `AddDataBridgeInfrastructure()`
- Razor Pages / Views for any server-rendered UI
- Request/response DTO models specific to HTTP (distinct from domain models)

### What it must NOT contain
- Business logic (belongs in Domain/Application)
- Database queries (belongs in Infrastructure)
- Anything that would need to change if you added a gRPC endpoint for the same operation

### The test
> "Would this file need to change if I added a CLI front-end for the same feature?"

If yes → it's HTTP-specific detail and belongs in API. The use case itself should not change.

### DataBridge examples

**`DataBridge.Api/Controllers/ExportController.cs`**
Receives the HTTP POST, validates the request, enforces the `TableWhitelistPolicy` for non-Admins, builds an `ExportCommand`, fires it as a background task, returns `202 Accepted`. The controller does *not* know how data is fetched or how Excel files are written.

**`DataBridge.Api/Program.cs`**
Configures Kestrel upload limits, registers Azure AD cookie auth, maps SignalR hub, and calls `builder.Services.AddDataBridgeInfrastructure(config)` — the one line that activates the entire Infrastructure layer.

### Folder conventions
```
DataBridge.Api/
  Controllers/       ← One controller per feature area
  Pages/             ← Razor Pages for server-rendered UI
  Requests/          ← HTTP-specific request DTOs
  wwwroot/           ← Static files
  Program.cs         ← App bootstrap and DI composition root
```

---

## The Interface Lifecycle — Full Walkthrough

Here is the complete journey of `IExcelParser`, from need to runtime:

```
1. NEED identified in Application:
   ImportDataUseCase needs to read Excel files.
   → Define interface in Application/Interfaces/IExcelParser.cs

2. CONTRACT written in Application:
   public interface IExcelParser {
       Task<(IReadOnlyList<string> Columns, DataTable Data)>
           ParseAsync(IEnumerable<(string Name, Stream Data)> files,
                      CancellationToken ct = default);
   }

3. IMPLEMENTATION written in Infrastructure:
   class ExcelDataReaderParser : IExcelParser   // uses ExcelDataReader NuGet
   {
       public async Task<...> ParseAsync(...) { ... }
   }

4. WIRED in Infrastructure/DependencyInjection.cs:
   services.AddScoped<IExcelParser, ExcelDataReaderParser>();

5. CONSUMED in Application/UseCases/ImportDataUseCase.cs:
   public class ImportDataUseCase(IExcelParser excelParser, ...)
   {
       var (columns, data) = await excelParser.ParseAsync(files, ct);
   }

6. SWAPPABLE: Want to try MiniExcel?
   Write MiniExcelParser : IExcelParser in Infrastructure.
   Change one line in DependencyInjection.cs.
   ImportDataUseCase is never touched.
```

---

## Domain Service vs Application Service vs Infrastructure Service

This naming confusion trips people up. Here is the distinction:

| Name | What it is | Has external deps? | Has interfaces? | DataBridge example |
|---|---|---|---|---|
| **Domain Service** | Stateless algorithm operating on domain data | No | No | `DataCleaningEngine` |
| **Application Service** (Use Case) | Orchestrates domain + interfaces for one user request | No | Consumes them | `ExportDataUseCase` |
| **Infrastructure Service** | Concrete implementation using external technology | Yes | Implements one | `ExcelDataReaderParser` |

**Common mistake:** "This is business logic, so it goes in Domain."

Ask yourself: does it call any interface? Does it need a database result or a file? If yes, it's an Application Service (Use Case), not a Domain Service. Domain Services are self-contained — they receive all their inputs as parameters and compute a result.

---

## Decision Tree — "Where Does This Class Go?"

```
Does it have zero external dependencies AND operate only on primitive
types / domain objects (no interfaces, no I/O)?
  YES → Domain

Does it define a contract (interface) for something the app needs,
or orchestrate a workflow by calling interfaces?
  YES → Application

Does it implement a contract using a NuGet package, SQL driver,
file system, HTTP client, or external service?
  YES → Infrastructure

Does it handle HTTP requests, configure middleware, or map
routes to use cases?
  YES → API
```

If a class seems to fit two categories, split it: extract the pure logic into Domain, leave the orchestration in Application, leave the I/O in Infrastructure.

---

## Applying This to a New Project

1. **Create four projects** with the same dependency constraints in the `.csproj` files:
   - `MyApp.Domain` — no ProjectReferences, no PackageReferences (except system)
   - `MyApp.Application` — references only `MyApp.Domain`
   - `MyApp.Infrastructure` — references `MyApp.Application` + `MyApp.Domain` + all NuGet packages
   - `MyApp.Api` — references `MyApp.Application` + `MyApp.Infrastructure`

2. **Start with the Domain.** Write your business rules as pure C# with no NuGet packages.

3. **Write use cases in Application.** For each external thing a use case needs (database, file, email, etc.), define an interface. Do not implement it yet.

4. **Implement interfaces in Infrastructure.** Pick your NuGet packages here.

5. **Wire everything in `DependencyInjection.cs`** inside Infrastructure. Call it from `Program.cs` in the API project.

6. **Controllers in API** receive HTTP input, build a Command or query object, call the use case, return the HTTP response. Nothing else.

The build system enforces the rules: if you accidentally add a reference in the wrong direction, the project will not compile.
