using Application.Data_Cleaning;
using AutoMapper;
using Domain.Aggregates;
using Domain.Models.ProcessRun;
using System.Data;
using System.Text.RegularExpressions;

namespace Application.DataProcessor
{
    public class DataProcessor(HelperFunctions helper, IMapper mapper)
    {

        private readonly HelperFunctions _helper = helper;
        private readonly IMapper _mapper = mapper;

        //Clean and populate PO and Vendor Codes
        public IEnumerable<FAGLL03Populated> ProcessRawData(IEnumerable<FAGLL03RAWEntity> rawData)
        {
            IEnumerable<FAGLL03Populated> populatedRecords = _mapper.Map<IEnumerable<FAGLL03Populated>>(rawData);
            IEnumerable<FAGLL03Populated> vendor_processed = ProcessVendorColumn(populatedRecords);
            IEnumerable<FAGLL03Populated> poProcessed = ProcessPurchasingDocumentColumn(vendor_processed);
            return poProcessed;
        }
        private static IEnumerable<FAGLL03Populated> ProcessVendorColumn(IEnumerable<FAGLL03Populated> records)
        {
            string vendorRegexPattern = @"\bLT\d{4}\b | VC\s ?\d{7}|\bVC\s ? -?\s ? (?:\d{5}|\d{7})\b|\b\d{7}\b|\b\d{5}\b";
            Regex vendorRegex = new(vendorRegexPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            string vendorCodeRegexPattern = @"\bLT\d{4}\b|\d{7}|\d{5}";
            Regex vendorCodeRegex = new(vendorCodeRegexPattern, RegexOptions.IgnoreCase);

            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Vendor) || string.IsNullOrWhiteSpace(record.Vendor))
                {
                    var text = record.Invoice_Description;
                    if (!string.IsNullOrEmpty(text))
                    {
                        Match match = vendorRegex.Match(text);
                        if (match.Success)
                        {
                            Match vendorMatch = vendorCodeRegex.Match(match.Value);
                            if (vendorMatch.Success)
                            {
                                record.Vendor = vendorMatch.Value; // Update object property
                                record.Processed = "Extracted Vendor From [Inv_Desc]"; // Set new property
                            }
                        }
                        else
                        {
                            record.Vendor = "Not Found";
                            record.Processed = "Checked [Inv_Desc]";
                        }
                    }
                    else
                    {
                        record.Vendor = "Not Found";
                        record.Processed = "Checked [Inv_Desc]";
                    }
                }


            }
            return records;
        }
        private static IEnumerable<FAGLL03Populated> ProcessPurchasingDocumentColumn(IEnumerable<FAGLL03Populated> records)
        {
            string poRegexPattern = @"\b7\d{9}\b|\b8\d{9}\b|\b3\d{9}\b";
            Regex poRegex = new(poRegexPattern);

            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Purchasing_Document) || string.IsNullOrWhiteSpace(record.Purchasing_Document))
                {
                    string? extractedValue = null;
                    string processColumn = "";

                    if (!string.IsNullOrEmpty(record.Document_Header))
                    {
                        Match match = poRegex.Match(record.Document_Header);
                        if (match.Success)
                        {
                            extractedValue = match.Value;
                            processColumn = "PO Extracted from [Doc_Header]";
                        }
                    }

                    // If not found, try to extract from Assignment
                    if (string.IsNullOrEmpty(extractedValue))
                    {
                        if (!string.IsNullOrEmpty(record.Assignment))
                        {
                            Match match = poRegex.Match(record.Assignment);
                            if (match.Success)
                            {
                                extractedValue = match.Value;
                                processColumn = "PO Extracted from [Assignment]";
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(extractedValue))
                    {
                        record.Purchasing_Document = extractedValue; // Update property
                        record.LineItemType = "With PO";

                        if (record.Processed == "Extracted Vendor From [Inv_Desc]")
                        {
                            record.Processed = processColumn + " & Extracted Vendor From [Inv_Desc]";
                        }
                        else
                        {
                            record.Processed = processColumn;
                        }
                    }
                    else // PO Not Found
                    {
                        record.Purchasing_Document = "Not Found";
                        record.LineItemType = "Non PO";

                        // The complex logic for updating the combined 'Processed' column
                        if (record.Processed == "Extracted Vendor From [Inv_Desc]")
                        {
                            record.Processed = "PO not found & Extracted Vendor From [Inv_Desc]";
                        }
                        else if (record.Processed == "Checked [Inv_Desc]")
                        {
                            record.Processed = "PO not found & Vendor not found"; // More accurate final status
                        }
                        else
                        {
                            record.Processed = "PO not found";
                        }
                    }
                }
                else
                {
                    record.LineItemType = "With PO"; // Explicitly set LineItemType for existing POs
                }
            }
            return records;
        }
        public static IEnumerable<FAGLL03ProcessedResult> AssignCorporateLabelEnumerable(IEnumerable<FAGLL03ProcessedResult> Data)
        {
            foreach (var record in Data)
            {
                var cc = record.Company_Code;
                string c_type;
                if (cc == "1000")
                    c_type = "Corporate";
                else c_type = "Offshore";

                record.Vertical = c_type;
            }
            return Data;
        }
        public IEnumerable<FAGLL03NetCPFixed> MSMECreditPeriodFixEnumerable(IEnumerable<FAGLL03NetLiability> netLiabilitydata)
        {

            IEnumerable<FAGLL03NetCPFixed> netcpfixed = _mapper.Map<IEnumerable<FAGLL03NetCPFixed>>(netLiabilitydata);
            foreach (var record in netcpfixed)
            {
                var cp = record.Credit_Period;
                var ind = record.Industry;

                if ((ind == "1" || ind == "2") && cp == "60")
                {
                    record.Credit_Period = "45";
                    record.CP_Fixed = true;
                }
                else
                {
                    record.CP_Fixed = false;
                }
            }
            return netcpfixed;
        }
        public IEnumerable<FAGLL03ProcessedResult> AssignBaseHyperionsEnumerable(IEnumerable<FAGLL03NetCPFixed> netLiabilityData)
        {

            IEnumerable<FAGLL03ProcessedResult> processedRecords = _mapper.Map<IEnumerable<FAGLL03ProcessedResult>>(netLiabilityData);

            foreach (var record in processedRecords)
            {
                var mapping = GLHyperionMapper.ProcessGlAccount(record.GL_Account!);
                record.Base_Hyperion_Code = mapping.HyperionCode;
                record.Base_Hyperion_Description = mapping.HyperionCodeDescription;
                record.Base_SAP_Amount = (decimal)record.Amount_Local!;
            }
            return processedRecords;
        }
        public IEnumerable<FAGLL03ProcessedResult> AgeingCalculationEnumerable(IEnumerable<FAGLL03ProcessedResult> processedData, DateTime currentQuarter)
        {
            // Use LINQ Select to iterate and perform the necessary updates on each item.
            return processedData.Select(record =>
            {
                var glCode = record.GL_Account ?? string.Empty;

                // --- 1. Determine MSME Status ---
                bool isMSME = _helper.IsMSMED(record);

                // --- 2. Determine Credit Period (cp) ---
                int cp = 0;
                var creditPeriodStr = record.Credit_Period;

                if (!string.IsNullOrEmpty(creditPeriodStr) && !creditPeriodStr.Contains("no", StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(creditPeriodStr, out cp);
                }


                // --- 3. Determine Base Date based on priority ---
                // MSME Priority: Document Date > Posting Date > Payment Date
                // Non-MSME Priority: Posting Date > Document Date > Payment Date
                DateTime? baseDate = isMSME
                    ? record.Document_Date ?? record.Posting_Date ?? record.Payment_Date
                    : record.Posting_Date ?? record.Document_Date ?? record.Payment_Date;

                // --- 4. Calculate Ageing and update the record properties ---
                if (baseDate.HasValue)
                {
                    DateTime dueDate = baseDate.Value.AddDays(cp);
                    TimeSpan ageing = currentQuarter - dueDate;
                    int ageingDays = ageing.Days;
                    decimal ageingYears = ageingDays / 365.0m;

                    // Ageing Group Classification
                    string ageingGroup = AgeingGroupClassificationEnumerable(ageingDays, glCode)!;

                    // Populate the entity's output fields
                    record.Ageing = ageingDays;
                    record.Ageing_Years = ageingYears;
                    record.Ageing_Group = ageingGroup;
                    record.Calculated_Due_Date = dueDate;
                }
                else
                {
                    // Case where all dates are null
                    record.Ageing = 0;
                    record.Ageing_Years = 0.0m;
                    record.Ageing_Group = null;
                    record.Calculated_Due_Date = null;
                }

                return record;
            });
        }
        private string? AgeingGroupClassificationEnumerable(int ageingDays, string glCode)
        {
            float ageingYears = ageingDays / 365;
            List<string> NotDueGls = [.. _helper.NotDueGL.Select(x => x.Gl_Code)];
            List<string> AgeingGroup = [.. _helper.AgeingGroup.OrderBy(x => x.Group_Code).Select(x => x.Group_Name)];

            if (NotDueGls.Contains(glCode))
                return AgeingGroup[4];
            else if (ageingYears <= 1)
                return AgeingGroup[0];
            else if (ageingYears > 1 && ageingYears <= 2)
                return AgeingGroup[1];
            else if (ageingYears > 2 && ageingYears <= 3)
                return AgeingGroup[2];
            else if (ageingYears > 3)
                return AgeingGroup[3];
            return null;
        }
        public IEnumerable<FAGLL03ProcessedResult> HyperionClassificationEnumerable(IEnumerable<FAGLL03ProcessedResult> processedData, DateTime currentQuarter)
        {
            var agedData = AgeingCalculationEnumerable(processedData, currentQuarter);
            foreach (var record in agedData)
            {
                var GL = record.GL_Account;
                if (string.IsNullOrEmpty(GL)) throw new Exception("GL Account is null");

                GLHyperionMapper.GLAccountMapping mapping = GLHyperionMapper.ProcessGlAccount(GL);
                string hyp_code = mapping.HyperionCode;
                string hyp_code_desc = mapping.HyperionCodeDescription;
                string billed = mapping.BilledStatus;
                record.Billed_Status = billed;
                bool isMSME = _helper.IsMSMED(record);
                var Industry = record.Industry;


                //MSME
                if (isMSME)
                {
                    if (Industry == "1" || Industry == "2")
                    {
                        if (Industry == "1")
                            record.MSME_Type = "Micro";
                        else
                            record.MSME_Type = "Small";
                        record.Adjustment_Type = "MSME Adjustment";

                        if (record.Ageing <= 45)
                        {
                            record.Hyperion_Code = "2D170100";
                            record.Hyp_Code_Description = "Principal Amount payable to Micro & Small Enterprise with less than 45 days credit period";
                            record.MSME_Ageing = "<=45";
                        }

                        else if (record.Ageing > 45)
                        {
                            record.Hyperion_Code = "2D170200";
                            record.Hyp_Code_Description = "Principal Amt payable to Micro & Small Enterprise exceeding 45 days credit period";
                            record.MSME_Ageing = ">45";
                        }
                    }
                    else if (Industry == "3")
                    {
                        record.Adjustment_Type = "MSME Adjustment";
                        record.Hyperion_Code = "2D190510";
                        record.Hyp_Code_Description = "Principal Amt payable to Medium Enterprise";
                        record.MSME_Ageing = "N/A";
                        record.MSME_Type = "Medium";
                    }
                }
                //Capital Revenue
                else if (_helper.IsCapitalRevenue(record))
                {
                    record.Adjustment_Type = "Capital Adjustment";
                    record.Hyperion_Code = "2D251000";
                    record.Hyp_Code_Description = "Capital Revenue Adjustment";
                }
                else
                {
                    record.Hyperion_Code = hyp_code;
                    record.Hyp_Code_Description = hyp_code_desc;
                    if (string.IsNullOrEmpty(record.Adjustment_Type))
                        record.Adjustment_Type = "None";
                }
            }

            return agedData;
        }
        public static DataTable MergeTradeAndSNAData(DataTable TradeData, DataTable SNAData)
        {
            string joinKey = "Invoice_Key";
            var resultTable = new DataTable("Merged Trade and SNA Data");

            foreach (DataColumn col in TradeData.Columns)
            {
                resultTable.Columns.Add(col.ColumnName, col.DataType);
            }

            List<string> potentialDuplicateBaseNames =
            [   "Purchasing_Document", "Vendor", "Document_Date", "LineItemType", "Posting_Date", "Payment_Date", "Amount_Local", "Industry",
                "Industry_Merged", "Payment_Terms","Credit_Period","Processed","Edited","GL_Account","GL_Description","Company_Code",
                "Document_Type",  "SOURCE", "ProcessId", "StepIndex", "Vendor_Code", "Vendor_Name", "ICP_Name", "Entity_Type", "Entity_Relation","IsSNACompany",
                "Document_Number","V_Credit_Period","V_Industry","Vendor_Description","Vendor_Merged","Document_Header","Assignment",
                "Invoice_Description","Invoice_Reference","lifnr","Profit_Center","c_vendor_name","Report_Date","Line_Item_Type","User_Name","Document_Currency",
                "Amount_Doc","CP_Merged","Ind_Merged","Company_Type","Advance_22006","Advance_23051","Advance_23057","Advance_23059",
                "Advance_23141","Advance_23054","Advance_22072","Advance_22113","Advance_23021","Advance_22071","Advance_22075",
                "Advance_TotalAdvanceAmount","Advance_Applied","Adjustment_Type","Total_Remaining_Advance","Grouped_Invoice_Key_Original"
              ];

            // Add SNAData columns, checking for potential name conflicts (only for columns NOT named 'Invoice_Key')
            foreach (DataColumn col in SNAData.Columns)
            {
                if (col.ColumnName != joinKey && !potentialDuplicateBaseNames.Contains(col.ColumnName))
                {
                    // Append a suffix if the column name already exists in TradeData
                    string newColumnName = TradeData.Columns.Contains(col.ColumnName)
                                         ? col.ColumnName + "_SNA"
                                         : col.ColumnName;

                    resultTable.Columns.Add(newColumnName, col.DataType);
                }
            }

            // 2. Perform the Left Join using LINQ
            var query = from tradeRow in TradeData.AsEnumerable()
                        join snaRowSet in SNAData.AsEnumerable()
                        on tradeRow.Field<object>(joinKey) equals snaRowSet.Field<object>(joinKey) into joinedSet
                        from snaRow in joinedSet.DefaultIfEmpty() // Use DefaultIfEmpty() for Left Join functionality
                        select new { tradeRow, snaRow };

            // 3. Populate the result DataTable
            foreach (var rowPair in query)
            {
                DataRow newRow = resultTable.NewRow();
                foreach (DataColumn col in TradeData.Columns)
                    newRow[col.ColumnName] = rowPair.tradeRow[col.ColumnName];

                // Copy all columns from SNAData (Right Table)
                if (rowPair.snaRow != null)
                {
                    foreach (DataColumn col in SNAData.Columns)
                    {
                        // Handle the join key column separately or use the potentially renamed column name
                        string targetColumnName = col.ColumnName;
                        if (col.ColumnName != joinKey && !potentialDuplicateBaseNames.Contains(col.ColumnName))
                        {
                            // Check if the original name was renamed
                            if (TradeData.Columns.Contains(col.ColumnName))
                            {
                                targetColumnName = col.ColumnName + "_SNA";
                            }
                            newRow[targetColumnName] = rowPair.snaRow[col.ColumnName];
                        }
                    }
                }
                else
                {
                    // If there's no match (null snaRow), set corresponding SNA columns to DBNull
                    foreach (DataColumn col in SNAData.Columns)
                    {
                        if (col.ColumnName != joinKey && !potentialDuplicateBaseNames.Contains(col.ColumnName))
                        {
                            string targetColumnName = col.ColumnName;
                            if (TradeData.Columns.Contains(col.ColumnName))
                            {
                                targetColumnName = col.ColumnName + "_SNA";
                            }
                            newRow[targetColumnName] = DBNull.Value;
                        }
                    }
                }

                resultTable.Rows.Add(newRow);
            }
            return resultTable;
        }
        public IEnumerable<FAGLL03ProcessedResult> AssignICPHyperionCodesEnumerable(IEnumerable<FAGLL03ProcessedResult> data)
        {
            // Pre-process dictionary: use StringComparer.OrdinalIgnoreCase for safer lookups
            var icpToHyperionDict = _helper.ICPHyperionMaps
                .GroupBy(r => r.ICP_Name)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Local function for standardizing "Not Mapped" logic
            static string Normalize(string? val) =>
                string.IsNullOrWhiteSpace(val) || val.Equals("NA", StringComparison.OrdinalIgnoreCase) || val.Equals("N/A", StringComparison.OrdinalIgnoreCase)
                ? "Not Mapped"
                : val;

            foreach (var row in data)
            {
                decimal amount = row.Amount_Doc_Adjusted;
                row.Transacton_Type = (amount >= 0) ? "Credit" : "Debit";
                string? icpName = row.ICP_Name;

                // 3. Perform Lookup
                if (!string.IsNullOrWhiteSpace(icpName) && icpToHyperionDict.TryGetValue(icpName, out var mapRow))
                {
                    // Case: ICP exists in map
                    row.ICP_Hyperion = (row.Transacton_Type == "Credit")
                        ? Normalize(mapRow.Hyperion_Credit)
                        : Normalize(mapRow.Hyperion_Debit);
                }
                else if (!string.IsNullOrWhiteSpace(icpName))
                {
                    // Case: ICP Name exists in your data but NOT in the Hyperion Map table
                    row.ICP_Hyperion = "Not Mapped";
                }
                else
                {
                    row.ICP_Hyperion = "Not Mapped";
                }
            }

            return data;
        }
        public IEnumerable<FAGLL03ProcessedResult> SNAERVCalculationEnumerable(IEnumerable<FAGLL03ProcessedResult> data, DateTime lastDayofquarter)
        {
            var forexmap = _helper.ForexMonthEndMaps;
            var forexRates = forexmap.AsEnumerable()
            .Where(r => r.Date == lastDayofquarter.Date)
            .ToDictionary(
                row => row.Currency,
                row => row.Conversion_Rate
            );

            foreach (var row in data)
            {
                var currency = row.Document_Currency;
                var ICP_Code = row.ICP_Name;
                decimal amountDocAdjusted = 0m;
                decimal amountLocalAdjusted = 0m;

                amountDocAdjusted = row.Amount_Doc_Adjusted;
                amountLocalAdjusted = row.Amount_Local_Adjusted;

                if (currency != null && forexRates.TryGetValue(currency, out decimal conversionRate))
                {
                    decimal amountINR = amountDocAdjusted * conversionRate;
                    row.Amount_Doc_Adjusted_INR = amountINR;
                    decimal erv = amountLocalAdjusted - amountINR;
                    row.Amount_Doc_Adjusted_ERV = erv;
                    row.Exchange_Rate = conversionRate;
                }
            }

            return data;
        }
        public static IEnumerable<FAGLL03ProcessedResult> MergeICPHyperionAndAmountDocINR(IEnumerable<FAGLL03ProcessedResult> data)
        {
            foreach (var record in data)
            {
                if (record.IsSNACompany)
                {
                    record.Hyperion_Code = record.ICP_Hyperion!;
                    record.Net_Amount_INR = record.Amount_Doc_Adjusted_INR;
                }
                else
                {
                    record.Net_Amount_INR = record.Amount_Local_Adjusted;
                }
            }
            return data;
        }
        public class RunParameters
        {
            public required Guid ProcessId { get; set; }
            public required DateTime QuarterDate { get; set; }
            public required TimeSpan ProcessDuration { get; set; }
            public string ProcessName { get; set; } = "Trade Payables Quarterly Run";
            public string ProcessDescription { get; set; } = null!;
        }

        public static ProcessRun GenerateProcessSummary(
                IEnumerable<FAGLL03ProcessedResult> resultsData,
                IEnumerable<FAGLL03ProcessedGITLocal> gitResultsData,
                RunParameters parameters,
                ProcessState state)
        {
            if (resultsData == null || !resultsData.Any())
                throw new ArgumentException("Empty results data, please try again!");

            IEnumerable<FAGLL03ProcessedResult> dataList = [.. resultsData];
            List<string> ListofRevisions = ["R01", "R02", "R03"];
            List<ProcessResultSummary> resultSummaries = [];

            foreach (var rev in ListofRevisions)
            {
                var originalSAPAmountLocal = dataList
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Amount_Local);

                var totalAdvanceAdjustedLocal = dataList
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Advance_Applied);
                var netLiabilityAmountLocal = dataList
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Amount_Local_Adjusted);
                var hyperionResults = dataList
                    .Where(r => r.RevisionNumber == rev && r.Hyperion_Code != null)
                    .GroupBy(r => r.Hyperion_Code)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount_Local_Adjusted));

                var msmeResults = new MSMEResults
                {
                    Hyperion_2D170100_Net_Balance = hyperionResults.GetValueOrDefault("2D170100", 0M),
                    Hyperion_2D170200_Net_Balance = hyperionResults.GetValueOrDefault("2D170200", 0M),
                    Hyperion_2D190510_Net_Balance = hyperionResults.GetValueOrDefault("2D190510", 0M)
                };
                var capitalRevenueResults = new CapitalRevenueResults
                {
                    Hyperion_2D190300_Net_Balance = hyperionResults.GetValueOrDefault("2D190300", 0M)
                };
                var snaData = dataList.Where(r => r.RevisionNumber == rev && r.IsSNACompany);
                var snaResults = new SNACompanyResults
                {
                    Original_SAP_Amount_Local = snaData.Sum(r => r.Amount_Local) ?? 0m,
                    Advance_Adjusted_Local = snaData.Sum(r => r.Advance_Applied),
                    Net_Balance_Local = snaData.Sum(r => r.Amount_Local_Adjusted),
                    Net_Balance_Doc_INR = snaData.Sum(r => r.Amount_Doc_Adjusted_INR),
                    Net_ERV = snaData.Sum(r => r.Amount_Doc_Adjusted_ERV)
                };
                var dataListGIT = gitResultsData?.ToList() ?? new List<FAGLL03ProcessedGITLocal>();
                var gitResults = new GITAdvanceAdjustmentResults
                {
                    Total_Adjusted_Amount_Local = dataListGIT
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Total_Adjustment),

                    Total_SAP_Amount_Local = dataListGIT
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Amount_Local),

                    Total_Net_Balance = dataListGIT
                    .Where(r => r.RevisionNumber == rev)
                    .Sum(r => r.Balance_Local)
                };
                var newRevisionSummary = new ProcessResultSummary
                {
                    RevisionNumber = rev,
                    Original_SAP_AmountLocal = (decimal)originalSAPAmountLocal!,
                    TotalAdvanceAdjustedLocal = totalAdvanceAdjustedLocal,
                    NetLiabilityAmountLocal = netLiabilityAmountLocal,
                    MSMEResults = msmeResults,
                    CapitalRevenueResults = capitalRevenueResults,
                    GITAdvanceAdjustmentResults = gitResults,
                    SNACompanyResults = snaResults,
                };
                resultSummaries.Add(newRevisionSummary);
            }
            return new ProcessRun
            {
                ProcessId = parameters.ProcessId,
                QuarterDate = parameters.QuarterDate.Date,
                RunDateTime = DateTime.Now,
                ProcessName = parameters.ProcessName,
                ProcessDescription = parameters.ProcessDescription,
                ProcessDuration = parameters.ProcessDuration,
                ProcessStatus = ProcessStatus.RunCompleted,
                RunStartedBy = state.UserPSNO, //TODO: get signed in users PS NO
                RevisionSummaries = resultSummaries, // Initialize with the new result
                CurrentRevisionNumber = "", // Set the current revision ID (e.g., 1, )
                ReportDate = state.ReportDate,
                RevisionNumber = state.RevisionNumber, // This is the string "R01", "R02", etc.
            };
        }

        public static DataTable AddJoinKeysColumn(DataTable data)
        {


            if (!data.Columns.Contains("Composite_Join_Key"))
                data.Columns.Add("Composite_Join_Key", typeof(string));

            foreach (DataRow row in data.Rows)
            {
                var po = row.Field<string?>("Purchasing_Document");
                var vendor = row.Field<string?>("Vendor");
                var cc = row.Field<string?>("Company_Code");
                var report_date = row.Field<DateTime>("Report_Date");
                var icp_name = row.Field<string>("ICP_Name");
                var underscore = "_";

                var composite_key = string.Concat(
                    [po,underscore,
                        vendor,underscore,
                        cc,underscore,
                        icp_name, underscore,
                        //gl,underscore,
                        //amount_local, underscore,
                        //doccurr, underscore,
                        //amount_doc, underscore,
                        report_date
                    ]);

                row["Composite_Join_Key"] = composite_key;
            }

            return data;
        }
    }
}
