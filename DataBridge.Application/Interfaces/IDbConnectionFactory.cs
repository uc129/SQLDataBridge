using System.Data.Common;

namespace DataBridge.Application.Interfaces;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
    DbConnection CreateConnection(string connectionString);
}
