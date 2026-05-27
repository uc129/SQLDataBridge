using Dapper;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class GLHyperionMapRepository(TradePayableDbContext db) : IGLHyperionMapRepository
{
    public async Task<IEnumerable<GLHyperionMap>> GetAllAsync()
    {
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        return await conn.QueryAsync<GLHyperionMap>("SELECT * FROM [TP_Master_GLHyperionMap]");
    }
}
