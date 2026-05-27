using Dapper;
using DataBridge.Domain.TradePayable.Aggregates;
using DataBridge.Domain.TradePayable.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class FAGLL03StagingRepository(TradePayableDbContext db) : IFAGLL03StagingRepository
{
    public async Task BulkInsertAsync(IEnumerable<FAGLL03RAWEntity> rows, Guid runId)
    {
        var table = BuildDataTable(rows, runId);

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = "TP_FAGLL03_Raw",
            BatchSize            = 500,
            BulkCopyTimeout      = 0,
        };

        foreach (DataColumn col in table.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulk.WriteToServerAsync(table);
    }

    public async Task<IEnumerable<FAGLL03RAWEntity>> GetByRunIdAsync(Guid runId)
    {
        const string sql = "SELECT * FROM TP_FAGLL03_Raw WHERE RunId = @runId";
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        return await conn.QueryAsync<FAGLL03RAWEntity>(sql, new { runId });
    }

    private static DataTable BuildDataTable(IEnumerable<FAGLL03RAWEntity> rows, Guid runId)
    {
        var dt = new DataTable();
        dt.Columns.Add("RunId",               typeof(string));
        dt.Columns.Add("Invoice_Key",         typeof(string));
        dt.Columns.Add("Document_Number",     typeof(string));
        dt.Columns.Add("Purchasing_Document", typeof(string));
        dt.Columns.Add("Invoice_Reference",   typeof(string));
        dt.Columns.Add("Document_Header",     typeof(string));
        dt.Columns.Add("Document_Type",       typeof(string));
        dt.Columns.Add("Company_Code",        typeof(string));
        dt.Columns.Add("Assignment",          typeof(string));
        dt.Columns.Add("Vendor",              typeof(string));
        dt.Columns.Add("Vendor_Description",  typeof(string));
        dt.Columns.Add("Invoice_Description", typeof(string));
        dt.Columns.Add("Industry",            typeof(string));
        dt.Columns.Add("Amount_Local",        typeof(decimal));
        dt.Columns.Add("GL_Account",          typeof(string));
        dt.Columns.Add("GL_Description",      typeof(string));
        dt.Columns.Add("Profit_Center",       typeof(string));
        dt.Columns.Add("Payment_Terms",       typeof(string));
        dt.Columns.Add("Document_Currency",   typeof(string));
        dt.Columns.Add("Amount_Doc",          typeof(decimal));
        dt.Columns.Add("Document_Date",       typeof(DateTime));
        dt.Columns.Add("Posting_Date",        typeof(DateTime));
        dt.Columns.Add("Payment_Date",        typeof(DateTime));
        dt.Columns.Add("User_Name",           typeof(string));
        dt.Columns.Add("SOURCE",              typeof(string));
        dt.Columns.Add("Edited",              typeof(string));
        dt.Columns.Add("RevisionNumber",      typeof(string));
        dt.Columns.Add("Report_Date",         typeof(DateTime));
        dt.Columns.Add("QuarterEndDate",      typeof(DateTime));
        dt.Columns.Add("UploadedDate",        typeof(DateTime));

        foreach (var r in rows)
        {
            dt.Rows.Add(
                runId.ToString(),
                (object?)r.Invoice_Key ?? DBNull.Value,
                (object?)r.Document_Number     ?? DBNull.Value,
                (object?)r.Purchasing_Document ?? DBNull.Value,
                (object?)r.Invoice_Reference   ?? DBNull.Value,
                (object?)r.Document_Header     ?? DBNull.Value,
                (object?)r.Document_Type       ?? DBNull.Value,
                (object?)r.Company_Code        ?? DBNull.Value,
                (object?)r.Assignment          ?? DBNull.Value,
                (object?)r.Vendor              ?? DBNull.Value,
                (object?)r.Vendor_Description  ?? DBNull.Value,
                (object?)r.Invoice_Description ?? DBNull.Value,
                (object?)r.Industry            ?? DBNull.Value,
                (object?)r.Amount_Local        ?? DBNull.Value,
                (object?)r.GL_Account          ?? DBNull.Value,
                (object?)r.GL_Description      ?? DBNull.Value,
                (object?)r.Profit_Center       ?? DBNull.Value,
                (object?)r.Payment_Terms       ?? DBNull.Value,
                (object?)r.Document_Currency   ?? DBNull.Value,
                (object?)r.Amount_Doc          ?? DBNull.Value,
                (object?)r.Document_Date       ?? DBNull.Value,
                (object?)r.Posting_Date        ?? DBNull.Value,
                (object?)r.Payment_Date        ?? DBNull.Value,
                (object?)r.User_Name           ?? DBNull.Value,
                (object?)r.SOURCE              ?? DBNull.Value,
                (object?)r.Edited              ?? DBNull.Value,
                (object?)r.RevisionNumber      ?? DBNull.Value,
                (object?)r.Report_Date         ?? DBNull.Value,
                (object?)r.QuarterEndDate      ?? DBNull.Value,
                (object?)r.UploadedDate        ?? DBNull.Value
            );
        }

        return dt;
    }
}
