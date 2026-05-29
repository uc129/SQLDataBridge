using DataBridge.Application.TradePayable.Extensions;
using System.Data;

namespace DataBridge.Application.TradePayable.Processing;

public class GITHelper(HelperFunctions helper)
{
    // ── Grouping ──────────────────────────────────────────────────────────────

    public static List<DataTable> GroupFAGLL03DataWithoutProfitCenter(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "Processed Data with grouping keys");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            GL = r.Field<string>("GL_Account"),
            Rev = r.Field<string>("RevisionNumber"),
            ReportDate = GetDate(r, "Report_Date"),
            ICP = r.Field<string?>("ICP_Name"),
            IsSNA = r.Field<string?>("IsSNACompany") == "True",
        }).ToList();

        var agg = BuildGroupedTable("Grouped FAGLL03 Table w/o PC");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow row in g) row["Grouped_Invoice_Key_Original"] = guid;

            agg.Rows.Add(
                guid, g.Key.PO, g.Key.Vendor, g.Key.CC, g.Key.GL,
                g.First().Field<string>("Profit_Center"),
                g.Sum(r => GetAmount(r, "Amount_Local")),
                g.First().Field<string>("Vendor_Description"),
                g.First().Field<string>("GL_Description"),
                g.First().Field<string>("Industry"),
                g.First().Field<string>("Credit_Period"),
                g.Key.Rev, g.Key.ReportDate, g.Key.ICP, g.Key.IsSNA);
        }

        return [newTable, agg];
    }

    public static List<DataTable> GroupFAGLL03DataWithProfitCenter(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "Processed data with Grouping Keys with PC");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            GL = r.Field<string>("GL_Account"),
            PC = r.Field<string>("Profit_Center"),
            Rev = r.Field<string>("RevisionNumber"),
            ReportDate = GetDate(r, "Report_Date"),
            ICP = r.Field<string?>("ICP_Name"),
            IsSNA = r.Field<string?>("IsSNACompany") == "True",
        }).ToList();

        var agg = BuildGroupedTable("Grouped FAGLL03 Table with PC");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow row in g) row["Grouped_Invoice_Key_Original"] = guid;

            agg.Rows.Add(
                guid, g.Key.PO, g.Key.Vendor, g.Key.CC, g.Key.GL, g.Key.PC,
                g.Sum(r => GetAmount(r, "Amount_Local")),
                g.First().Field<string>("Vendor_Description"),
                g.First().Field<string>("GL_Description"),
                g.First().Field<string>("Industry"),
                g.First().Field<string>("Credit_Period"),
                g.Key.Rev, g.Key.ReportDate, g.Key.ICP, g.Key.IsSNA);
        }

        return [newTable, agg];
    }

    public static List<DataTable> GroupFAGLL03DataForSNA(DataTable data)
    {
        var newTable = HelperFunctions.DeepCopyDataTable(data, "SNA data with Grouping Keys");
        if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
            newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

        var groups = newTable.AsEnumerable().GroupBy(r => new
        {
            Vendor = r.Field<string>("Vendor"),
            Vertical = r.Field<string>("Vertical"),
            ReportDate = GetDate(r, "Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
            GL = r.Field<string>("GL_Account"),
            CC = r.Field<string>("Company_Code"),
        }).ToList();

        var agg = BuildSNAGroupedTable("Grouped SNA Data");

        foreach (var g in groups)
        {
            var guid = Guid.NewGuid();
            foreach (DataRow row in g) row["Grouped_Invoice_Key_Original"] = guid;

            agg.Rows.Add(
                guid, g.Key.Vendor,
                g.Sum(r => GetAmount(r, "Amount_Local")),
                g.First().Field<string>("Vendor_Description"),
                g.Key.Rev, g.Key.ReportDate, g.Key.Vertical,
                g.Key.GL, g.First().Field<string>("GL_Description"), g.Key.CC);
        }

        return [newTable, agg];
    }

    // ── Pivoting ──────────────────────────────────────────────────────────────

    public DataTable PivotLiabilityGLDataWithoutProfitCenter(DataTable data)
    {
        var pivot = CreateLiabilityPivotBase("Pivoted Liability GLs w/o PC",
            ["Purchasing_Document", "Vendor", "Company_Code", "Profit_Center", "Report_Date", "RevisionNumber", "ICP_Name", "IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            Date = r.Field<DateTime>("Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Purchasing_Document"] = g.Key.PO;
            row["Vendor"] = g.Key.Vendor;
            row["Company_Code"] = g.Key.CC;
            row["Profit_Center"] = g.First().Field<string>("Profit_Center");
            row["Report_Date"] = g.Key.Date;
            row["RevisionNumber"] = g.Key.Rev;
            FillLiabilityGLColumns(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    public DataTable PivotLiabilityGLDataWithProfitCenter(DataTable data)
    {
        var pivot = CreateLiabilityPivotBase("Pivoted Liability GLs with PC",
            ["Purchasing_Document", "Vendor", "Company_Code", "Profit_Center", "Report_Date", "RevisionNumber", "ICP_Name", "IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            PO = r.Field<string>("Purchasing_Document"),
            Vendor = r.Field<string>("Vendor"),
            CC = r.Field<string>("Company_Code"),
            PC = r.Field<string>("Profit_Center"),
            Date = r.Field<DateTime>("Report_Date"),
            Rev = r.Field<string>("RevisionNumber"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Purchasing_Document"] = g.Key.PO;
            row["Vendor"] = g.Key.Vendor;
            row["Company_Code"] = g.Key.CC;
            row["Profit_Center"] = g.Key.PC;
            row["Report_Date"] = g.Key.Date;
            row["RevisionNumber"] = g.Key.Rev;
            FillLiabilityGLColumns(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    public DataTable PivotFAGLL03SNAData(DataTable data)
    {
        var pivot = CreateLiabilityPivotBase("Pivoted Liability SNA Data",
            ["Vendor", "Vertical", "Report_Date", "RevisionNumber", "Company_Code",
             "Profit_Center", "Purchasing_Document", "ICP_Name", "IsSNACompany"]);

        var groups = data.AsEnumerable().GroupBy(r => new
        {
            Vendor = r.Field<string>("Vendor"),
            Vertical = r.Field<string>("Vertical"),
            Rev = r.Field<string>("RevisionNumber"),
            Date = r.Field<DateTime>("Report_Date"),
            CC = r.Field<string>("Company_Code"),
        });

        foreach (var g in groups)
        {
            var row = pivot.NewRow();
            row["Vendor"] = g.Key.Vendor;
            row["Vertical"] = g.Key.Vertical;
            row["RevisionNumber"] = g.Key.Rev;
            row["Report_Date"] = g.Key.Date;
            row["Company_Code"] = g.Key.CC;
            FillLiabilityGLColumns(row, g, helper);
            pivot.Rows.Add(row);
        }
        return pivot;
    }

    // ── Cascaded joins ────────────────────────────────────────────────────────

    public DataTable PerformCascadedJoinWithoutProfitCenter(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw FAGLL03 GIT Sheet Without Profit Center", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Purchasing_Document"), r.Field<string>("Vendor"),
                  r.Field<string>("Company_Code"), r.Field<DateTime>("Report_Date"),
                  r.Field<string>("RevisionNumber")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Purchasing_Document"), adv.Field<string>("Vendor"),
                       adv.Field<string>("Company_Code"), adv.Field<DateTime>("Report_Date"),
                       adv.Field<string>("RevisionNumber"));
            ApplyCascadedJoin(adv, key, dict, result, helper);
        }

        return result;
    }

    public DataTable PerformCascadedJoinWithProfitCenter(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw FAGLL03 GIT Sheet With Profit Center", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Purchasing_Document"), r.Field<string>("Vendor"),
                  r.Field<string>("Company_Code"), r.Field<string>("Profit_Center"),
                  r.Field<DateTime>("Report_Date"), r.Field<string>("RevisionNumber")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Purchasing_Document"), adv.Field<string>("Vendor"),
                       adv.Field<string>("Company_Code"), adv.Field<string>("Profit_Center"),
                       adv.Field<DateTime>("Report_Date"), adv.Field<string>("RevisionNumber"));
            ApplyCascadedJoin(adv, key, dict, result, helper);
        }

        return result;
    }

    public DataTable PerformCascadedJoinSNAData(DataTable advances, DataTable pivoted)
    {
        var result = BuildCascadedResult(advances, "Raw FAGLL03 GIT Sheet for SNA Data", helper);
        pivoted.ReplaceNullsWithZero();

        var dict = pivoted.AsEnumerable().ToDictionary(
            r => (r.Field<string>("Vendor"), r.Field<string>("Vertical"),
                  r.Field<string>("RevisionNumber"), r.Field<DateTime>("Report_Date"),
                  r.Field<string>("Company_Code")),
            r => BuildGlAmountMap(r, helper));

        foreach (DataRow adv in advances.Rows)
        {
            var key = (adv.Field<string>("Vendor"), adv.Field<string>("Vertical"),
                       adv.Field<string>("RevisionNumber"), adv.Field<DateTime>("Report_Date"),
                       adv.Field<string>("Company_Code"));
            ApplyCascadedJoin(adv, key, dict, result, helper);
        }

        return result;
    }

    // ── Static manipulation + filter methods ──────────────────────────────────

    public static DataTable GITAdvanceManipulationFAGLL03Data(DataTable gitData, bool isSNA = false)
    {
        var result = HelperFunctions.DeepCopyDataTable(gitData, "Processed FAGLL03 GIT Sheet");
        result.Columns.Add("Adjusted_GL", typeof(string));
        result.Columns.Add("Adjusted_Amount", typeof(decimal));
        result.Columns.Add("Total_Adjustment", typeof(decimal));
        result.Columns.Add("Balance_Local", typeof(string));

        string[] standardGLs = ["14005", "14006", "14007", "14012", "14021", "14701"];
        string[] snaGLs = ["14702", "14703", "14704", "14705"];

        foreach (DataRow row in result.Rows)
        {
            var vendor = row["Vendor"]?.ToString() ?? string.Empty;
            decimal total = HelperFunctions.GetDecimalValue(row, "Amount_Local");

            row["Total_Adjustment"] = 0m;
            row["Balance_Local"] = total;

            bool invalid = string.IsNullOrWhiteSpace(vendor) ||
                           vendor.Contains("not", StringComparison.OrdinalIgnoreCase) ||
                           vendor.Contains("null", StringComparison.OrdinalIgnoreCase);

            if (total <= 0 || invalid)
            {
                foreach (var gl in standardGLs.Concat(snaGLs)) row[gl] = "0";
                row["Adjusted_GL"] = total <= 0 ? "All GL set to 0, negative Advance" : "All GL set to 0, no Vendor Found!";
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
                if (absGL <= balance)
                {
                    adjusted += absGL;
                    balance -= absGL;
                    row["Balance_Local"] = balance;
                }
                else
                {
                    decimal diff = absGL - balance;
                    adjusted += balance;
                    row[gl] = -balance;
                    balance = 0;
                    row["Balance_Local"] = 0m;
                    row["Adjusted_Amount"] = diff;
                    row["Adjusted_GL"] = gl;

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
        var output = new DataTable("Unpivoted_ProcessedGIT");
        output.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
        output.Columns.Add("Purchasing_Document", typeof(string));
        output.Columns.Add("Vendor", typeof(string));
        output.Columns.Add("Company_Code", typeof(string));
        output.Columns.Add("GL_Account", typeof(string));
        output.Columns.Add("Profit_Center", typeof(string));
        output.Columns.Add("Amount_Local", typeof(decimal));
        output.Columns.Add("Report_Date", typeof(DateTime));
        output.Columns.Add("Join_Type", typeof(string));
        output.Columns.Add("Total_Adjustment", typeof(decimal));
        output.Columns.Add("Balance_Local", typeof(decimal));
        output.Columns.Add("Liability_GL_Code", typeof(string));
        output.Columns.Add("Liability_Amount", typeof(decimal));
        output.Columns.Add("Grouped_Key", typeof(Guid));
        output.Columns.Add("Advance_Adjustment", typeof(decimal));

        if (!processedGIT.Columns.Contains("Purchasing_Document"))
            processedGIT.Columns.Add("Purchasing_Document", typeof(string));

        if (!processedGIT.Columns.Contains("Profit_Center"))
            processedGIT.Columns.Add("Profit_Center", typeof(string));

        var glMap = new[] {
            ("14005","Grouped_Key_14005"),("14006","Grouped_Key_14006"),("14007","Grouped_Key_14007"),
            ("14012","Grouped_Key_14012"),("14021","Grouped_Key_14021"),("14701","Grouped_Key_14701"),
            ("14702","Grouped_Key_14702"),("14703","Grouped_Key_14703"),("14704","Grouped_Key_14704"),
            ("14705","Grouped_Key_14705"),
        };

        var results = from row in processedGIT.AsEnumerable()
                      from gl in glMap
                      let amt = row.Field<decimal?>(gl.Item1) ?? 0m
                      let key = row.Field<Guid?>(gl.Item2) ?? Guid.Empty
                      where amt != 0m
                      select (row, gl.Item1, amt, key);

        foreach (var (src, glCode, amt, key) in results)
        {
            var r = output.NewRow();
            r["Grouped_Invoice_Key"] = src["Grouped_Invoice_Key"];
            r["Purchasing_Document"] = src["Purchasing_Document"] ?? string.Empty;
            r["Vendor"] = src["Vendor"];
            r["Company_Code"] = src["Company_Code"];
            r["GL_Account"] = src["GL_Account"];
            r["Profit_Center"] = src["Profit_Center"] ?? string.Empty;
            r["Amount_Local"] = src["Amount_Local"];
            r["Report_Date"] = src["Report_Date"];
            r["Join_Type"] = src["Join_Type"];
            r["Total_Adjustment"] = src["Total_Adjustment"];
            r["Balance_Local"] = HelperFunctions.GetDecimalValue(src, "Balance_Local");
            r["Liability_GL_Code"] = glCode;
            r["Liability_Amount"] = amt;
            r["Grouped_Key"] = key;
            r["Advance_Adjustment"] = Math.Abs(amt);
            output.Rows.Add(r);
        }

        return output;
    }

    public static DataTable FilterForDataWithPO(DataTable table)
    {
        var filtered = table.AsEnumerable().Where(r =>
        {
            var po = r.Field<string>("Purchasing_Document") ?? string.Empty;
            var isSNA = r.Field<string>("IsSNACompany") == "True";
            return !isSNA && !string.IsNullOrWhiteSpace(po) &&
                   !po.StartsWith("no", StringComparison.OrdinalIgnoreCase);
        });
        if (!filtered.Any()) throw new InvalidOperationException("FilterForDataWithPO returned no rows.");
        return HelperFunctions.DeepCopyDataTable(filtered.CopyToDataTable(), "Data with PO");
    }

    public static DataTable FilterForDataWithoutPO(DataTable table)
    {
        var filtered = table.AsEnumerable().Where(r =>
        {
            var po = r.Field<string>("Purchasing_Document") ?? string.Empty;
            var isSNA = r.Field<string>("IsSNACompany") == "True";
            return !isSNA && (string.IsNullOrWhiteSpace(po) ||
                              po.StartsWith("no", StringComparison.OrdinalIgnoreCase));
        });
        if (!filtered.Any()) throw new InvalidOperationException("FilterForDataWithoutPO returned no rows.");
        return HelperFunctions.DeepCopyDataTable(filtered.CopyToDataTable(), "Data without PO");
    }

    public static DataTable FilterForSNAData(DataTable table)
    {
        var filtered = table.AsEnumerable().Where(r => r.Field<string>("IsSNACompany") == "True");
        if (!filtered.Any()) throw new InvalidOperationException("FilterForSNAData returned no rows.");
        return HelperFunctions.DeepCopyDataTable(filtered.CopyToDataTable(), "SNA only Data");
    }

    public static DataTable FilterForAdvanceGLs(DataTable table)
    {
        var filtered = table.AsEnumerable().Where(r => r.Field<string>("GL_Account")!.StartsWith('2'));
        if (!filtered.Any()) throw new InvalidOperationException("No advance GL rows found.");
        return HelperFunctions.DeepCopyDataTable(filtered.CopyToDataTable(), "Only Advance GLs");
    }

    public static DataTable FilterForLiabilityGLs(DataTable table, IEnumerable<string> glCodes)
    {
        var set = new HashSet<string>(glCodes, StringComparer.OrdinalIgnoreCase);
        var filtered = table.AsEnumerable().Where(r => set.Contains(r.Field<string>("GL_Account") ?? string.Empty));
        if (!filtered.Any()) throw new InvalidOperationException("No liability GL rows found.");
        return HelperFunctions.DeepCopyDataTable(filtered.CopyToDataTable(), "Only Liability GLs");
    }

    // ── LineItemWiseAdvanceAdjustment ─────────────────────────────────────────

    public DataTable LineItemWiseAdvanceAdjustment(DataTable populatedData, DataTable liabilityData)
    {
        var popCopy = HelperFunctions.DeepCopyDataTable(populatedData, "pop copy");
        var libCopy = HelperFunctions.DeepCopyDataTable(liabilityData, "lib copy");
        var libList = libCopy.AsEnumerable().ToList();
        var popList = popCopy.AsEnumerable().ToList();
        var joined = new List<Dictionary<string, object?>>();

        string[] snaGLs = ["14702", "14703", "14704", "14705"];

        foreach (DataRow p in popList)
        {
            var pKey = p.Field<Guid>("Grouped_Invoice_Key_Original");
            var advances = libList.Where(l => l.Field<Guid>("Grouped_Key") == pKey).ToList();
            var pivoted = PivotAdvanceGroup(advances).FirstOrDefault();

            var row = new Dictionary<string, object?>();
            foreach (DataColumn col in p.Table.Columns)
                row[col.ColumnName] = p[col] == DBNull.Value ? null : p[col];
            row["Amount_Local"] = HelperFunctions.GetDecimalValue(p, "Amount_Local");

            if (pivoted != null)
            {
                foreach (DataColumn col in pivoted.Table.Columns)
                {
                    if (p.Table.Columns.Contains(col.ColumnName)) continue;
                    if (col.ColumnName is "Grouped_Invoice_Key" or "Join_Type" or "Liability_GL_Code" or "Grouped_Key" or "Invoice_Key") continue;
                    row["Advance_" + col.ColumnName] = HelperFunctions.GetDecimalValue(pivoted, col.ColumnName);
                }
            }
            else
            {
                foreach (var gl in helper.AdvanceGLs)
                    row["Advance_" + gl.GL_Code] = 0m;
                row["Advance_TotalAdvanceAmount"] = 0m;
            }

            joined.Add(row);
        }

        var final = new List<Dictionary<string, object?>>();
        foreach (var group in joined.GroupBy(i => i["Grouped_Invoice_Key_Original"]))
        {
            var sorted = group.OrderByDescending(i => Math.Abs(Convert.ToDecimal(i["Amount_Local"]))).ToList();
            decimal remaining = sorted.First().TryGetValue("Advance_TotalAdvanceAmount", out var adv)
                ? Convert.ToDecimal(adv) : 0m;

            foreach (var item in sorted)
            {
                decimal lineAmt = Convert.ToDecimal(item["Amount_Local"]);
                decimal applied = 0m;
                string adjType = string.Empty;
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
                    ["Amount_Local_Adjusted"] = lineAmt + applied,
                    ["Adjustment_Type"] = adjType,
                    ["Total_Remaining_Advance"] = remaining,
                };
                final.Add(finalRow);
            }
        }

        return ConvertDictionaryListToDataTable(final, "LineItemWiseAdvanceAdjustment");
    }

    // ── Pivot advance group ───────────────────────────────────────────────────

    public List<DataRow> PivotAdvanceGroup(List<DataRow> group)
    {
        if (group.Count == 0) return [];
        var first = group[0];
        var dt = new DataTable("Pivoted Advance Group");

        foreach (DataColumn col in first.Table.Columns)
            if (col.ColumnName != "GL_Account" && col.ColumnName != "Advance_Adjustment")
                dt.Columns.Add(col.ColumnName, col.DataType);

        foreach (var gl in helper.AdvanceGLs)
            dt.Columns.Add(gl.GL_Code, typeof(decimal));
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DataTable BuildGroupedTable(string name)
    {
        var dt = new DataTable(name);
        dt.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
        dt.Columns.Add("Purchasing_Document", typeof(string));
        dt.Columns.Add("Vendor", typeof(string));
        dt.Columns.Add("Company_Code", typeof(string));
        dt.Columns.Add("GL_Account", typeof(string));
        dt.Columns.Add("Profit_Center", typeof(string));
        dt.Columns.Add("Amount_Local", typeof(decimal));
        dt.Columns.Add("Vendor_Description", typeof(string));
        dt.Columns.Add("GL_Description", typeof(string));
        dt.Columns.Add("Industry", typeof(string));
        dt.Columns.Add("Credit_Period", typeof(string));
        dt.Columns.Add("RevisionNumber", typeof(string));
        dt.Columns.Add("Report_Date", typeof(DateTime));
        dt.Columns.Add("ICP_Name", typeof(string));
        dt.Columns.Add("IsSNACompany", typeof(bool));
        return dt;
    }

    private static DataTable BuildSNAGroupedTable(string name)
    {
        var dt = new DataTable(name);
        dt.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
        dt.Columns.Add("Vendor", typeof(string));
        dt.Columns.Add("Amount_Local", typeof(decimal));
        dt.Columns.Add("Vendor_Description", typeof(string));
        dt.Columns.Add("RevisionNumber", typeof(string));
        dt.Columns.Add("Report_Date", typeof(DateTime));
        dt.Columns.Add("Vertical", typeof(string));
        dt.Columns.Add("GL_Account", typeof(string));
        dt.Columns.Add("GL_Description", typeof(string));
        dt.Columns.Add("Company_Code", typeof(string));
        return dt;
    }

    private DataTable CreateLiabilityPivotBase(string name, IEnumerable<string> dimensionCols)
    {
        var dt = new DataTable(name);
        foreach (var col in dimensionCols) dt.Columns.Add(col, typeof(string));
        // override specific types
        if (dt.Columns.Contains("Report_Date")) dt.Columns["Report_Date"]!.DataType = typeof(DateTime);
        if (dt.Columns.Contains("IsSNACompany")) dt.Columns["IsSNACompany"]!.DataType = typeof(bool);

        foreach (var gl in helper.LiabilityGLs)
        {
            dt.Columns.Add(gl.GL_Code, typeof(decimal));
            dt.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
        }
        return dt;
    }

    private static void FillLiabilityGLColumns(DataRow row, IEnumerable<DataRow> group, HelperFunctions h)
    {
        foreach (var src in group)
        {
            var gl = src.Field<string>("GL_Account");
            var amt = src.Field<decimal>("Amount_Local");
            if (gl is not null && row.Table.Columns.Contains(gl))
            {
                row[gl] = amt;
                row[$"Grouped_Key_{gl}"] = src.Field<Guid>("Grouped_Invoice_Key");
            }
        }
    }

    private class GlAmountData(decimal amount, Guid key)
    {
        public decimal Amount = amount;
        public Guid Key = key;
    }

    private static Dictionary<string, GlAmountData> BuildGlAmountMap(DataRow r, HelperFunctions h) =>
        h.LiabilityGLs.ToDictionary(
            gl => gl.GL_Code,
            gl =>
            {
                decimal val = r.Field<decimal?>(gl.GL_Code) ?? 0m;
                Guid guid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}") ?? Guid.Empty;
                return new GlAmountData(val > 0 ? 0m : val, guid);
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
        Dictionary<TKey, Dictionary<string, GlAmountData>> dict,
        DataTable result, HelperFunctions h) where TKey : notnull
    {
        if (!dict.TryGetValue(key, out var matchedGLs))
        {
            var noMatch = result.NewRow();
            noMatch.ItemArray = (object[])adv.ItemArray.Clone()!;
            noMatch["Join_Type"] = "No Match";
            result.Rows.Add(noMatch);
            return;
        }

        decimal advAmt = adv.Field<decimal>("Amount_Local");
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

        if (matchedGLs.Values.All(v => v.Amount == 0))
            dict.Remove(key);
    }

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

    public static DataTable ConvertDictionaryListToDataTable(List<Dictionary<string, object?>> data, string name)
    {
        if (data.Count == 0) return new DataTable(name);
        var dt = new DataTable(name);

        foreach (var key in data[0].Keys)
        {
            var sample = data.Select(d => d.TryGetValue(key, out var v) ? v : null).FirstOrDefault(v => v != null);
            var type = sample?.GetType() ?? typeof(string);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = Nullable.GetUnderlyingType(type)!;
            if (type == typeof(int) || type == typeof(long) || type == typeof(double)) type = typeof(decimal);
            dt.Columns.Add(key, type);
        }

        foreach (var dict in data)
        {
            var row = dt.NewRow();
            foreach (DataColumn col in dt.Columns)
            {
                var val = dict.TryGetValue(col.ColumnName, out var v) ? v : null;
                row[col.ColumnName] = val is null ? DBNull.Value : Convert.ChangeType(val, col.DataType);
            }
            dt.Rows.Add(row);
        }

        return dt;
    }
}
