using DataBridge.Domain.TradePayable.Aggregates;
using System.Data;
using System.Text.RegularExpressions;

namespace DataBridge.Application.TradePayable.Processing;

public class DataProcessor(HelperFunctions helper)
{
    // ── Step 1: Vendor + PO extraction ───────────────────────────────────────

    public IEnumerable<FAGLL03Populated> ProcessRawData(IEnumerable<FAGLL03RAWEntity> rawData, Guid runId)
    {
        var records = rawData.Select(r => new FAGLL03Populated
        {
            Invoice_Key = r.Invoice_Key,
            Document_Number = r.Document_Number,
            Purchasing_Document = r.Purchasing_Document,
            Invoice_Reference = r.Invoice_Reference,
            Document_Header = r.Document_Header,
            Document_Type = r.Document_Type,
            Company_Code = r.Company_Code,
            Assignment = r.Assignment,
            Vendor = r.Vendor,
            Vendor_Description = r.Vendor_Description,
            Invoice_Description = r.Invoice_Description,
            Industry = r.Industry,
            Amount_Local = r.Amount_Local,
            GL_Account = r.GL_Account,
            GL_Description = r.GL_Description,
            Profit_Center = r.Profit_Center,
            Payment_Terms = r.Payment_Terms,
            Document_Currency = r.Document_Currency,
            Amount_Doc = r.Amount_Doc,
            Document_Date = r.Document_Date,
            Posting_Date = r.Posting_Date,
            Payment_Date = r.Payment_Date,
            User_Name = r.User_Name,
            SOURCE = r.SOURCE,
            Edited = r.Edited,
            RevisionNumber = r.RevisionNumber,
            Report_Date = r.Report_Date,
            QuarterEndDate = r.QuarterEndDate,
            UploadedDate = r.UploadedDate,
            RunId = runId
        }).ToList();

        ProcessVendorColumn(records);
        ProcessPurchasingDocumentColumn(records);
        return records;
    }

    private static void ProcessVendorColumn(IEnumerable<FAGLL03Populated> records)
    {
        var vendorRegex = new Regex(@"\bLT\d{4}\b|VC\s?\d{7}|\bVC\s?-?\s?(?:\d{5}|\d{7})\b|\b\d{7}\b|\b\d{5}\b", RegexOptions.IgnoreCase);
        var vendorCodeRegex = new Regex(@"\bLT\d{4}\b|\d{7}|\d{5}", RegexOptions.IgnoreCase);

        foreach (var r in records)
        {
            if (!string.IsNullOrWhiteSpace(r.Vendor)) continue;
            var text = r.Invoice_Description;
            if (string.IsNullOrEmpty(text)) { r.Vendor = "Not Found"; r.Processed = "Checked [Inv_Desc]"; continue; }
            var m = vendorRegex.Match(text);
            if (!m.Success) { r.Vendor = "Not Found"; r.Processed = "Checked [Inv_Desc]"; continue; }
            var cm = vendorCodeRegex.Match(m.Value);
            if (!cm.Success) { r.Vendor = "Not Found"; r.Processed = "Checked [Inv_Desc]"; continue; }
            r.Vendor = cm.Value;
            r.Processed = "Extracted Vendor From [Inv_Desc]";
        }
    }

    private static void ProcessPurchasingDocumentColumn(IEnumerable<FAGLL03Populated> records)
    {
        var poRegex = new Regex(@"\b7\d{9}\b|\b8\d{9}\b|\b3\d{9}\b");

        foreach (var r in records)
        {
            if (!string.IsNullOrWhiteSpace(r.Purchasing_Document)) { r.LineItemType = "With PO"; continue; }

            string? found = null;
            string source = "";

            if (!string.IsNullOrEmpty(r.Document_Header))
            {
                var m = poRegex.Match(r.Document_Header);
                if (m.Success) { found = m.Value; source = "PO Extracted from [Doc_Header]"; }
            }

            if (found is null && !string.IsNullOrEmpty(r.Assignment))
            {
                var m = poRegex.Match(r.Assignment);
                if (m.Success) { found = m.Value; source = "PO Extracted from [Assignment]"; }
            }

            if (found is not null)
            {
                r.Purchasing_Document = found;
                r.LineItemType = "With PO";
                r.Processed = r.Processed == "Extracted Vendor From [Inv_Desc]"
                    ? source + " & Extracted Vendor From [Inv_Desc]"
                    : source;
            }
            else
            {
                r.Purchasing_Document = "Not Found";
                r.LineItemType = "Non PO";
                r.Processed = r.Processed switch
                {
                    "Extracted Vendor From [Inv_Desc]" => "PO not found & Extracted Vendor From [Inv_Desc]",
                    "Checked [Inv_Desc]" => "PO not found & Vendor not found",
                    _ => "PO not found",
                };
            }
        }
    }

