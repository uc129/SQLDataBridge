namespace DataBridge.Domain.TradePayable.Configuration;

public class TradePayableSettings
{
    public string DatabaseName { get; set; } = "TradeMSEDDetails";
    public Dictionary<string, string> StepTables { get; set; } = [];
    public Dictionary<string, string> BackupTables { get; set; } = [];
    public Dictionary<string, string> MasterTables { get; set; } = [];
    public Dictionary<string, string> Views { get; set; } = [];

    public string GetStepTable(string key) =>
        StepTables.TryGetValue(key, out var name) ? name : throw new KeyNotFoundException($"Step table key '{key}' not configured.");

    public string GetBackupTable(string key) =>
        BackupTables.TryGetValue(key, out var name) ? name : throw new KeyNotFoundException($"Backup table key '{key}' not configured.");

    public string GetMasterTable(string key) =>
        MasterTables.TryGetValue(key, out var name) ? name : throw new KeyNotFoundException($"Master table key '{key}' not configured.");

    public string GetView(string key) =>
        Views.TryGetValue(key, out var name) ? name : throw new KeyNotFoundException($"View key '{key}' not configured.");
}
