using Dapper;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class MasterTableRepository<T>(
    TradePayableDbContext db,
    IOptions<TradePayableSettings> settings,
    string tableKey) : IMasterTableRepository<T> where T : class
{
    private string TableName => settings.Value.GetMasterTable(tableKey);

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        return await conn.QueryAsync<T>($"SELECT * FROM [{TableName}]");
    }

    public async Task UpsertAsync(T record)
    {
        var props  = GetWritableProperties(record);
        var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        int id     = idProp is null ? 0 : (int)(idProp.GetValue(record) ?? 0);

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        if (id <= 0)
        {
            var cols   = string.Join(", ", props.Select(p => $"[{p.Name}]"));
            var values = string.Join(", ", props.Select(p => $"@{p.Name}"));
            await conn.ExecuteAsync($"INSERT INTO [{TableName}] ({cols}) VALUES ({values})", record);
        }
        else
        {
            var sets = string.Join(", ", props.Select(p => $"[{p.Name}] = @{p.Name}"));
            await conn.ExecuteAsync($"UPDATE [{TableName}] SET {sets} WHERE [Id] = @Id", record);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync($"DELETE FROM [{TableName}] WHERE [Id] = @id", new { id });
    }

    private static IReadOnlyList<PropertyInfo> GetWritableProperties(T record) =>
        typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.Name != "Id")
            .ToList();
}
