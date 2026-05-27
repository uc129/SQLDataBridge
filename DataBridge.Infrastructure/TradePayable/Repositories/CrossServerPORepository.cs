using Dapper;
using DataBridge.Domain.TradePayable.Contracts;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class CrossServerPORepository(TradePayableDbContext db) : ICrossServerPORepository
{
    public async Task<Dictionary<string, string>> GetPOCreditPeriodsAsync()
    {
        const string sql = """
            SELECT [PurchasingDoc], MAX([CreditPeriod]) AS Credit_Period
            FROM [Lnt_PO_Data].[dbo].[POTemsfromSAP]
            GROUP BY [PurchasingDoc]
            """;

        await using var conn = db.OpenCrossServerPO();
        await conn.OpenAsync();

        var rows = await conn.QueryAsync<(string PurchasingDoc, string Credit_Period)>(sql);
        return rows.ToDictionary(r => r.PurchasingDoc, r => r.Credit_Period, StringComparer.OrdinalIgnoreCase);
    }
}
