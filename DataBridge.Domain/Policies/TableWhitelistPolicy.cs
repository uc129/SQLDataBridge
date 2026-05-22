namespace DataBridge.Domain.Policies;

public static class TableWhitelistPolicy
{
    public static readonly IReadOnlySet<string> ImportTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FNATool_QuickImport_1",           "FNATool_QuickImport_2",           "FNATool_QuickImport_3",
        "FNATool_VendorDetailsPipeline_1", "FNATool_VendorDetailsPipeline_2", "FNATool_VendorDetailsPipeline_3",
    };

    public static readonly IReadOnlySet<string> CleanTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FNATool_VendorDetailsPipeline_1", "FNATool_VendorDetailsPipeline_2", "FNATool_VendorDetailsPipeline_3",
    };

    public static readonly IReadOnlySet<string> ExportTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FNATool_QuickImport_1",                "FNATool_QuickImport_2",                "FNATool_QuickImport_3",
        "FNATool_VendorDetailsPipeline_1",      "FNATool_VendorDetailsPipeline_2",      "FNATool_VendorDetailsPipeline_3",
        "View_FNATool_VendorDetailsPipeline_1", "View_FNATool_VendorDetailsPipeline_2", "View_FNATool_VendorDetailsPipeline_3",
    };
}
