namespace DataBridge.Domain.Models;

public class TableMetrics
{
    public string TableName      { get; set; } = string.Empty;
    public bool   Exists         { get; set; }
    public long   RowCount       { get; set; }
    public long?  VendorNotFound { get; set; }
    public long?  PoNotFound     { get; set; }
}

public class DashboardMetrics
{
    public List<TableMetrics> Tables      { get; set; } = new();
    public long               ViewRowCount { get; set; }
    public string             ViewName    { get; set; } = string.Empty;
}

public class ProgressMessage
{
    public string  JobId       { get; set; } = string.Empty;
    public string  Stage       { get; set; } = string.Empty;
    public string  Message     { get; set; } = string.Empty;
    public int     Percent     { get; set; }
    public long    RowsDone    { get; set; }
    public long    RowsTotal   { get; set; }
    public bool    IsError     { get; set; }
    public bool    IsComplete  { get; set; }
    public List<string>? OutputFiles { get; set; }
}

public class JobResult
{
    public bool          Success      { get; set; }
    public string        Message      { get; set; } = string.Empty;
    public int           FilesCreated { get; set; }
    public long          RowsTotal    { get; set; }
    public string        ElapsedTime  { get; set; } = string.Empty;
    public List<string>  OutputFiles  { get; set; } = new();
}
