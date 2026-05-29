using Dapper;
using DataBridge.Domain.TradePayable.Configuration;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;
using System.Data;

namespace DataBridge.Infrastructure.TradePayable;

internal sealed class MergedDataService(
    TradePayableDbContext db,
    IMasterTableRepository<ICPVendorMap> icpRepo,
    TradePayableSettings settings) : IMergedDataService
{
    public async Task<DataTable> ComputeAsync(CancellationToken ct = default)
    {
        // Fetch all four source tables in parallel.
        var step1Task = FetchStep01Async(ct);
        var podataTask = FetchPoDataAsync(ct);
        var mVendTask = FetchVendorMasterAsync(ct);
        var icpTask = icpRepo.GetAllAsync();

        await Task.WhenAll(step1Task, podataTask, mVendTask, icpTask);

        var step1Rows = await step1Task;
        var podataRows = await podataTask;
        var mVendRows = await mVendTask;
        var icpRecords = await icpTask;

        // ── Step 1 lookups (from podata) ─────────────────────────────────────────
        // PO_Vendor_Data: ebeln → {lifnr, vendor_name}
        var byEbeln = podataRows
            .Where(r => r.Ebeln != null)
            .GroupBy(r => r.Ebeln!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key,
                          g => (Lifnr: g.Max(r => r.Lifnr), Name: g.Max(r => r.Name1)),
                          StringComparer.OrdinalIgnoreCase);

        // Vendor_Only_Data: lifnr → vendor_name
        var byLifnr = podataRows
            .Where(r => r.Lifnr != null)
            .GroupBy(r => r.Lifnr!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key,
                          g => g.Max(r => r.Name1),
                          StringComparer.OrdinalIgnoreCase);

        // Name_Only_Data: vendor_name → lifnr  (for name-based LIKE matching)
        var byVendorName = byLifnr
            .Where(kv => kv.Value != null)
            .GroupBy(kv => kv.Value!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key,
                          g => g.Max(kv => kv.Key),
                          StringComparer.OrdinalIgnoreCase);

        // ── Step 2 lookup: (vendor_code, company_code) → row ────────────────────
        var vendorMaster = mVendRows
            .Where(r => r.PkcVendorCode != null && r.PkcCompanyCode != null)
            .GroupBy(r => (r.PkcVendorCode!, r.PkcCompanyCode!),
                     VendorKeyComparer.Instance)
            .ToDictionary(g => g.Key, g => g.First(),
                          VendorKeyComparer.Instance);

        // ── Reverse lookup: vendor_name → first vendor master row ───────────────
        var vendorMasterByName = mVendRows
            .Where(r => r.C_VendorName != null && r.PkcVendorCode != null)
            .GroupBy(r => r.C_VendorName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(),
                          StringComparer.OrdinalIgnoreCase);

        // ── Step 4 lookup: vendor_code → ICP entry ──────────────────────────────
        var icpByCode = icpRecords
            .Where(m => m.Vendor_Code != null)
            .GroupBy(m => m.Vendor_Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(),
                          StringComparer.OrdinalIgnoreCase);

        // ── Build output DataTable ───────────────────────────────────────────────
        var result = BuildSchema();

        foreach (var src in step1Rows)
        {
            string? po = src.Purchasing_Document;
            string? vendor = src.Vendor;
            string? vendDesc = src.Vendor_Description;
            string? industry = src.Industry;
            string? payTerms = src.Payment_Terms;
            string? company = src.Company_Code;
            string? gl = src.GL_Account;

            bool hasPO = !string.IsNullOrEmpty(po) && po != "Not Found";
            bool hasVendor = !string.IsNullOrEmpty(vendor) && vendor != "Not Found";
            bool bothNF = po == "Not Found" && vendor == "Not Found";

            // ── Step 1: PO / Vendor / Name matching ──────────────────────────────
            string? poMatchLifnr = null, poMatchName = null;
            string? vOnlyLifnr = null, vOnlyName = null;
            string? nOnlyLifnr = null, nOnlyName = null;

            if (hasPO && byEbeln.TryGetValue(po!, out var pm))
            {
                poMatchLifnr = pm.Lifnr;
                poMatchName = pm.Name;
            }
            else if (!hasPO || poMatchLifnr == null)
            {
                if (hasVendor && byLifnr.TryGetValue(vendor!, out var vn))
                {
                    vOnlyLifnr = vendor;
                    vOnlyName = vn;
                }
                else if (poMatchLifnr == null && vOnlyLifnr == null && bothNF
                         && !string.IsNullOrEmpty(vendDesc) && vendDesc != "Not Found")
                {
                    // LIKE '%vendor_name%' — scan all vendor names for a substring match
                    foreach (var kvp in byVendorName)
                    {
                        if (vendDesc.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            nOnlyName = kvp.Key;
                            nOnlyLifnr = kvp.Value;
                            break;
                        }
                    }
                }
            }

            // Merged columns follow the COALESCE chains from the view SQL
            string? mergedPO = NullIfNotFound(po);
            string? mergedVendor = poMatchLifnr ?? vOnlyLifnr ?? nOnlyLifnr ?? NullIfNotFound(vendor);
            // Merged_Vendor_Description: n_only first, then v_only, then po, then original

            List<string> vendorCheck = new List<string>()
            {
                "14701", "14702", "14002", "14001", "14021",
                "14100", "14142", "14155", "14219", "14275",
                "14393", "14477", "14503", "14507", "14550",
                "14552", "14560", "14589", "14620", "14621",
                "14622", "14624", "14677", "14684", "14703",
                "14704", "14705", "14711", "14712", "14721",
                "14753", "14777", "14779", "14794", "14810",
                "14831", "14836", "14850", "14853", "14893",
                "14898", "14982"
            };

            //if (mergedVendor != null && vendorCheck.Contains(mergedVendor))
            //{
            //    // Do something if the mergedVendor is in the vendorCheck list
            //    System.Diagnostics.Debug.WriteLine($"Merged Vendor {mergedVendor} is in the vendor check list.");
            //}

            string? mergedVendDesc = NullIfNotFound(vOnlyName ?? vendDesc ?? nOnlyName ?? poMatchName);

            // ── Step 2: Vendor CP / Industry ────────────────────────────────────
            // 2a: recover vendor code from master when vendor is null but description has a value
            if (mergedVendor == null && !string.IsNullOrWhiteSpace(mergedVendDesc)
                && vendorMasterByName.TryGetValue(mergedVendDesc!, out var vmByName))
            {
                mergedVendor = vmByName.PkcVendorCode;
            }

            string? zterm = null, industryType = null;
            if (mergedVendor != null && company != null
                && vendorMaster.TryGetValue((mergedVendor, company), out var vm))
            {
                zterm = vm.Zterm;
                industryType = vm.IndustryType;
                // 2b: fill description from master if still blank
                if (string.IsNullOrWhiteSpace(mergedVendDesc))
                    mergedVendDesc = vm.C_VendorName;
            }

            string? mergedCP = TryCastMod100(payTerms) ?? TryCastMod100(zterm);
            string? mergedIndustry = NullIfNotFound(industry) ?? industryType;

            // ── Step 3: Column cleanup (NULLIF / TRIM) ────────────────────────────
            string? finalPO = NullIfTrimEmpty(mergedPO);
            string? finalVendor = NullIfTrimEmpty(mergedVendor);
            string? finalCP = NullIfTrimEmpty(mergedCP);
            string? finalInd = NullIfTrimEmpty(mergedIndustry);

            // ── Step 4: ICP enrichment ────────────────────────────────────────────
            ICPVendorMap? icp = null;
            if (finalVendor != null && gl != "14710")
                icpByCode.TryGetValue(finalVendor, out icp);

            string vertical = company == "1000" ? "Corporate" : "Offshore";
            bool isSNA = icp?.ICP_Name != null;

            var row = result.NewRow();
            row["Invoice_Key"] = Str(src.Invoice_Key);
            row["Document_Number"] = Str(src.Document_Number);
            row["Invoice_Reference"] = Str(src.Invoice_Reference);
            row["Document_Header"] = Str(src.Document_Header);
            row["Document_Type"] = Str(src.Document_Type);
            row["Company_Code"] = Str(src.Company_Code);
            row["Assignment"] = Str(src.Assignment);
            row["Invoice_Description"] = Str(src.Invoice_Description);
            row["Amount_Local"] = Str(src.Amount_Local);
            row["Document_Currency"] = Str(src.Document_Currency);
            row["Amount_Doc"] = Str(src.Amount_Doc);
            row["GL_Account"] = Str(src.GL_Account);
            row["GL_Description"] = Str(src.GL_Description);
            row["Profit_Center"] = Str(src.Profit_Center);
            row["Document_Date"] = Str(src.Document_Date);
            row["Posting_Date"] = Str(src.Posting_Date);
            row["Payment_Date"] = Str(src.Payment_Date);
            row["Report_Date"] = Str(src.Report_Date);
            row["User_Name"] = Str(src.User_Name);
            row["SOURCE"] = Str(src.SOURCE);
            row["Edited"] = Str(src.Edited);
            row["RunId"] = Str(src.RunId);
            row["StepIndex"] = Str(src.StepIndex);
            row["Processed"] = Str(src.Processed);
            row["RevisionNumber"] = Str(src.RevisionNumber);
            row["QuarterEndDate"] = Str(src.QuarterEndDate);
            row["Purchasing_Document"] = Str(finalPO);
            row["Vendor"] = Str(finalVendor);
            row["Vendor_Description"] = Str(mergedVendDesc);
            row["Credit_Period"] = Str(finalCP);
            row["Industry"] = Str(finalInd);
            row["Vertical"] = vertical;
            row["Vendor_Code"] = Str(icp?.Vendor_Code);
            row["Vendor_Name"] = Str(icp?.Vendor_Name);
            row["ICP_Name"] = Str(icp?.ICP_Name);
            row["Entity_Type"] = Str(icp?.Entity_Type);
            row["Entity_Relation"] = Str(icp?.Entity_Relation);
            row["IsSNACompany"] = isSNA ? "True" : "False";
            result.Rows.Add(row);
        }

        return result;
    }

    // ── Data fetchers ────────────────────────────────────────────────────────────

    private async Task<IEnumerable<Step01Row>> FetchStep01Async(CancellationToken ct)
    {
        var table = settings.GetStepTable("Step_01");
        await using var conn = db.OpenDefault();
        await conn.OpenAsync(ct);
        return await conn.QueryAsync<Step01Row>($"SELECT * FROM [{table}]");
    }

    private async Task<IEnumerable<PoDataRow>> FetchPoDataAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT [ebeln] AS Ebeln, [lifnr] AS Lifnr, [name1] AS Name1
            FROM [Lnt_PO_Data].[dbo].[podata]
            """;
        await using var conn = db.OpenCrossServerVendor();
        await conn.OpenAsync(ct);
        return await conn.QueryAsync<PoDataRow>(sql);
    }

    private async Task<IEnumerable<VendorMasterRow>> FetchVendorMasterAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT [pkc_vendor_code]  AS PkcVendorCode,
                   [pkc_company_code] AS PkcCompanyCode,
                   [c_vendor_name]    AS C_VendorName,
                   [ZTERM]            AS Zterm,
                   [industry_type]    AS IndustryType
            FROM [Lnt_PO_Data].[dbo].[m_Vendor]
            """;
        await using var conn = db.OpenCrossServerVendor();
        await conn.OpenAsync(ct);
        return await conn.QueryAsync<VendorMasterRow>(sql);
    }

    // ── Schema ────────────────────────────────────────────────────────────────────

    private static DataTable BuildSchema()
    {
        var dt = new DataTable("MergedColumnsWithICP");
        string[] strCols =
        [
            "Invoice_Key", "Document_Number", "Invoice_Reference", "Document_Header",
            "Document_Type", "Company_Code", "Assignment", "Invoice_Description",
            "Amount_Local", "Document_Currency", "Amount_Doc", "GL_Account",
            "GL_Description", "Profit_Center", "Document_Date", "Posting_Date",
            "Payment_Date", "Report_Date", "User_Name", "SOURCE", "Edited",
            "RunId", "StepIndex", "Processed", "RevisionNumber", "QuarterEndDate",
            "Purchasing_Document", "Vendor", "Vendor_Description", "Credit_Period",
            "Industry", "Vertical", "Vendor_Code", "Vendor_Name",
            "ICP_Name", "Entity_Type", "Entity_Relation"
        ];
        foreach (var col in strCols)
            dt.Columns.Add(col, typeof(string));
        dt.Columns.Add("IsSNACompany", typeof(string));
        return dt;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static object Str(string? s) => s is null ? DBNull.Value : (object)s;
    private static string? TryCastMod100(string? s) =>
        int.TryParse(s, out int n) ? (n % 100).ToString() : null;
    private static string? NullIfTrimEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string? NullIfNotFound(string? s) =>
        s == "Not Found" ? null : s;

    // ── Row types for Dapper mapping ──────────────────────────────────────────────

    private sealed class Step01Row
    {
        public string? Invoice_Key { get; set; }
        public string? Document_Number { get; set; }
        public string? Purchasing_Document { get; set; }
        public string? Invoice_Reference { get; set; }
        public string? Document_Header { get; set; }
        public string? Document_Type { get; set; }
        public string? Company_Code { get; set; }
        public string? Assignment { get; set; }
        public string? Vendor { get; set; }
        public string? Vendor_Description { get; set; }
        public string? Invoice_Description { get; set; }
        public string? Industry { get; set; }
        public string? Amount_Local { get; set; }
        public string? Document_Currency { get; set; }
        public string? Amount_Doc { get; set; }
        public string? GL_Account { get; set; }
        public string? GL_Description { get; set; }
        public string? Profit_Center { get; set; }
        public string? Payment_Terms { get; set; }
        public string? Document_Date { get; set; }
        public string? Posting_Date { get; set; }
        public string? Payment_Date { get; set; }
        public string? Report_Date { get; set; }
        public string? User_Name { get; set; }
        public string? SOURCE { get; set; }
        public string? Edited { get; set; }
        public string? RunId { get; set; }
        public string? StepIndex { get; set; }
        public string? Processed { get; set; }
        public string? RevisionNumber { get; set; }
        public string? QuarterEndDate { get; set; }
    }
    private sealed class PoDataRow
    {
        public string? Ebeln { get; set; }
        public string? Lifnr { get; set; }
        public string? Name1 { get; set; }
    }
    private sealed class VendorMasterRow
    {
        public string? PkcVendorCode { get; set; }
        public string? PkcCompanyCode { get; set; }
        public string? C_VendorName { get; set; }
        public string? Zterm { get; set; }
        public string? IndustryType { get; set; }
    }


    // Comparer for (string, string) tuple keys — case-insensitive on both parts.
    private sealed class VendorKeyComparer : IEqualityComparer<(string, string)>
    {
        public static readonly VendorKeyComparer Instance = new();
        public bool Equals((string, string) x, (string, string) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2);
        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
    }
}
