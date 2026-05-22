using System.Text.RegularExpressions;

namespace DataBridge.Domain.Services;

public static class ColumnNameSanitizer
{
    public static string SafeColumnName(string name)
    {
        var safe = Regex.Replace(name ?? "", @"[^0-9a-zA-Z_]", "_").Trim('_').ToLower();
        if (string.IsNullOrEmpty(safe)) return "col";
        if (char.IsDigit(safe[0]))      safe = "c_" + safe;
        return safe;
    }
}
