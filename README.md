# DataBridge

ASP.NET Core 9 web app for moving data between Excel and SQL Server.

## Features

- **SQL → Excel Export**: Stream any SQL view or custom query to split Excel files (auto-splits at 1M rows). Real-time progress via SignalR.
- **Excel → SQL Import**: Upload multiple `.xlsx` files into a SQL table. Auto-detects unified schema across all files. Handles mismatched columns. All data stored as NVARCHAR.
- **Connection tester**: Test your SQL connection string before running.
- **Cancel**: Cancel any running job mid-flight.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0) (for IIS)
- SQL Server with ODBC Driver 17 or 18

---

## Run Locally

```bash
cd DataBridge
dotnet run
# Open https://localhost:5001
```

---

## Build for Production

```bash
cd DataBridge
dotnet publish -c Release -o ./publish
```

---

## Deploy to IIS

1. Install the **ASP.NET Core Hosting Bundle** on the server.
2. Publish the app:
   ```
   dotnet publish -c Release -o C:\inetpub\DataBridge
   ```
3. In IIS Manager:
   - Create a new site pointing to `C:\inetpub\DataBridge`
   - Set Application Pool → **No Managed Code**
   - Ensure the app pool identity has **write access** to any output folders

4. Browse to your server URL.

---

## SignalR Dependency

SignalR is used for real-time progress updates. The client-side library
(`signalr.min.js`) must be present at:

```
wwwroot/lib/signalr/signalr.min.js
```

Install via libman or npm:

```bash
# Using libman (recommended)
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman install @microsoft/signalr@latest -p unpkg -d wwwroot/lib/signalr --files dist/browser/signalr.min.js
```

Or download directly from:
https://cdn.jsdelivr.net/npm/@microsoft/signalr/dist/browser/signalr.min.js

---

## Configuration

Edit `appsettings.json` to change defaults:

```json
{
  "DataBridge": {
    "DefaultOutputFolder": "C:\\DataBridge\\Output",
    "MaxRowsPerFile": 1000000,
    "FetchChunkSize": 50000
  }
}
```

---

## Large File Uploads

For large Excel files, IIS limits uploads to 30 MB by default.
The included `web.config` raises this to **500 MB**.

To increase further, edit `web.config`:
```xml
<requestLimits maxAllowedContentLength="1073741824" /> <!-- 1 GB -->
```

And add to `Program.cs`:
```csharp
builder.Services.Configure<FormOptions>(o => {
    o.MultipartBodyLengthLimit = 1_073_741_824;
});
```

---

## Project Structure

```
DataBridge/
├── Hubs/
│   └── ProgressHub.cs          SignalR hub
├── Models/
│   └── Models.cs               Shared models
├── Pages/
│   ├── Index.cshtml            Dashboard
│   ├── Export.cshtml           SQL → Excel page
│   ├── Import.cshtml           Excel → SQL page
│   ├── ApiController.cs        Connection test endpoint
│   └── Shared/_Layout.cshtml   Shared layout
├── Services/
│   ├── SqlExportService.cs     Export logic
│   └── ExcelImportService.cs   Import logic
├── wwwroot/
│   ├── css/site.css
│   └── lib/signalr/            (add manually — see above)
├── Program.cs
├── appsettings.json
└── web.config                  IIS configuration
```
