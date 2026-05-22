using System.Text.RegularExpressions;

namespace DataBridge.Domain.Services;

public static class VendorExtractionRules
{
    public static readonly Regex VendorRegex = new(
        @"\bLT\d{4}\b|VC\s?\d{7}|\bVC\s?-?\s?(?:\d{5}|\d{7})\b|\b\d{7}\b|\b\d{5}\b",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    public static readonly Regex VendorCodeRegex = new(
        @"\bLT\d{4}\b|\d{7}|\d{5}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Regex BuildPoRegex(int[] digits)
    {
        if (digits.Length == 0) digits = [7, 8, 3];
        var alts = string.Join("|", digits.Distinct().Select(d => $@"\b{d}\d{{9}}\b"));
        return new Regex(alts, RegexOptions.Compiled);
    }
}
