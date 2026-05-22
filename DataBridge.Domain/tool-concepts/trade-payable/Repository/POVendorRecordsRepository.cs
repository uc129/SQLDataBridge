using Dapper;
using Domain.Aggregates;
using Infrastructure.Dapper;
using Infrastructure.Contracts;



namespace Infrastructure.Repository
{
    using System.Data;
    using Dapper; // Keep Dapper for the connection execution methods

    public class POVendorRecordsRepository(DapperContext dbcontext) : IPOVendorRecordsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        // Change return type to Task<DataTable>
        public async Task<DataTable> GetAllAsDataTableAsync()
        {
            string sql = "SELECT [ebeln] as EBELN, MAX([lifnr]) as LIFNR, MAX([name1]) as Vendor_Name FROM [Lnt_PO_Data].[dbo].[podata] GROUP BY [ebeln]";

            // Use the connection creation method provided by DapperContext
            using var connection = _dbcontext.CreateConnection("lnt_po_data");

            // 1. Execute the query and return a DbDataReader (raw ADO.NET results)
            using var reader = await connection.ExecuteReaderAsync(sql);

            // 2. Create a new DataTable
            var dataTable = new DataTable("POVendorData");

            // 3. Load the results from the reader into the DataTable
            dataTable.Load(reader);

            return dataTable;
        }

        public async Task<IEnumerable<POVendorRecords>> GetAllAsync()
        {
            string sql = "SELECT [ebeln], MAX([lifnr]) as lifnr, MAX([name1]) as vendor_name FROM [Lnt_PO_Data].[dbo].[podata] GROUP BY [ebeln]";
            using var connection = _dbcontext.CreateConnection("lnt_po_data");
            var result = await connection.QueryAsync<POVendorRecords>(sql);
            return result;
        }
    }
}