    // ── Step 12: MSME CP fix ──────────────────────────────────────────────────

    public IEnumerable<FAGLL03NetCPFixed> MSMECreditPeriodFixEnumerable(IEnumerable<FAGLL03NetLiability> data) =>
        data.Select(r =>
        {
            var fixed_ = MapToNetCPFixed(r);
            if ((r.Industry is "1" or "2") && r.Credit_Period == "60")
            {
                fixed_.Credit_Period = "45";
                fixed_.CP_Fixed = true;
            }
            else
            {
                fixed_.CP_Fixed = false;
            }
            return fixed_;
        });

    private static FAGLL03NetCPFixed MapToNetCPFixed(FAGLL03NetLiability src)
    {
        var dst = new FAGLL03NetCPFixed();
        CopyProperties(src, dst);
        return dst;
    }

    // ── Step 12: Base Hyperion assignment ─────────────────────────────────────

    public IEnumerable<FAGLL03ProcessedResult> AssignBaseHyperionsEnumerable(IEnumerable<FAGLL03NetCPFixed> data) =>
        data.Select(r =>
        {
            var result = MapToProcessedResult(r);
            var mapping = GLHyperionMapper.ProcessGlAccount(result.GL_Account ?? string.Empty);
            result.Base_Hyperion_Code = mapping.HyperionCode;
            result.Base_Hyperion_Description = mapping.HyperionCodeDescription;
            result.Base_SAP_Amount = result.Amount_Local ?? 0m;
            return result;
        });

    private static FAGLL03ProcessedResult MapToProcessedResult(FAGLL03NetCPFixed src)
    {
        var dst = new FAGLL03ProcessedResult();
        CopyProperties(src, dst);
        return dst;
    }

    // ── Step 12: Ageing + Hyperion classification ─────────────────────────────

    public IEnumerable<FAGLL03ProcessedResult> HyperionClassificationEnumerable(
        IEnumerable<FAGLL03ProcessedResult> data, DateTime currentQuarter)
    {
        var aged = AgeingCalculationEnumerable(data, currentQuarter);

        foreach (var r in aged)
        {
            var gl = r.GL_Account ?? string.Empty;
            var mapping = GLHyperionMapper.ProcessGlAccount(gl);
            r.Billed_Status = mapping.BilledStatus;

            if (helper.IsMSMED(r))
            {
                r.Adjustment_Type = "MSME Adjustment";
                if (r.Industry == "1") { r.MSME_Type = "Micro"; }
                else if (r.Industry == "2") { r.MSME_Type = "Small"; }

                if (r.Industry is "1" or "2")
                {
                    if (r.Ageing <= 45)
                    {
                        r.Hyperion_Code = "2D170100";
                        r.Hyp_Code_Description = "Principal Amount payable to Micro & Small Enterprise with less than 45 days credit period";
                        r.MSME_Ageing = "<=45";
                    }
                    else
                    {
                        r.Hyperion_Code = "2D170200";
                        r.Hyp_Code_Description = "Principal Amt payable to Micro & Small Enterprise exceeding 45 days credit period";
                        r.MSME_Ageing = ">45";
                    }
                }
                else if (r.Industry == "3")
                {
                    r.Hyperion_Code = "2D190510";
                    r.Hyp_Code_Description = "Principal Amt payable to Medium Enterprise";
                    r.MSME_Ageing = "N/A";
                    r.MSME_Type = "Medium";
                }
            }
            else if (helper.IsCapitalRevenue(r))
            {
                r.Adjustment_Type = "Capital Adjustment";
                r.Hyperion_Code = "2D251000";
                r.Hyp_Code_Description = "Capital Revenue Adjustment";
            }
            else
            {
                r.Hyperion_Code = mapping.HyperionCode;
                r.Hyp_Code_Description = mapping.HyperionCodeDescription;
                if (string.IsNullOrEmpty(r.Adjustment_Type))
                    r.Adjustment_Type = "None";
            }

            yield return r;
        }
    }

