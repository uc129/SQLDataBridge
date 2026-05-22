using Dapper;
using Domain.Aggregates;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class ExcelUploadRepository(DapperContext dbcontext ) : IExcelUploadRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        public async Task<Message> ReplaceRAWFAGLL03RevisionData(List<FAGLL03RAWEntity> data, string revision, DateTime quarter)
        {
            using var connection = _dbcontext.CreateConnection("default");

            // 1. Convert list to DataTable for the Table-Valued Parameter
            DataTable uploadTable = ConvertListToDataTableInOrder.FAGLL03EntityToDataTable(data);

            try
            {
                // RUN VALIDATION BEFORE CALLING DB
                ConvertListToDataTableInOrder.ValidateDataTable(uploadTable);
            }
            catch (Exception valEx)
            {
                return new Message { Success = false, Text = "Data Validation Error: " + valEx.Message };
            }

            var parameters = new DynamicParameters();
            parameters.Add("@RevisionNumber", revision);
            parameters.Add("@QuarterEndDate", quarter);
            // Note: Use the exact name of the Table Type created in SQL: [dbo].[FAGLL03_TableType]
            parameters.Add("@UploadData", uploadTable.AsTableValuedParameter("[dbo].[FAGLL03RAWType]"));

            try
            {
                // 3. Execute Stored Procedure
                await connection.ExecuteAsync(
                    "sp_TradePayables_UploadRevision",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new Message
                {
                    Success = true,
                    Text = $"Revision {revision} successfully replaced with {data.Count} records."
                };
            }
            catch (Exception ex)
            {
                // No need for manual Rollback here as the SP handles the transaction
                return new Message { Success = false, Text = "Database Error: " + ex.Message };
            }
        }


        
    }


    public class ConvertListToDataTableInOrder
    {

        public static DataTable FAGLL03EntityToDataTable(List<FAGLL03RAWEntity> data)
        {
            DataTable dt = new("FAGLL03RAW Staging Table");

            // POSITIONAL ORDER AS PER SQL [dbo].[FAGLL03RAWType]
            dt.Columns.Add("Purchasing_Document", typeof(string));   // 0
            dt.Columns.Add("Document_Header", typeof(string));      // 1
            dt.Columns.Add("Assignment", typeof(string));           // 2
            dt.Columns.Add("Invoice_Reference", typeof(string));    // 3
            dt.Columns.Add("Vendor", typeof(string));               // 4
            dt.Columns.Add("Invoice_Description", typeof(string));  // 5
            dt.Columns.Add("Vendor_Description", typeof(string));   // 6
            dt.Columns.Add("GL_Account", typeof(string));           // 7
            dt.Columns.Add("GL_Description", typeof(string));       // 8
            dt.Columns.Add("Company_Code", typeof(string));         // 9
            dt.Columns.Add("User_Name", typeof(string));            // 10
            dt.Columns.Add("Amount_Local", typeof(decimal));        // 11
            dt.Columns.Add("Document_Type", typeof(string));        // 12
            dt.Columns.Add("Document_Number", typeof(string));      // 13
            dt.Columns.Add("Industry", typeof(string));             // 14
            dt.Columns.Add("Payment_Terms", typeof(string));        // 15 - MOVED UP
            dt.Columns.Add("Profit_Center", typeof(string));        // 16
            dt.Columns.Add("Document_Date", typeof(DateTime));      // 17
            dt.Columns.Add("Posting_Date", typeof(DateTime));       // 18
            dt.Columns.Add("Payment_Date", typeof(DateTime));       // 19
            dt.Columns.Add("Document_Currency", typeof(string));    // 20
            dt.Columns.Add("Amount_Doc", typeof(decimal));          // 21
            dt.Columns.Add("Edited", typeof(string));               // 22
            dt.Columns.Add("SOURCE", typeof(string));               // 23
            dt.Columns.Add("Report_Date", typeof(DateTime));        // 24
            dt.Columns.Add("RevisionNumber", typeof(string));       // 25
            dt.Columns.Add("QuarterEndDate", typeof(DateTime));     // 26
            dt.Columns.Add("Invoice_Key", typeof(Guid));            // 27 - MOVED DOWN
            dt.Columns.Add("UploadedDate", typeof(DateTime));       // 28

            foreach (var item in data)
            {
                dt.Rows.Add(
                    ToSqlValue(item.Purchasing_Document),   // 0
                    ToSqlValue(item.Document_Header),        // 1
                    ToSqlValue(item.Assignment),             // 2
                    ToSqlValue(item.Invoice_Reference),      // 3
                    ToSqlValue(item.Vendor),                 // 4
                    ToSqlValue(item.Invoice_Description),    // 5
                    ToSqlValue(item.Vendor_Description),     // 6
                    ToSqlValue(item.GL_Account),             // 7
                    ToSqlValue(item.GL_Description),         // 8
                    ToSqlValue(item.Company_Code),           // 9
                    ToSqlValue(item.User_Name),              // 10
                    ToSqlValue(item.Amount_Local),           // 11
                    ToSqlValue(item.Document_Type),          // 12
                    ToSqlValue(item.Document_Number),        // 13
                    ToSqlValue(item.Industry),               // 14
                    ToSqlValue(item.Payment_Terms),          // 15
                    ToSqlValue(item.Profit_Center),          // 16
                    ToSqlValue(item.Document_Date),          // 17
                    ToSqlValue(item.Posting_Date),           // 18
                    ToSqlValue(item.Payment_Date),           // 19
                    ToSqlValue(item.Document_Currency),      // 20
                    ToSqlValue(item.Amount_Doc),             // 21
                    ToSqlValue(item.Edited),                 // 22
                    ToSqlValue(item.SOURCE),                 // 23
                    ToSqlValue(item.Report_Date),            // 24
                    ToSqlValue(item.RevisionNumber),         // 25
                    ToSqlValue(item.QuarterEndDate),         // 26
                    item.Invoice_Key,                        // 27
                    ToSqlValue(item.UploadedDate)            // 28
                );
            }
            return dt;
        }

        private static object ToSqlValue(object? value)
        {
            if (value == null) return DBNull.Value;

            // If it's a DateTime, ensure it is within SQL Server's min range (1753)
            if (value is DateTime dt)
            {
                if (dt < new DateTime(1753, 1, 1)) return DBNull.Value;
                return dt;
            }

            return value;
        }

        public static void ValidateDataTable(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                int rowIndex = dt.Rows.IndexOf(row);
                foreach (DataColumn col in dt.Columns)
                {
                    try
                    {
                        var value = row[col];
                        if (value == DBNull.Value) continue;

                        // Test 1: Date Range Check (The most common cause of Error 241)
                        if (col.DataType == typeof(DateTime))
                        {
                            DateTime dtValue = (DateTime)value;
                            if (dtValue < new DateTime(1753, 1, 1))
                            {
                                throw new Exception($"Date {dtValue} is earlier than SQL Minimum (1753).");
                            }
                        }

                        // Test 2: Decimal check
                        if (col.DataType == typeof(decimal))
                        {
                            decimal decValue = (decimal)value;
                            // Ensure it doesn't exceed typical SQL Decimal(18,2)
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Validation failed at Row [{rowIndex + 1}], Column [{col.ColumnName}]: {ex.Message}");
                    }
                }
            }
        }
    }
}





