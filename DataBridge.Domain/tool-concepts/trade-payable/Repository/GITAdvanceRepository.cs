using Dapper;
using Domain.Aggregates;
using Infrastructure.Contracts;
using Infrastructure.Dapper;




namespace Infrastructure.Repository
{
    public class GITAdvanceRepository(DapperContext dbContext) : IGITAdvanceRepository
    {
        private readonly DapperContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        public async Task<IEnumerable<FAGLL03ProcessedGITLocal>> GetByProcessIdAsync(Guid processId)
        {
            const string sql = @"
            SELECT 
                Grouped_Invoice_Key, Purchasing_Document, Vendor, Company_Code, GL_Account, Profit_Center, 
                Amount_Local, Vendor_Description, GL_Description, Industry, Credit_Period, Report_Date, 
                ICP_Name, IsSNACompany, [14005] AS _14005, Grouped_Key_14005, [14006] AS _14006, 
                Grouped_Key_14006, [14007] AS _14007, Grouped_Key_14007, [14012] AS _14012, 
                Grouped_Key_14012, [14021] AS _14021, Grouped_Key_14021, [14701] AS _14701, 
                Grouped_Key_14701, [14705] AS _14705, Grouped_Key_14705, Join_Type, Adjusted_GL, 
                Adjusted_Amount, Total_Adjustment, Balance_Local, Vertical, Composite_Join_Key, 
                ProcessId, StepIndex
            FROM 
                [TradeMSEDDetails_UAT].[dbo].[Step_03_TradePayables_ProcessedGIT_Data]
            WHERE 
                ProcessId = @ProcessId";

            using var connection = _dbContext.CreateConnection("default");

            var parameters = new { ProcessId = processId };

            var results = await connection.QueryAsync<FAGLL03ProcessedGITLocal>(sql, parameters);
            return results;
        }
    }
}