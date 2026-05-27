namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03NetLiability : FAGLL03JoinedAndMerged
{
    public FAGLL03NetLiability() { }

    public FAGLL03NetLiability(FAGLL03NetLiability existing) : base(existing)
    {
        Grouped_Invoice_Key_Original = existing.Grouped_Invoice_Key_Original;
        Composite_Join_Key = existing.Composite_Join_Key;
        Advance_22006 = existing.Advance_22006;
        Advance_22071 = existing.Advance_22071;
        Advance_22072 = existing.Advance_22072;
        Advance_22113 = existing.Advance_22113;
        Advance_23021 = existing.Advance_23021;
        Advance_23051 = existing.Advance_23051;
        Advance_23054 = existing.Advance_23054;
        Advance_23057 = existing.Advance_23057;
        Advance_23059 = existing.Advance_23059;
        Advance_23141 = existing.Advance_23141;
        Advance_22075 = existing.Advance_22075;
        Advance_TotalAdvanceAmount = existing.Advance_TotalAdvanceAmount;
        Advance_Applied = existing.Advance_Applied;
        Amount_Local_Adjusted = existing.Amount_Local_Adjusted;
        Adjustment_Type = existing.Adjustment_Type;
        Total_Remaining_Advance = existing.Total_Remaining_Advance;
        Base_Hyperion_Code = existing.Base_Hyperion_Code;
        Base_Hyperion_Description = existing.Base_Hyperion_Description;
        Base_SAP_Amount = existing.Base_SAP_Amount;
        Advance_22006_Doc = existing.Advance_22006_Doc;
        Advance_22071_Doc = existing.Advance_22071_Doc;
        Advance_22072_Doc = existing.Advance_22072_Doc;
        Advance_22113_Doc = existing.Advance_22113_Doc;
        Advance_23021_Doc = existing.Advance_23021_Doc;
        Advance_23051_Doc = existing.Advance_23051_Doc;
        Advance_23054_Doc = existing.Advance_23054_Doc;
        Advance_23057_Doc = existing.Advance_23057_Doc;
        Advance_23059_Doc = existing.Advance_23059_Doc;
        Advance_23141_Doc = existing.Advance_23141_Doc;
        Advance_TotalAdvanceAmount_Doc = existing.Advance_TotalAdvanceAmount_Doc;
        Advance_Applied_Doc = existing.Advance_Applied_Doc;
        Amount_Doc_Adjusted = existing.Amount_Doc_Adjusted;
        Total_Remaining_Advance_Doc = existing.Total_Remaining_Advance_Doc;
    }

    public Guid Grouped_Invoice_Key_Original { get; set; } = Guid.Empty;
    public string? Composite_Join_Key { get; set; }

    // Local currency advance fields
    public decimal Advance_22006 { get; set; }
    public decimal Advance_22071 { get; set; }
    public decimal Advance_22072 { get; set; }
    public decimal Advance_22113 { get; set; }
    public decimal Advance_23021 { get; set; }
    public decimal Advance_23051 { get; set; }
    public decimal Advance_23054 { get; set; }
    public decimal Advance_23057 { get; set; }
    public decimal Advance_23059 { get; set; }
    public decimal Advance_23141 { get; set; }
    public decimal Advance_22075 { get; set; }
    public decimal Advance_TotalAdvanceAmount { get; set; }
    public decimal Advance_Applied { get; set; }
    public decimal Amount_Local_Adjusted { get; set; }
    public string? Adjustment_Type { get; set; }
    public decimal Total_Remaining_Advance { get; set; }
    public string? Base_Hyperion_Code { get; set; }
    public string? Base_Hyperion_Description { get; set; }
    public decimal Base_SAP_Amount { get; set; }

    // Document currency advance fields
    public Guid Grouped_Invoice_Key_Original_Doc { get; set; } = Guid.Empty;
    public decimal Advance_22006_Doc { get; set; }
    public decimal Advance_22071_Doc { get; set; }
    public decimal Advance_22072_Doc { get; set; }
    public decimal Advance_22113_Doc { get; set; }
    public decimal Advance_23021_Doc { get; set; }
    public decimal Advance_23051_Doc { get; set; }
    public decimal Advance_23054_Doc { get; set; }
    public decimal Advance_23057_Doc { get; set; }
    public decimal Advance_23059_Doc { get; set; }
    public decimal Advance_23141_Doc { get; set; }
    public decimal Advance_TotalAdvanceAmount_Doc { get; set; }
    public decimal Advance_Applied_Doc { get; set; }
    public decimal Amount_Doc_Adjusted { get; set; }
    public decimal Total_Remaining_Advance_Doc { get; set; }
}