    public IEnumerable<FAGLL03ProcessedResult> AgeingCalculationEnumerable(
        IEnumerable<FAGLL03ProcessedResult> data, DateTime currentQuarter) =>
        data.Select(r =>
        {
            var cp = 0;
            if (!string.IsNullOrEmpty(r.Credit_Period) &&
                !r.Credit_Period!.Contains("no", StringComparison.OrdinalIgnoreCase))
                int.TryParse(r.Credit_Period, out cp);

            bool isMSME = helper.IsMSMED(r);
            DateTime? baseDate = isMSME
                ? r.Document_Date ?? r.Posting_Date ?? r.Payment_Date
                : r.Posting_Date ?? r.Document_Date ?? r.Payment_Date;

            if (baseDate.HasValue)
            {
                var due = baseDate.Value.AddDays(cp);
                var days = (currentQuarter - due).Days;
                r.Ageing = days;
                r.Ageing_Years = days / 365.0m;
                r.Ageing_Group = AgeingGroupName(days, r.GL_Account ?? string.Empty);
                r.Calculated_Due_Date = due;
            }
            else
            {
                r.Ageing = 0; r.Ageing_Years = 0m; r.Ageing_Group = null; r.Calculated_Due_Date = null;
            }
            return r;
        });

    private string? AgeingGroupName(int days, string gl)
    {
        float years = days / 365f;
        var groups = helper.AgeingGroup.OrderBy(g => g.Group_Code).Select(g => g.Group_Name).ToList();
        if (helper.NotDueGL.Any(n => n.Gl_Code == gl)) return groups.ElementAtOrDefault(4);
        if (years <= 1) return groups.ElementAtOrDefault(0);
        if (years <= 2) return groups.ElementAtOrDefault(1);
        if (years <= 3) return groups.ElementAtOrDefault(2);
        return groups.ElementAtOrDefault(3);
    }

    // ── Step 12: ICP Hyperion + ERV ───────────────────────────────────────────

    public IEnumerable<FAGLL03ProcessedResult> AssignICPHyperionCodesEnumerable(IEnumerable<FAGLL03ProcessedResult> data)
    {
        var dict = helper.ICPHyperionMaps
            .GroupBy(r => r.ICP_Name)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        static string Norm(string? v) =>
            string.IsNullOrWhiteSpace(v) || v.Equals("NA", StringComparison.OrdinalIgnoreCase)
            ? "Not Mapped" : v;

        foreach (var r in data)
        {
            r.Transacton_Type = r.Amount_Doc_Adjusted >= 0 ? "Credit" : "Debit";
            if (!string.IsNullOrWhiteSpace(r.ICP_Name) && dict.TryGetValue(r.ICP_Name, out var map))
                r.ICP_Hyperion = r.Transacton_Type == "Credit" ? Norm(map.Hyperion_Credit) : Norm(map.Hyperion_Debit);
            else
                r.ICP_Hyperion = "Not Mapped";
            yield return r;
        }
    }

    public IEnumerable<FAGLL03ProcessedResult> SNAERVCalculationEnumerable(
        IEnumerable<FAGLL03ProcessedResult> data, DateTime quarterEnd)
    {
        var rates = helper.ForexMonthEndMaps
            .Where(f => f.Date == quarterEnd.Date)
            .ToDictionary(f => f.Currency, f => f.Conversion_Rate);

        foreach (var r in data)
        {
            if (r.Document_Currency is not null && rates.TryGetValue(r.Document_Currency, out decimal rate))
            {
                var amtINR = r.Amount_Doc_Adjusted * rate;
                r.Amount_Doc_Adjusted_INR = amtINR;
                r.Amount_Doc_Adjusted_ERV = r.Amount_Local_Adjusted - amtINR;
                r.Exchange_Rate = rate;
            }
            yield return r;
        }
    }

    public static IEnumerable<FAGLL03ProcessedResult> MergeICPHyperionAndAmountDocINR(
        IEnumerable<FAGLL03ProcessedResult> data)
    {
        foreach (var r in data)
        {
            if (r.IsSNACompany)
            {
                r.Hyperion_Code = r.ICP_Hyperion!;
                r.Net_Amount_INR = r.Amount_Doc_Adjusted_INR;
            }
            else
            {
                r.Net_Amount_INR = r.Amount_Local_Adjusted;
            }
            yield return r;
        }
    }

