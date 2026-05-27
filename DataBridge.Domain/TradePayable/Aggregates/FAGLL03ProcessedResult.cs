namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03ProcessedResult : FAGLL03NetCPFixed
{
    public FAGLL03ProcessedResult() { }

    public FAGLL03ProcessedResult(FAGLL03ProcessedResult existing) : base(existing)
    {
        Hyperion_Code = existing.Hyperion_Code;
        Hyp_Code_Description = existing.Hyp_Code_Description;
        Base_Hyperion_Debit = existing.Base_Hyperion_Debit;
        Destination_Hyperion_Credit = existing.Destination_Hyperion_Credit;
        Transacton_Type = existing.Transacton_Type;
        Ageing = existing.Ageing;
        Ageing_Years = existing.Ageing_Years;
        Ageing_Group = existing.Ageing_Group;
        Due_Status = existing.Due_Status;
        Billed_Status = existing.Billed_Status;
        MSME_Ageing = existing.MSME_Ageing;
        MSME_Type = existing.MSME_Type;
        Calculated_Due_Date = existing.Calculated_Due_Date;
        Amount_Doc_Adjusted_INR = existing.Amount_Doc_Adjusted_INR;
        Amount_Doc_Adjusted_ERV = existing.Amount_Doc_Adjusted_ERV;
        Exchange_Rate = existing.Exchange_Rate;
        ICP_Hyperion = existing.ICP_Hyperion;
        Net_Amount_INR = existing.Net_Amount_INR;
        Net_Amount_Doc = existing.Net_Amount_Doc;
    }

    // Ageing
    public int Ageing { get; set; }
    public decimal Ageing_Years { get; set; }
    public string? Ageing_Group { get; set; }
    public string? Due_Status { get; set; }
    public string? Billed_Status { get; set; }
    public string? MSME_Ageing { get; set; }
    public string? MSME_Type { get; set; }
    public DateTime? Calculated_Due_Date { get; set; }

    // Hyperion codes
    public decimal Base_Hyperion_Debit { get; set; }
    public decimal Destination_Hyperion_Credit { get; set; }
    public string? Hyperion_Code { get; set; }
    public string? Hyp_Code_Description { get; set; }
    public string? ICP_Hyperion { get; set; }
    public string? Transacton_Type { get; set; }

    // ERV calculation
    public decimal Amount_Doc_Adjusted_INR { get; set; }
    public decimal Amount_Doc_Adjusted_ERV { get; set; }
    public decimal Exchange_Rate { get; set; }

    // Net balance
    public decimal Net_Amount_INR { get; set; }
    public decimal Net_Amount_Doc { get; set; }
}
