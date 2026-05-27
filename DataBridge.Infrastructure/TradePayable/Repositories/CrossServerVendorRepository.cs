using Dapper;
using DataBridge.Domain.TradePayable.Contracts;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class CrossServerVendorRepository(TradePayableDbContext db) : ICrossServerVendorRepository
{
    public async Task<Dictionary<string, VendorBasicRecord>> GetVendorDataAsync()
    {
        const string sql = """
            SELECT [pkc_vendor_code]  AS Vendor_Code,
                   [c_vendor_name]    AS Vendor_Name,
                   [industry_type]    AS Industry_Type,
                   [ZTERM]            AS ZTERM
            FROM [Lnt_PO_Data].[dbo].[m_Vendor]
            """;

        await using var conn = db.OpenCrossServerVendor();
        await conn.OpenAsync();

        var rows = await conn.QueryAsync<(string Vendor_Code, string? Vendor_Name, string? Industry_Type, string? ZTERM)>(sql);

        return rows
            .GroupBy(r => r.Vendor_Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var r = g.First();
                    return new VendorBasicRecord(r.Vendor_Name, r.Industry_Type, r.ZTERM);
                },
                StringComparer.OrdinalIgnoreCase);
    }
}
