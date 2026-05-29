using DataBridge.Domain.TradePayable.Models;
using System.Data;
using System.Text.Json;

namespace DataBridge.Application.TradePayable.Processing;

public static class StepStatsComputer
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static string SetUploadStats(string? existingJson, int parsedRows, int loadedRows)
    {
        var all = Deserialize(existingJson);
        all["upload"] = new Dictionary<string, string>
        {
            ["Parsed rows"]  = parsedRows.ToString("N0"),
            ["Loaded rows"]  = loadedRows.ToString("N0"),
            ["Filtered out"] = (parsedRows - loadedRows).ToString("N0"),
        };
        return Serialize(all);
    }

    public static string MergeStepStats(string? existingJson, int stepIndex, ProcessState state,
        TimeSpan? elapsed = null)
    {
        var all   = Deserialize(existingJson);
        var stats = ComputeStepStats(stepIndex, state);
        if (elapsed.HasValue)
            stats["Duration"] = FormatDuration(elapsed.Value);
        if (stats.Count > 0)
            all[stepIndex.ToString()] = stats;
        return Serialize(all);
    }

    public static string SetPipelineRuntime(string? existingJson, TimeSpan total)
    {
        var all = Deserialize(existingJson);
        all["pipeline"] = new Dictionary<string, string> { ["Total runtime"] = FormatDuration(total) };
        return Serialize(all);
    }

    // ── Per-step computation ─────────────────────────────────────────────────

    private static Dictionary<string, string> ComputeStepStats(int stepIndex, ProcessState state) =>
        stepIndex switch
        {
            1  => RowsOnly(state, 1),
            2  => Step2Stats(state),
            3  => Step3Stats(state),
            6  => RowsOnly(state, 6),
            7  => Step7Stats(state),
            10 => RowsOnly(state, 10),
            11 => RowsOnly(state, 11),
            12 => RowsOnly(state, 12),
            13 => Step13Stats(state),
            _  => new(),
        };

    private static Dictionary<string, string> RowsOnly(ProcessState state, int slot)
    {
        if (!state.StepData.TryGetValue(slot, out var dt)) return new();
        return new() { ["Rows"] = dt.Rows.Count.ToString("N0") };
    }

    private static Dictionary<string, string> Step2Stats(ProcessState state)
    {
        if (!state.StepData.TryGetValue(2, out var dt)) return new();

        var rows  = dt.AsEnumerable().ToList();
        var total = rows.Count;

        var result = new Dictionary<string, string> { ["Rows"] = total.ToString("N0") };

        if (dt.Columns.Contains("Vendor"))
            result["Null vendor"] = rows.Count(r => IsNullOrEmpty(r, "Vendor")).ToString("N0");

        if (dt.Columns.Contains("Vendor_Description"))
            result["Null vendor desc"] = rows.Count(r => IsNullOrEmpty(r, "Vendor_Description")).ToString("N0");

        if (dt.Columns.Contains("IsSNACompany"))
            result["SNA companies"] = rows
                .Count(r => r["IsSNACompany"]?.ToString()?.Equals("True", StringComparison.OrdinalIgnoreCase) == true)
                .ToString("N0");

        return result;
    }

    private static Dictionary<string, string> Step3Stats(ProcessState state)
    {
        if (!state.StepData.TryGetValue(3, out var gitDt)) return new();
        var result = new Dictionary<string, string> { ["GIT rows"] = gitDt.Rows.Count.ToString("N0") };
        if (state.StepData.TryGetValue(5, out var origDt))
            result["After advance"] = origDt.Rows.Count.ToString("N0");
        return result;
    }

    private static Dictionary<string, string> Step7Stats(ProcessState state)
    {
        if (!state.StepData.TryGetValue(7, out var gitDt)) return new();
        return new() { ["GIT doc rows"] = gitDt.Rows.Count.ToString("N0") };
    }

    private static Dictionary<string, string> Step13Stats(ProcessState state)
    {
        if (state.Summary is { } s)
        {
            return new()
            {
                ["Original SAP (INR)"]      = s.Original_SAP_AmountLocal.ToString("N0"),
                ["Advance adjusted (INR)"]  = s.TotalAdvanceAdjustedLocal.ToString("N0"),
                ["Net liability (INR)"]     = s.NetLiabilityAmountLocal.ToString("N0"),
                ["SNA net balance (INR)"]   = s.SNACompanyResults.Net_Balance_Local.ToString("N0"),
            };
        }

        // Fallback: compute directly from StepData[12] if Summary is not populated yet.
        if (!state.StepData.TryGetValue(12, out var dt)) return new();
        var result = new Dictionary<string, string> { ["Rows"] = dt.Rows.Count.ToString("N0") };
        if (dt.Columns.Contains("Amount_Local_Adjusted"))
            result["Net liability (INR)"] = dt.AsEnumerable()
                .Sum(r => ParseDecimal(r["Amount_Local_Adjusted"]))
                .ToString("N0");
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsNullOrEmpty(DataRow row, string col)
    {
        var v = row[col];
        return v == null || v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString());
    }

    private static decimal ParseDecimal(object? val) =>
        val is decimal d ? d :
        decimal.TryParse(val?.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0m;

    private static Dictionary<string, Dictionary<string, string>> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, JsonOpts) ?? new(); }
        catch { return new(); }
    }

    private static string FormatDuration(TimeSpan t) =>
        t.TotalMinutes >= 1 ? $"{t.TotalMinutes:F1}m" : $"{t.TotalSeconds:F1}s";

    private static string Serialize(Dictionary<string, Dictionary<string, string>> dict) =>
        JsonSerializer.Serialize(dict);
}
