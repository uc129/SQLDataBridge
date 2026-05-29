using DataBridge.Application.TradePayable.Services;
using DataBridge.Domain.TradePayable.Aggregates;
using DataBridge.Domain.TradePayable.MasterTables;
using System.Data;

namespace DataBridge.Application.TradePayable.Processing;

public class HelperFunctions(StaticMasterTableService masterTableService)
{
    public IEnumerable<AdvanceGLs> AdvanceGLs { get; private set; } = [];
    public IEnumerable<LiabilityGLs> LiabilityGLs { get; private set; } = [];
    public IEnumerable<NotDueGLs> NotDueGL { get; private set; } = [];
    public IEnumerable<MSMECompanyCodes> MSMECompanyCodes { get; private set; } = [];
    public IEnumerable<CapitalCreditorGLs> CapitalGLs { get; private set; } = [];
    public IEnumerable<UnclaimedGLs> UnclaimedGLs { get; private set; } = [];
    public IEnumerable<InsuranceGLs> InsurerGLs { get; private set; } = [];
    public IEnumerable<NonMSMEGLs> NonMSMEGLs { get; private set; } = [];
    public IEnumerable<AgeingGroup> AgeingGroup { get; private set; } = [];
    public IEnumerable<ICPVendorMap> ICPVendorMaps { get; private set; } = [];
    public IEnumerable<ICPHyperionMap> ICPHyperionMaps { get; private set; } = [];
    public IEnumerable<ForexMonthEndMap> ForexMonthEndMaps { get; private set; } = [];

    private bool _initialized;

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await Task.WhenAll(
            masterTableService.GetAdvanceGLsAsync().ContinueWith(t => AdvanceGLs = t.Result),
            masterTableService.GetLiabilityGLsAsync().ContinueWith(t => LiabilityGLs = t.Result),
            masterTableService.GetNotDueGLsAsync().ContinueWith(t => NotDueGL = t.Result),
            masterTableService.GetMSMECodesAsync().ContinueWith(t => MSMECompanyCodes = t.Result),
            masterTableService.GetCapitalGLsAsync().ContinueWith(t => CapitalGLs = t.Result),
            masterTableService.GetUnclaimedGLsAsync().ContinueWith(t => UnclaimedGLs = t.Result),
            masterTableService.GetInsuranceGLsAsync().ContinueWith(t => InsurerGLs = t.Result),
            masterTableService.GetNonMSMEGLsAsync().ContinueWith(t => NonMSMEGLs = t.Result),
            masterTableService.GetAgeingGroupsAsync().ContinueWith(t => AgeingGroup = t.Result),
            masterTableService.GetICPVendorMapAsync().ContinueWith(t => ICPVendorMaps = t.Result),
            masterTableService.GetICPHyperionMapAsync().ContinueWith(t => ICPHyperionMaps = t.Result),
            masterTableService.GetForexMonthEndMapAsync().ContinueWith(t => ForexMonthEndMaps = t.Result)
        );

