using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataBridge.Infrastructure.TradePayable;

internal sealed class TradePayableDbContext(IConfiguration config)
{
    public SqlConnection OpenDefault() =>
        new(config.GetConnectionString("Default") ?? throw new InvalidOperationException("Default connection string not configured."));

    public SqlConnection OpenCrossServerPO() =>
        new(config.GetConnectionString("LtheInvoiceTracking") ?? throw new InvalidOperationException("LtheInvoiceTracking connection string not configured."));

    public SqlConnection OpenCrossServerVendor() =>
        new(config.GetConnectionString("LntPoData") ?? throw new InvalidOperationException("LntPoData connection string not configured."));
}
