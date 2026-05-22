using DataBridge.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace DataBridge.Infrastructure.Persistence;

internal sealed class SqlConnectionFactory(IConfiguration config) : IDbConnectionFactory
{
    private string DefaultCs => config.GetConnectionString("Default") ?? string.Empty;

    public DbConnection CreateConnection() => new SqlConnection(DefaultCs);
    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
}
