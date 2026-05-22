using Dapper;
using Domain.Aggregates;
using Infrastructure.Dapper;
using Infrastructure.Contracts;



namespace Infrastructure.Repository
{
    using System.Data;
    using Dapper; // Keep Dapper for QueryAsync/ExecuteReader, etc.

    public class POCreditperiodRepository(DapperContext dbcontext) : IPOCreditPeriodRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        public async Task<DataTable> GetAllAsDataTableAsync() // Change return type to DataTable
        {
            string sql = "SELECT [PurchasingDoc], MAX([CreditPeriod]) AS Credit_Period FROM [Lnt_PO_Data].[dbo].[POTemsfromSAP] GROUP BY [PurchasingDoc]";

            using var connection = _dbcontext.CreateConnection("lthe_invoice_tracking");

            // 1. Execute the query using a DbDataReader
            using var reader = await connection.ExecuteReaderAsync(sql);

            // 2. Create a new DataTable
            var dataTable = new DataTable();

            // 3. Load the results from the reader into the DataTable
            dataTable.Load(reader);

            return dataTable;
        }


        public async Task<IEnumerable<POCreditPeriod>> GetAllAsync()
        {
            string sql = "SELECT [PurchasingDoc], MAX([CreditPeriod]) AS Credit_Period FROM [Lnt_PO_Data].[dbo].[POTemsfromSAP] GROUP BY [PurchasingDoc]";
            using var connection = _dbcontext.CreateConnection("lthe_invoice_tracking");
            var result = await connection.QueryAsync<POCreditPeriod>(sql);
            return result;
        }
    }
}
