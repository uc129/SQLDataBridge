namespace DataBridge.Domain.TradePayable.Contracts;

public interface ICrossServerVendorRepository
{
    /// <summary>Returns vendor code → basic vendor record from the cross-server LntPoData source.</summary>
    Task<Dictionary<string, VendorBasicRecord>> GetVendorDataAsync();
}

/// <summary>Vendor master data fetched from the cross-server LntPoData database.</summary>
public record VendorBasicRecord(
    string? Vendor_Name,
    string? Industry_Type,
    string? ZTERM);
