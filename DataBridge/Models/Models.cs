namespace DataBridge.Models;

public class ExportRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueryOrView      { get; set; } = string.Empty;  // view name or full SQL
    public bool   IsRawQuery       { get; set; } = false;
    public string OutputFolder     { get; set; } = string.Empty;
    public string FilePrefix       { get; set; } = "export";
    public string SheetName        { get; set; } = "Data";
    public int    MaxRowsPerFile   { get; set; } = 1_000_000;
}

public class ImportRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName        { get; set; } = string.Empty;
    public string SchemaName       { get; set; } = "dbo";
    public bool   ReplaceTable     { get; set; } = true;
    public List<IFormFile> Files   { get; set; } = new();
}

public class ProgressMessage
{
    public string JobId      { get; set; } = string.Empty;
    public string Stage      { get; set; } = string.Empty;   // Counting, Fetching, Writing, Done, Error
    public string Message    { get; set; } = string.Empty;
    public int    Percent    { get; set; } = 0;
    public long   RowsDone   { get; set; } = 0;
    public long   RowsTotal  { get; set; } = 0;
    public bool   IsError    { get; set; } = false;
    public bool   IsComplete { get; set; } = false;
}

public class JobResult
{
    public bool   Success      { get; set; }
    public string Message      { get; set; } = string.Empty;
    public int    FilesCreated { get; set; }
    public long   RowsTotal    { get; set; }
    public string ElapsedTime  { get; set; } = string.Empty;
    public List<string> OutputFiles { get; set; } = new();
}
