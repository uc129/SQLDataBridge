using Dapper;
using Domain.Aggregates;
using Infrastructure.Dapper;
using Infrastructure.Contracts;



namespace Infrastructure.Repository
{
    using System.Data;
    using Dapper; // Keep Dapper for the connection execution methods

    public class VendorRecordsRepository(DapperContext dbcontext) : IVendorRecordsRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;

        // Change return type to Task<DataTable>
        public async Task<DataTable> GetAllAsDataTableAsync()
        {
            string sql = @"SELECT [pkc_vendor_code] as PKC_Vendor_Code,
                                  [pkc_company_code] as PKC_Company_Code,
                                  [c_vendor_name] as C_Vendor_Name, 
                                  [ZTERM], 
                                  [industry_type] as Industry_Type 
                           FROM [Lnt_PO_Data].[dbo].[m_Vendor]";




            // Use the connection creation method provided by DapperContext
            using var connection = _dbcontext.CreateConnection("lnt_po_data");

            // 1. Execute the query and return a DbDataReader (raw ADO.NET results)
            // ExecuteReaderAsync is a Dapper extension method on IDbConnection.
            using var reader = await connection.ExecuteReaderAsync(sql);

            // 2. Create a new DataTable
            var dataTable = new DataTable("VendorRecords");

            // 3. Load the results from the reader into the DataTable
            dataTable.Load(reader);

            return dataTable;
        }

        public async Task<IEnumerable<VendorRecords>> GetAllAsync()
        {
            string sql = "SELECT [pkc_vendor_code],[pkc_company_code],[c_vendor_name], [ZTERM], [industry_type] FROM [Lnt_PO_Data].[dbo].[m_Vendor]";
            using var connection = _dbcontext.CreateConnection("lnt_po_data");
            var result = await connection.QueryAsync<VendorRecords>(sql);
            return result;
        }
    }
}
