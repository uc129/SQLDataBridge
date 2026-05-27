namespace DataBridge.Domain.TradePayable.Aggregates;

public class FAGLL03JoinedAndMerged : FAGLL03Populated
{
    public FAGLL03JoinedAndMerged() { }

    public FAGLL03JoinedAndMerged(FAGLL03JoinedAndMerged existing) : base(existing)
    {
        Credit_Period = existing.Credit_Period;
        Vendor_Merged = existing.Vendor_Merged;
        CP_Merged = existing.CP_Merged;
        Industry_Merged = existing.Industry_Merged;
        Vendor_Code = existing.Vendor_Code;
        Vendor_Name = existing.Vendor_Name;
        ICP_Name = existing.ICP_Name;
        Vertical = existing.Vertical;
        Entity_Type = existing.Entity_Type;
        Entity_Relation = existing.Entity_Relation;
        IsSNACompany = existing.IsSNACompany;
    }

    public string? Credit_Period { get; set; }
    public bool Vendor_Merged { get; set; } = false;
    public bool CP_Merged { get; set; } = false;
    public bool Industry_Merged { get; set; } = false;
    public string? Vendor_Code { get; set; }
    public string? Vendor_Name { get; set; }
    public string? ICP_Name { get; set; }
    public string? Vertical { get; set; }
    public string? Entity_Type { get; set; }
    public string? Entity_Relation { get; set; }
    public bool IsSNACompany { get; set; } = false;
}
