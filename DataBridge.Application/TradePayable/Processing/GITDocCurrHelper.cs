using DataBridge.Application.TradePayable.Extensions;
using System.Data;

namespace DataBridge.Application.TradePayable.Processing;

/// <summary>
/// Document-currency variant of GITHelper — operates on Amount_Doc instead of Amount_Local.
/// Grouping keys are Grouped_Invoice_Key_Doc / Grouped_Invoice_Key_Original_Doc.
/// </summary>
public class GITDocCurrHelper(HelperFunctions helper)
{
    // ── Grouping ──────────────────────────────────────────────────────────────

    public static List<DataTable> GroupFAGLL03DocCurrDataWithoutProfitCenter(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "Data with Doc Grouping Keys");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            GL = r.Field<string>("GL_Account"),
            Date = GetDate(r, "Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            Currency = r.Field<string>("Document_Currency"),
        }).ToList();

        var agg = BuildGroupedDocTable("Grouped FAGLL03 Table With DocCurr Without PC");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow r in g) r["Grouped_Invoice_Key_Original_Doc"] = guid;

            agg.Rows.Add(
                guid, g.Key.PO, g.Key.Vendor, g.Key.CC, g.Key.GL,
                g.First().Field<string>("Profit_Center"),
                g.First().Field<string>("Vendor_Description"),
                g.First().Field<string>("GL_Description"),
                g.First().Field<string>("Industry"),
                g.First().Field<string>("Credit_Period"),
                g.Key.Date, g.Key.Rev, g.Key.Currency,
                g.Sum(r => GetAmount(r, "Amount_Doc")),
                g.First().Field<string?>("ICP_Name"));
        }

        return [newTable, agg];
    }

    public static List<DataTable> GroupFAGLL03DocCurrDataWithProfitCenter(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "Data with Doc Grouping Keys with PC");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            GL = r.Field<string>("GL_Account"),
            PC = r.Field<string>("Profit_Center"),
            Date = GetDate(r, "Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            Currency = r.Field<string>("Document_Currency"),
        }).ToList();

        var agg = BuildGroupedDocTable("Grouped FAGLL03 Table With DocCurr With PC");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow r in g) r["Grouped_Invoice_Key_Original_Doc"] = guid;

            agg.Rows.Add(
                guid, g.Key.PO, g.Key.Vendor, g.Key.CC, g.Key.GL, g.Key.PC,
                g.First().Field<string>("Vendor_Description"),
                g.First().Field<string>("GL_Description"),
                g.First().Field<string>("Industry"),
                g.First().Field<string>("Credit_Period"),
                g.Key.Date, g.Key.Rev, g.Key.Currency,
                g.Sum(r => GetAmount(r, "Amount_Doc")),
                g.First().Field<string?>("ICP_Name"));
        }

        return [newTable, agg];
    }

    public static List<DataTable> GroupFAGLL03DocCurrDataForSNA(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "SNA Doc Grouping Keys");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            Vendor = r.Field<string>("Vendor"),
            Vertical = r.Field<string>("Vertical"),
            Date = GetDate(r, "Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            GL = r.Field<string>("GL_Account"),
            CC = r.Field<string>("Company_Code"),
            Currency = r.Field<string>("Document_Currency"),
        }).ToList();

        var agg = BuildSNADocTable("Grouped SNA DocCurr Data");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow r in g) r["Grouped_Invoice_Key_Original_Doc"] = guid;

            agg.Rows.Add(
                guid, g.Key.Vendor,
                g.Sum(r => GetAmount(r, "Amount_Doc")),
                g.First().Field<string>("Vendor_Description"),
                g.Key.Rev, g.Key.Date, g.Key.Vertical,
                g.Key.GL, g.First().Field<string>("GL_Description"),
                g.Key.CC, g.Key.Currency);
        }

        return [newTable, agg];
    }

    // ── Pivoting ──────────────────────────────────────────────────────────────

    public DataTable PivotLiabilityGLDocCurrDataWithoutProfitCenter(DataTable data)
    {
        var pivot = CreateDocPivotBase("Pivoted Liability GLs DocCurr w/o PC",
            ["Purchasing_Document", "Vendor", "Company_Code", "Profit_Center", "Report_Date", "RevisionNumber", "Document_Currency", "ICP_Name", "IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            Date = r.Field<DateTime>("Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            Curr = r.Field<string>("Document_Currency"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Purchasing_Document"] = g.Key.PO; row["Vendor"] = g.Key.Vendor;
            row["Company_Code"] = g.Key.CC; row["Profit_Center"] = g.First().Field<string>("Profit_Center");
            row["Report_Date"] = g.Key.Date; row["RevisionNumber"] = g.Key.Rev;
            row["Document_Currency"] = g.Key.Curr;
            FillDocLiabilityGLCols(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    public DataTable PivotLiabilityGLDocCurrDataWithProfitCenter(DataTable data)
    {
        var pivot = CreateDocPivotBase("Pivoted Liability GLs DocCurr with PC",
            ["Purchasing_Document", "Vendor", "Company_Code", "Profit_Center", "Report_Date", "RevisionNumber", "Document_Currency", "ICP_Name", "IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            PC = r.Field<string>("Profit_Center"),
            Date = r.Field<DateTime>("Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            Curr = r.Field<string>("Document_Currency"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Purchasing_Document"] = g.Key.PO; row["Vendor"] = g.Key.Vendor;
            row["Company_Code"] = g.Key.CC; row["Profit_Center"] = g.Key.PC;
            row["Report_Date"] = g.Key.Date; row["RevisionNumber"] = g.Key.Rev;
            row["Document_Currency"] = g.Key.Curr;
            FillDocLiabilityGLCols(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    public DataTable PivotFAGLL03DocCurrSNAData(DataTable data)
    {
        var pivot = CreateDocPivotBase("Pivoted Liability SNA DocCurr Data",
            ["Vendor","Vertical","Report_Date","RevisionNumber","Company_Code","Document_Currency",
             "Profit_Center","Purchasing_Document","ICP_Name","IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            Vendor = r.Field<string>("Vendor"),
            Vertical = r.Field<string>("Vertical"),
            Rev = r.Field<string>("RevisionNumber"),
            Date = r.Field<DateTime>("Report_Date"),
            CC = r.Field<string>("Company_Code"),
            Curr = r.Field<string>("Document_Currency"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Vendor"] = g.Key.Vendor; row["Vertical"] = g.Key.Vertical;
            row["RevisionNumber"] = g.Key.Rev; row["Report_Date"] = g.Key.Date;
            row["Company_Code"] = g.Key.CC; row["Document_Currency"] = g.Key.Curr;
            FillDocLiabilityGLCols(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    // ── Cascaded joins (doc currency) ─────────────────────────────────────────

    public DataTable PerformDocCurrCascadedJoinWithoutProfitCenter(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw GIT DocCurr Sheet w/o PC", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Purchasing_Document"), r.Field<string>("Vendor"),
                  r.Field<string>("Company_Code"), r.Field<DateTime>("Report_Date"),
                  r.Field<string>("RevisionNumber"), r.Field<string>("Document_Currency")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Purchasing_Document"), adv.Field<string>("Vendor"),
                       adv.Field<string>("Company_Code"), adv.Field<DateTime>("Report_Date"),
                       adv.Field<string>("RevisionNumber"), adv.Field<string>("Document_Currency"));
            ApplyCascadedJoin(adv, key, dict, result, helper, "Amount_Doc");
        }
        return result;
    }

    public DataTable PerformDocCurrCascadedJoinWithProfitCenter(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw GIT DocCurr Sheet with PC", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Purchasing_Document"), r.Field<string>("Vendor"),
                  r.Field<string>("Company_Code"), r.Field<string>("Profit_Center"),
                  r.Field<DateTime>("Report_Date"), r.Field<string>("RevisionNumber"),
                  r.Field<string>("Document_Currency")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Purchasing_Document"), adv.Field<string>("Vendor"),
                       adv.Field<string>("Company_Code"), adv.Field<string>("Profit_Center"),
                       adv.Field<DateTime>("Report_Date"), adv.Field<string>("RevisionNumber"),
                       adv.Field<string>("Document_Currency"));
            ApplyCascadedJoin(adv, key, dict, result, helper, "Amount_Doc");
        }
        return result;
    }

    public DataTable PerformCascadedJoinDocCurrSNAData(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw GIT DocCurr SNA Sheet", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Vendor"), r.Field<string>("Vertical"),
                  r.Field<string>("RevisionNumber"), r.Field<DateTime>("Report_Date"),
                  r.Field<string>("Company_Code"), r.Field<string>("Document_Currency")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Vendor"), adv.Field<string>("Vertical"),
                       adv.Field<string>("RevisionNumber"), adv.Field<DateTime>("Report_Date"),
                       adv.Field<string>("Company_Code"), adv.Field<string>("Document_Currency"));
            ApplyCascadedJoin(adv, key, dict, result, helper, "Amount_Doc");
        }
        return result;
    }

    // ── Static manipulation + unpivot ─────────────────────────────────────────

    public static DataTable GITAdvanceManipulationFAGLL03DocCurrData(DataTable gitData, bool isSNA = false)
    {
        var result = HelperFunctions.DeepCopyDataTable(gitData, "Processed FAGLL03 GIT DocCurr Sheet");
        result.Columns.Add("Adjusted_GL", typeof(string));
        result.Columns.Add("Adjusted_Amount", typeof(decimal));
        result.Columns.Add("Total_Adjustment", typeof(decimal));
        result.Columns.Add("Balance_Doc", typeof(string));

        string[] standardGLs = ["14005", "14006", "14007", "14012", "14021", "14701"];
        string[] snaGLs = ["14702", "14703", "14704", "14705"];

        foreach (DataRow row in result.Rows)
        {
            var vendor = row["Vendor"]?.ToString() ?? string.Empty;
            decimal total = HelperFunctions.GetDecimalValue(row, "Amount_Doc");

            row["Total_Adjustment"] = 0m;
            row["Balance_Doc"] = total;

            bool invalid = string.IsNullOrWhiteSpace(vendor) ||
                           vendor.Contains("not", StringComparison.OrdinalIgnoreCase) ||
                           vendor.Contains("null", StringComparison.OrdinalIgnoreCase);

            if (total <= 0 || invalid)
            {
                foreach (var gl in standardGLs.Concat(snaGLs)) row[gl] = "0";
                row["Adjusted_GL"] = total <= 0 ? "All GL set to 0, negative Advance" : "All GL set to 0, no Vendor!";
                row["Adjusted_Amount"] = 0m;
                continue;
            }

            decimal balance = total, adjusted = 0m;
            var glList = new List<string>(standardGLs);
            if (isSNA) glList.AddRange(snaGLs);

            foreach (var gl in glList)
            {
                decimal glVal = HelperFunctions.GetDecimalValue(row, gl);
                if (glVal > 0) { row[gl] = 0; continue; }

                decimal absGL = Math.Abs(glVal);
                if (absGL <= balance) { adjusted += absGL; balance -= absGL; row["Balance_Doc"] = balance; }
                else
                {
                    row[gl] = -balance; adjusted += balance; balance = 0;
                    row["Balance_Doc"] = 0m; row["Adjusted_Amount"] = absGL - adjusted; row["Adjusted_GL"] = gl;
                    int idx = glList.IndexOf(gl);
                    for (int i = idx + 1; i < glList.Count; i++) row[glList[i]] = 0;
                    break;
                }
            }

            row["Total_Adjustment"] = adjusted;
        }

        return result;
    }

    public static DataTable UnpivotProcessedGIT2(DataTable processedGIT)
    {
        var output = new DataTable("Unpivoted_ProcessedGIT_DocCurr");
        output.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
        output.Columns.Add("Purchasing_Document", typeof(string));
        output.Columns.Add("Vendor", typeof(string));
        output.Columns.Add("Company_Code", typeof(string));
        output.Columns.Add("GL_Account", typeof(string));
        output.Columns.Add("Profit_Center", typeof(string));
        output.Columns.Add("Amount_Doc", typeof(decimal));
        output.Columns.Add("Document_Currency", typeof(string));
        output.Columns.Add("Report_Date", typeof(DateTime));
        output.Columns.Add("Join_Type", typeof(string));
        output.Columns.Add("Total_Adjustment", typeof(decimal));
        output.Columns.Add("Balance_Doc", typeof(decimal));
        output.Columns.Add("Liability_GL_Code", typeof(string));
        output.Columns.Add("Liability_Amount", typeof(decimal));
        output.Columns.Add("Grouped_Key", typeof(Guid));
        output.Columns.Add("Advance_Adjustment", typeof(decimal));

        var glMap = new[] {
            ("14005","Grouped_Key_14005"),("14006","Grouped_Key_14006"),("14007","Grouped_Key_14007"),
            ("14012","Grouped_Key_14012"),("14021","Grouped_Key_14021"),("14701","Grouped_Key_14701"),
            ("14702","Grouped_Key_14702"),("14703","Grouped_Key_14703"),("14704","Grouped_Key_14704"),
            ("14705","Grouped_Key_14705"),
        };

        if (!processedGIT.Columns.Contains("Purchasing_Document"))
            processedGIT.Columns.Add("Purchasing_Document", typeof(string));

        if (!processedGIT.Columns.Contains("Profit_Center"))
            processedGIT.Columns.Add("Profit_Center", typeof(string));

        var results = from row in processedGIT.AsEnumerable()
                      from gl in glMap
                      let amt = row.Field<decimal?>(gl.Item1) ?? 0m
                      let key = row.Field<Guid?>(gl.Item2) ?? Guid.Empty
                      where amt != 0m
                      select (row, gl.Item1, amt, key);

        foreach (var (src, glCode, amt, key) in results)
        {
            var r = output.NewRow();
            r["Grouped_Invoice_Key_Doc"] = src["Grouped_Invoice_Key_Doc"];
            r["Purchasing_Document"] = src["Purchasing_Document"] ?? string.Empty;
            r["Vendor"] = src["Vendor"];
            r["Company_Code"] = src["Company_Code"];
            r["GL_Account"] = src["GL_Account"];
            r["Profit_Center"] = src["Profit_Center"] ?? string.Empty;
            r["Amount_Doc"] = src["Amount_Doc"];
            r["Document_Currency"] = src["Document_Currency"];
            r["Report_Date"] = src["Report_Date"];
            r["Join_Type"] = src["Join_Type"];
            r["Total_Adjustment"] = src["Total_Adjustment"];
            r["Balance_Doc"] = HelperFunctions.GetDecimalValue(src, "Balance_Doc");
            r["Liability_GL_Code"] = glCode;
            r["Liability_Amount"] = amt;
            r["Grouped_Key"] = key;
            r["Advance_Adjustment"] = Math.Abs(amt);
            output.Rows.Add(r);
        }

        return output;
    }

    public DataTable DocCurrLineItemWiseAdvanceAdjustment(DataTable populatedData, DataTable liabilityData)
    {
        var popCopy = HelperFunctions.DeepCopyDataTable(populatedData, "pop copy");
        var libCopy = HelperFunctions.DeepCopyDataTable(liabilityData, "lib copy");
        var libList = libCopy.AsEnumerable().ToList();
        var popList = popCopy.AsEnumerable().ToList();
        var joined = new List<Dictionary<string, object?>>();

        string[] snaGLs = ["14702", "14703", "14704", "14705"];

        foreach (DataRow p in popList)
        {
            var pKey = p.Field<Guid>("Grouped_Invoice_Key_Original_Doc");
            var advances = libList.Where(l => l.Field<Guid>("Grouped_Key") == pKey).ToList();
            var pivoted = PivotAdvanceGroupDoc(advances).FirstOrDefault();

            var row = new Dictionary<string, object?>();
            foreach (DataColumn col in p.Table.Columns)
                row[col.ColumnName] = p[col] == DBNull.Value ? null : p[col];
            row["Amount_Doc"] = HelperFunctions.GetDecimalValue(p, "Amount_Doc");

            if (pivoted != null)
            {
                foreach (DataColumn col in pivoted.Table.Columns)
                {
                    if (p.Table.Columns.Contains(col.ColumnName)) continue;
                    if (col.ColumnName is "Grouped_Invoice_Key_Doc" or "Join_Type" or "Liability_GL_Code" or "Grouped_Key" or "Invoice_Key") continue;
                    row["Advance_" + col.ColumnName] = HelperFunctions.GetDecimalValue(pivoted, col.ColumnName);
                }
            }
            else
            {
                foreach (var gl in helper.AdvanceGLs) row["Advance_" + gl.GL_Code] = 0m;
                row["Advance_TotalAdvanceAmount"] = 0m;
            }

            joined.Add(row);
        }

        var final = new List<Dictionary<string, object?>>();
        foreach (var group in joined.GroupBy(i => i["Grouped_Invoice_Key_Original_Doc"]))
        {
            var sorted = group.OrderByDescending(i => Math.Abs(Convert.ToDecimal(i["Amount_Doc"]))).ToList();
            decimal remaining = sorted.First().TryGetValue("Advance_TotalAdvanceAmount", out var adv)
                ? Convert.ToDecimal(adv) : 0m;

            foreach (var item in sorted)
            {
                decimal lineAmt = Convert.ToDecimal(item["Amount_Doc"]);
                decimal applied = 0m; string adjType = string.Empty;
                string icp = item.TryGetValue("ICP_Name", out var i) ? i?.ToString() ?? string.Empty : string.Empty;
                string gl = item.TryGetValue("GL_Account", out var g) ? g?.ToString() ?? string.Empty : string.Empty;
                bool isSNA = !string.IsNullOrWhiteSpace(icp) &&
                             !icp.Equals("null", StringComparison.OrdinalIgnoreCase) &&
                             !icp.Equals("not", StringComparison.OrdinalIgnoreCase);

                bool eligible = !gl.StartsWith('2') && (isSNA || !snaGLs.Contains(gl));

                if (remaining > 0 && eligible && lineAmt < 0)
                {
                    applied = Math.Min(remaining, Math.Abs(lineAmt));
                    remaining -= applied;
                    adjType = "Advance Adjustment";
                }

                var finalRow = new Dictionary<string, object?>(item)
                {
                    ["Advance_Applied"] = applied,
                    ["Amount_Doc_Adjusted"] = lineAmt + applied,
                    ["Adjustment_Type"] = adjType,
                    ["Total_Remaining_Advance"] = remaining,
                };
                final.Add(finalRow);
            }
        }

        return GITHelper.ConvertDictionaryListToDataTable(final, "DocCurrLineItemWiseAdvanceAdjustment");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<DataRow> PivotAdvanceGroupDoc(List<DataRow> group)
    {
        if (group.Count == 0) return [];
        var first = group[0];
        var dt = new DataTable("Pivoted Advance Group Doc");

        foreach (DataColumn col in first.Table.Columns)
            if (col.ColumnName != "GL_Account" && col.ColumnName != "Advance_Adjustment")
                dt.Columns.Add(col.ColumnName, col.DataType);

        foreach (var gl in helper.AdvanceGLs) dt.Columns.Add(gl.GL_Code, typeof(decimal));
        dt.Columns.Add("TotalAdvanceAmount", typeof(decimal));

        var pivotRow = dt.NewRow();
        foreach (DataColumn col in first.Table.Columns)
            if (col.ColumnName != "GL_Account" && col.ColumnName != "Advance_Adjustment")
                pivotRow[col.ColumnName] = first[col.ColumnName];

        decimal total = 0m;
        foreach (var r in group)
        {
            var gl = r["GL_Account"]?.ToString();
            var amt = Convert.ToDecimal(r["Advance_Adjustment"]);
            total += amt;
            if (dt.Columns.Contains(gl!)) pivotRow[gl!] = amt;
        }

        pivotRow["TotalAdvanceAmount"] = total;
        dt.Rows.Add(pivotRow);
        return [.. dt.AsEnumerable()];
    }

    private static DataTable BuildGroupedDocTable(string name)
    {
        var dt = new DataTable(name);
        dt.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
        dt.Columns.Add("Purchasing_Document", typeof(string));
        dt.Columns.Add("Vendor", typeof(string));
        dt.Columns.Add("Company_Code", typeof(string));
        dt.Columns.Add("GL_Account", typeof(string));
        dt.Columns.Add("Profit_Center", typeof(string));
        dt.Columns.Add("Vendor_Description", typeof(string));
        dt.Columns.Add("GL_Description", typeof(string));
        dt.Columns.Add("Industry", typeof(string));
        dt.Columns.Add("Credit_Period", typeof(string));
        dt.Columns.Add("Report_Date", typeof(DateTime));
        dt.Columns.Add("RevisionNumber", typeof(string));
        dt.Columns.Add("Document_Currency", typeof(string));
        dt.Columns.Add("Amount_Doc", typeof(decimal));
        dt.Columns.Add("ICP_Name", typeof(string));
        return dt;
    }

    private static DataTable BuildSNADocTable(string name)
    {
        var dt = new DataTable(name);
        dt.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
        dt.Columns.Add("Vendor", typeof(string));
        dt.Columns.Add("Amount_Doc", typeof(decimal));
        dt.Columns.Add("Vendor_Description", typeof(string));
        dt.Columns.Add("RevisionNumber", typeof(string));
        dt.Columns.Add("Report_Date", typeof(DateTime));
        dt.Columns.Add("Vertical", typeof(string));
        dt.Columns.Add("GL_Account", typeof(string));
        dt.Columns.Add("GL_Description", typeof(string));
        dt.Columns.Add("Company_Code", typeof(string));
        dt.Columns.Add("Document_Currency", typeof(string));
        return dt;
    }

    private DataTable CreateDocPivotBase(string name, IEnumerable<string> dims)
    {
        var dt = new DataTable(name);
        foreach (var col in dims) dt.Columns.Add(col, typeof(string));
        if (dt.Columns.Contains("Report_Date")) dt.Columns["Report_Date"]!.DataType = typeof(DateTime);
        if (dt.Columns.Contains("IsSNACompany")) dt.Columns["IsSNACompany"]!.DataType = typeof(bool);

        foreach (var gl in helper.LiabilityGLs)
        {
            dt.Columns.Add(gl.GL_Code, typeof(decimal));
            dt.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
        }
        return dt;
    }

    private static void FillDocLiabilityGLCols(DataRow row, IEnumerable<DataRow> group, HelperFunctions h)
    {
        foreach (var src in group)
        {
            var gl = src.Field<string>("GL_Account");
            var amt = src.Field<decimal>("Amount_Doc");
            if (gl is not null && row.Table.Columns.Contains(gl))
            {
                row[gl] = amt;
                row[$"Grouped_Key_{gl}"] = src.Field<Guid>("Grouped_Invoice_Key_Doc");
            }
        }
    }

    private class GlData(decimal amt, Guid key) { public decimal Amount = amt; public Guid Key = key; }

    private static Dictionary<string, GlData> BuildGlAmountMap(DataRow r, HelperFunctions h) =>
        h.LiabilityGLs.ToDictionary(
            gl => gl.GL_Code,
            gl =>
            {
                decimal v = r.Field<decimal?>(gl.GL_Code) ?? 0m;
                Guid g = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}") ?? Guid.Empty;
                return new GlData(v > 0 ? 0m : v, g);
            });

    private static DataTable BuildCascadedResult(DataTable advances, string name, HelperFunctions h)
    {
        var result = advances.Clone();
        result.TableName = name;
        foreach (var gl in h.LiabilityGLs)
        {
            result.Columns.Add(gl.GL_Code, typeof(decimal));
            result.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
        }
        result.Columns.Add("Join_Type", typeof(string));
        return result;
    }

    private static void ApplyCascadedJoin<TKey>(DataRow adv, TKey key,
        Dictionary<TKey, Dictionary<string, GlData>> dict,
        DataTable result, HelperFunctions h, string amtCol) where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var matchedGLs))
        {
            var noMatch = result.NewRow();
            noMatch.ItemArray = (object[])adv.ItemArray.Clone()!;
            noMatch["Join_Type"] = "No Match";
            result.Rows.Add(noMatch);
            return;
        }

        decimal advAmt = adv.Field<decimal>(amtCol);
        var newRow = result.NewRow();
        newRow.ItemArray = (object[])adv.ItemArray.Clone()!;
        newRow["Join_Type"] = "Full Composite";
        bool processed = false;

        foreach (var gl in h.LiabilityGLs)
        {
            var data = matchedGLs[gl.GL_Code];
            newRow[$"Grouped_Key_{gl.GL_Code}"] = data.Key;

            if (data.Amount == 0) { newRow[gl.GL_Code] = 0m; continue; }

            processed = true;
            if (Math.Abs(data.Amount) > Math.Abs(advAmt))
            {
                decimal consumed = -Math.Abs(advAmt);
                data.Amount -= consumed;
                newRow[gl.GL_Code] = consumed;
            }
            else
            {
                newRow[gl.GL_Code] = data.Amount;
                data.Amount = 0;
            }
        }

        if (!processed) newRow["Join_Type"] = "No Match";
        result.Rows.Add(newRow);
        if (matchedGLs.Values.All(v => v.Amount == 0)) dict.Remove(key);
    }

    // expose ConvertDictionaryListToDataTable for reuse from GITHelper
    internal static DataTable ConvertDictionaryListToDataTable(List<Dictionary<string, object?>> data, string name) =>
        GITHelper.ConvertDictionaryListToDataTable(data, name);

    private static decimal GetAmount(DataRow row, string col)
    {
        if (row.IsNull(col)) return 0m;
        _ = decimal.TryParse(row[col]?.ToString(), out decimal v);
        return v;
    }

    private static DateTime GetDate(DataRow r, string col)
    {
        var v = r[col];
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(v?.ToString(), out var parsed) ? parsed : default;
    }
}