    // ── Step 11: Merge local + doc currency tables ────────────────────────────

    public static DataTable MergeTradeAndSNAData(DataTable tradeData, DataTable snaData)
    {
        const string joinKey = "Invoice_Key";
        var result = new DataTable("Merged Trade and SNA Data");

        foreach (DataColumn col in tradeData.Columns)
            result.Columns.Add(col.ColumnName, col.DataType);

        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Purchasing_Document","Vendor","Document_Date","LineItemType","Posting_Date","Payment_Date",
            "Amount_Local","Industry","Industry_Merged","Payment_Terms","Credit_Period","Processed","Edited",
            "GL_Account","GL_Description","Company_Code","Document_Type","SOURCE","ProcessId","StepIndex",
            "Vendor_Code","Vendor_Name","ICP_Name","Entity_Type","Entity_Relation","IsSNACompany",
            "Document_Number","Vendor_Description","Vendor_Merged","Document_Header","Assignment",
            "Invoice_Description","Invoice_Reference","Profit_Center","Report_Date","User_Name",
            "Document_Currency","Amount_Doc","CP_Merged","Ind_Merged","Grouped_Invoice_Key_Original",
            "Advance_Applied","Adjustment_Type","Total_Remaining_Advance","RunId",
        };

        foreach (DataColumn col in snaData.Columns)
        {
            if (col.ColumnName == joinKey || duplicates.Contains(col.ColumnName)) continue;
            var name = tradeData.Columns.Contains(col.ColumnName) ? col.ColumnName + "_SNA" : col.ColumnName;
            result.Columns.Add(name, col.DataType);
        }

        var snaIndex = snaData.AsEnumerable()
            .GroupBy(r => r.Field<object?>(joinKey))
            .Where(g => g.Key is not null)
            .ToDictionary(g => g.Key!, g => g.First());

        foreach (DataRow tr in tradeData.Rows)
        {
            var newRow = result.NewRow();
            foreach (DataColumn col in tradeData.Columns)
                newRow[col.ColumnName] = tr[col.ColumnName];

            snaIndex.TryGetValue(tr[joinKey], out DataRow? snaRow);

            foreach (DataColumn col in snaData.Columns)
            {
                if (col.ColumnName == joinKey || duplicates.Contains(col.ColumnName)) continue;
                var target = tradeData.Columns.Contains(col.ColumnName) ? col.ColumnName + "_SNA" : col.ColumnName;
                newRow[target] = snaRow is not null ? snaRow[col.ColumnName] : DBNull.Value;
            }

            result.Rows.Add(newRow);
        }

        return result;
    }

    // ── Utility methods ───────────────────────────────────────────────────────

    public static DataTable AddJoinKeysColumn(DataTable data)
    {
        if (!data.Columns.Contains("Composite_Join_Key"))
            data.Columns.Add("Composite_Join_Key", typeof(string));

        foreach (DataRow row in data.Rows)
        {
            var po = row.Table.Columns.Contains("Purchasing_Document") ? row.Field<string?>("Purchasing_Document") : null;
            var vendor = row.Table.Columns.Contains("Vendor") ? row.Field<string?>("Vendor") : null;
            var cc = row.Table.Columns.Contains("Company_Code") ? row.Field<string?>("Company_Code") : null;
            var icp = row.Table.Columns.Contains("ICP_Name") ? row.Field<string?>("ICP_Name") : null;
            var reportDate = row.Table.Columns.Contains("Report_Date") ? row.Field<DateTime?>("Report_Date") : null;

            row["Composite_Join_Key"] = $"{po}_{vendor}_{cc}_{icp}_{reportDate}";
        }
        return data;
    }

    // ── Private reflection copy helper ────────────────────────────────────────

    private static void CopyProperties(object src, object dst)
    {
        var srcProps = src.GetType().GetProperties().ToDictionary(p => p.Name);
        foreach (var dstProp in dst.GetType().GetProperties())
        {
            if (dstProp.CanWrite && srcProps.TryGetValue(dstProp.Name, out var srcProp) && srcProp.CanRead)
            {
                try { dstProp.SetValue(dst, srcProp.GetValue(src)); }
                catch { /* type mismatch between layers - skip */ }
            }
        }
    }
}
