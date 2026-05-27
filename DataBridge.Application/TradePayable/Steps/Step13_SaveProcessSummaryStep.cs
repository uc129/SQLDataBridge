using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;
using System.Data;

namespace DataBridge.Application.TradePayable.Steps;

/// <summary>
/// Computes the final summary figures from the processed result (slot 12)
/// and stores them on ProcessState.Summary for the completion notification.
/// Frees slot 12 after summarising.
/// </summary>
public class Step13_SaveProcessSummaryStep : IProcessStep
{
    public int StepIndex => 13;

    public Task<ProcessState> ExecuteAsync(ProcessState state)
    {
        if (state.Summary is not null) return Task.FromResult(state);

        var data = state.StepData[12];
        state.Summary        = BuildSummary(data, state.RevisionNumber);
        state.ProcessEndTime = DateTime.UtcNow;
        // StepData[12] is intentionally kept alive here so the pipeline runner
        // can write a final Excel result file before the task scope ends.

        return Task.FromResult(state);
    }

    private static ProcessResultSummary BuildSummary(DataTable data, string revisionNumber)
    {
        decimal GetVal(DataRow row, string col) =>
            data.Columns.Contains(col) && !row.IsNull(col) &&
            decimal.TryParse(row[col]?.ToString(), out var v) ? v : 0m;

        decimal Sum(string col) => data.AsEnumerable().Sum(r => GetVal(r, col));

        decimal SumWhere(string col, string filterCol, string filterVal) =>
            data.AsEnumerable()
                .Where(r => r[filterCol]?.ToString() == filterVal)
                .Sum(r => GetVal(r, col));

        bool IsSNA(DataRow r) =>
            r["IsSNACompany"]?.ToString()?.Equals("True", StringComparison.OrdinalIgnoreCase) == true;

        return new ProcessResultSummary
        {
            RevisionNumber            = revisionNumber,
            Original_SAP_AmountLocal  = Sum("Amount_Local"),
            TotalAdvanceAdjustedLocal = Sum("Advance_Applied"),
            NetLiabilityAmountLocal   = Sum("Amount_Local_Adjusted"),
            MSMEResults = new MSMEResults
            {
                Hyperion_2D170100_Net_Balance = SumWhere("Net_Amount_INR", "Hyperion_Code", "2D170100"),
                Hyperion_2D170200_Net_Balance = SumWhere("Net_Amount_INR", "Hyperion_Code", "2D170200"),
                Hyperion_2D190510_Net_Balance = SumWhere("Net_Amount_INR", "Hyperion_Code", "2D190510"),
            },
            CapitalRevenueResults = new CapitalRevenueResults
            {
                Hyperion_2D190300_Net_Balance = SumWhere("Net_Amount_INR", "Hyperion_Code", "2D251000"),
            },
            GITAdvanceAdjustmentResults = new GITAdvanceAdjustmentResults
            {
                Total_SAP_Amount_Local      = Sum("Base_SAP_Amount"),
                Total_Adjusted_Amount_Local = Sum("Amount_Local_Adjusted"),
                Total_Net_Balance           = Sum("Amount_Local_Adjusted"),
            },
            SNACompanyResults = new SNACompanyResults
            {
                Original_SAP_Amount_Local = data.AsEnumerable().Where(IsSNA).Sum(r => GetVal(r, "Amount_Local")),
                Advance_Adjusted_Local    = data.AsEnumerable().Where(IsSNA).Sum(r => GetVal(r, "Advance_Applied")),
                Net_Balance_Local         = data.AsEnumerable().Where(IsSNA).Sum(r => GetVal(r, "Amount_Local_Adjusted")),
                Net_Balance_Doc_INR       = data.AsEnumerable().Where(IsSNA).Sum(r => GetVal(r, "Amount_Doc_Adjusted_INR")),
                Net_ERV                   = data.AsEnumerable().Where(IsSNA).Sum(r => GetVal(r, "Amount_Doc_Adjusted_ERV")),
            },
        };
    }
}
