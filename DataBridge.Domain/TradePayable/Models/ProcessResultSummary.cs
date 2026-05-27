namespace DataBridge.Domain.TradePayable.Models;

public class ProcessResultSummary
{
    public required string RevisionNumber { get; set; }
    public required decimal Original_SAP_AmountLocal { get; set; }
    public required decimal TotalAdvanceAdjustedLocal { get; set; }
    public required decimal NetLiabilityAmountLocal { get; set; }
    public required MSMEResults MSMEResults { get; set; } = new();
    public required CapitalRevenueResults CapitalRevenueResults { get; set; } = new();
    public required GITAdvanceAdjustmentResults GITAdvanceAdjustmentResults { get; set; } = new();
    public required SNACompanyResults SNACompanyResults { get; set; } = new();
}

public class MSMEResults
{
    public decimal Hyperion_2D170100_Net_Balance { get; set; }
    public decimal Hyperion_2D170200_Net_Balance { get; set; }
    public decimal Hyperion_2D190510_Net_Balance { get; set; }
}

public class CapitalRevenueResults
{
    public decimal Hyperion_2D190300_Net_Balance { get; set; }
}

public class GITAdvanceAdjustmentResults
{
    public decimal Total_SAP_Amount_Local { get; set; }
    public decimal Total_Adjusted_Amount_Local { get; set; }
    public decimal Total_Net_Balance { get; set; }
}

public class SNACompanyResults
{
    public decimal Original_SAP_Amount_Local { get; set; }
    public decimal Advance_Adjusted_Local { get; set; }
    public decimal Net_Balance_Local { get; set; }
    public decimal Net_Balance_Doc_INR { get; set; }
    public decimal Net_ERV { get; set; }
}