        _initialized = true;
    }

    // ── Type-safe helpers ─────────────────────────────────────────────────────

    public bool IsAdvanceGL(FAGLL03ProcessedResult entity)
    {
        var gl = entity.GL_Account ?? string.Empty;
        return AdvanceGLs.Any(a => a.GL_Code.Equals(gl, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsMSMED(FAGLL03ProcessedResult entity)
    {
        if (IsAdvanceGL(entity)) return false;
        bool industryMatch = entity.Industry is "1" or "2" or "3";
        return industryMatch
            && MSMECompanyCodes.Any(c => c.Company_Code == entity.Company_Code)
            && !UnclaimedGLs.Any(u => u.Gl_Code == entity.GL_Account)
            && !NonMSMEGLs.Any(n => n.Gl_Code == entity.GL_Account);
    }

    public bool IsCapitalRevenue(FAGLL03ProcessedResult entity)
    {
        if (string.IsNullOrEmpty(entity.GL_Account) || string.IsNullOrEmpty(entity.Purchasing_Document)) return false;
        if (IsAdvanceGL(entity) || IsMSMED(entity)) return false;
        return CapitalGLs.Any(c => c.Gl_Code == entity.GL_Account)
            && entity.Purchasing_Document.StartsWith("71");
    }

    // ── DataRow helpers ───────────────────────────────────────────────────────

    public bool ISAdvanceGL(DataRow row)
    {
        var gl = row["GL_Account"]?.ToString() ?? string.Empty;
        return AdvanceGLs.Any(a => a.GL_Code.Equals(gl, StringComparison.OrdinalIgnoreCase));
    }

    public bool ISMSMED(DataRow row)
    {
        if (ISAdvanceGL(row)) return false;
        var cc = row["Company_Code"]?.ToString();
        var gl = row["GL_Account"]?.ToString();
        var ind = row["Industry"]?.ToString();
        bool industryMatch = ind is "1" or "2" or "3";
        return industryMatch
            && MSMECompanyCodes.Any(c => c.Company_Code == cc)
            && !UnclaimedGLs.Any(u => u.Gl_Code == gl)
            && !NonMSMEGLs.Any(n => n.Gl_Code == gl);
    }

    public bool ISCapitalRevenue(DataRow row)
    {
        var gl = row["GL_Account"]?.ToString();
        var po = row["Purchasing_Document"]?.ToString();
        if (string.IsNullOrEmpty(gl) || string.IsNullOrEmpty(po)) return false;
        if (ISAdvanceGL(row) || ISMSMED(row)) return false;
        return CapitalGLs.Any(c => c.Gl_Code == gl) && po.StartsWith("71");
    }

    // ── Static utilities ──────────────────────────────────────────────────────

    public static IEnumerable<FAGLL03ProcessedResult> CalculateJournalEntryEnumerable(IEnumerable<FAGLL03ProcessedResult> data)
    {
        foreach (var entity in data)
        {
            decimal net = entity.Amount_Local_Adjusted;
            if (entity.Base_Hyperion_Code == entity.Hyperion_Code)
            {
                entity.Base_Hyperion_Debit = 0m;
                entity.Destination_Hyperion_Credit = 0m;
            }
            else
            {
                entity.Base_Hyperion_Debit = Math.Abs(net);
                entity.Destination_Hyperion_Credit = Math.Abs(net);
            }
            yield return entity;
        }
    }

    public static decimal GetDecimalValue(DataRow row, string columnName)
    {
        if (row.IsNull(columnName)) return 0m;
        var val = row[columnName]?.ToString() ?? string.Empty;
        if (string.Equals(val, "NULL", StringComparison.OrdinalIgnoreCase)) return 0m;
        _ = decimal.TryParse(val, out decimal result);
        return result;
    }

    public static DataTable DeepCopyDataTable(DataTable original, string tableName)
    {
        var dt = new DataTable(tableName);
        foreach (DataColumn col in original.Columns)
            dt.Columns.Add(col.ColumnName, col.DataType);
        foreach (DataRow row in original.Rows)
            dt.ImportRow(row);
        return dt;
    }

    public static DataTable AppendTablesWithSameColumnNames(DataTable table1, DataTable table2, string name)
    {
        var result = DeepCopyDataTable(table1, name);
        foreach (DataRow row in table2.Rows)
            result.ImportRow(row);
        return result;
    }

    public static DateTime GetLastDateOfCurrentQuarter(DateTime date)
    {
        var m = date.Month;
        if (m <= 3) return new DateTime(date.Year, 3, 31);
        if (m <= 6) return new DateTime(date.Year, 6, 30);
        if (m <= 9) return new DateTime(date.Year, 9, 30);
        return new DateTime(date.Year, 12, 31);
    }

    public static DateTime GetLastDayOfPreviousQuarter(DateTime date)
    {
        int startMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateTime(date.Year, startMonth, 1).AddDays(-1);
    }

    public static DateTime GetLastDayOfQuarter(DateTime date)
    {
        int startMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateTime(date.Year, startMonth, 1).AddMonths(3).AddDays(-1);
    }

    public static decimal GetAmount(DataRow row, string col)
    {
        if (row.IsNull(col)) return 0m;
        _ = decimal.TryParse(row[col]?.ToString(), out decimal v);
        return v;
    }

    public static DateTime GetDate(DataRow r, string col)
    {
        var v = r[col];
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(v?.ToString(), out var parsed) ? parsed : default;
    }
}
