using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;

namespace DataBridge.Application.TradePayable.Processing;

public static class GLHyperionMapper
{
    private static Dictionary<string, GLAccountMapping> _map = [];
    private static bool _initialized;

    public static async Task InitializeAsync(IGLHyperionMapRepository repo)
    {
        if (_initialized) return;

        IEnumerable<GLHyperionMap> rows = await repo.GetAllAsync();
        var map = new Dictionary<string, GLAccountMapping>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            map[r.GLCode] = new GLAccountMapping(r.Hyperion_Code, r.Hyperion_Description, r.Billed_Status);

        _map         = map;
        _initialized = true;
    }

    public static GLAccountMapping ProcessGlAccount(string glAccount)
    {
        var def = new GLAccountMapping("Not Mapped", "NA", "NA");
        if (!_initialized) return def;
        return _map.TryGetValue(glAccount, out var m) ? m : def;
    }

    public record GLAccountMapping(string HyperionCode, string? HyperionCodeDescription, string? BilledStatus);
}
