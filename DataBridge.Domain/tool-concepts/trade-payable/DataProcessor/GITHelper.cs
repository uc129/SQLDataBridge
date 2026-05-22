using Shared.Extensions;
using System.Data;

namespace Application.DataProcessor
{
    public class GITHelper(HelperFunctions helper)
    {
        private readonly HelperFunctions _helper = helper;

        public static List<DataTable> GroupFAGLL03DataWithoutProfitCenter(DataTable processedData)
        {
            DataTable newTable = HelperFunctions.DeepCopyDataTable(processedData, "Processed Data with grouping keys");
            if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
                newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

            var groupedDataForProcessing = newTable.AsEnumerable()
                .GroupBy(row => new
                {
                    PurchasingDocument = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    CompanyCode = row.Field<string>("Company_Code"),
                    GLAccount = row.Field<string>("GL_Account"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    ICP_Name = row.Field<string?>("ICP_Name"),
                    ISSNA = row.Field<bool?>("IsSNACompany") ?? false,
                })
                .ToList();

            DataTable aggregatedTable = new("Grouped FAGLL03 Table w/o PC");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
            aggregatedTable.Columns.Add("Purchasing_Document", typeof(string));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Company_Code", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("Profit_Center", typeof(string));
            aggregatedTable.Columns.Add("Amount_Local", typeof(decimal)); // The aggregated sum
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Industry", typeof(string));
            aggregatedTable.Columns.Add("Credit_Period", typeof(string));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("ICP_Name", typeof(string));
            aggregatedTable.Columns.Add("IsSNACompany", typeof(bool));

            foreach (var group in groupedDataForProcessing)
            {
                Guid group_guid = Guid.NewGuid();
                foreach (DataRow row in group)
                    row["Grouped_Invoice_Key_Original"] = group_guid; // grouping keys for each row in the group in original table

                decimal totalAmountLocal = group.Sum(static r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Local"];
                    if (amountObj != DBNull.Value && amountObj != null)
                        decimal.TryParse(amountObj.ToString(), out value);
                    return value;
                });

                aggregatedTable.Rows.Add(
                    group_guid,
                    group.Key.PurchasingDocument,
                    group.Key.Vendor,
                    group.Key.CompanyCode,
                    group.Key.GLAccount,
                    group.First().Field<string>("Profit_Center"),
                    totalAmountLocal,
                    group.First().Field<string>("Vendor_Description"),
                    group.First().Field<string>("GL_Description"),
                    group.First().Field<string>("Industry"),
                    group.First().Field<string>("Credit_Period"),
                    group.Key.RevisionNumber,
                    group.Key.Report_Date,
                    group.Key.ICP_Name,
                    group.Key.ISSNA
                );
            }
            return [newTable, aggregatedTable];
        }
        public static List<DataTable> GroupFAGLL03DataWithProfitCenter(DataTable processedData)
        {
            DataTable newTable = HelperFunctions.DeepCopyDataTable(processedData, "Processed data with Grouping Keys with PC");
            if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
                newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

            var groupedDataForProcessing = newTable.AsEnumerable()
                .GroupBy(row => new
                {
                    PurchasingDocument = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    CompanyCode = row.Field<string>("Company_Code"),
                    GLAccount = row.Field<string>("GL_Account"),
                    Profit_Center = row.Field<string>("Profit_Center"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    ICP_Name = row.Field<string?>("ICP_Name"),
                    ISSNA = row.Field<bool?>("IsSNACompany") ?? false,
                })
                .ToList();

            DataTable aggregatedTable = new("Grouped FAGLL03 Table with PC");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
            aggregatedTable.Columns.Add("Purchasing_Document", typeof(string));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Company_Code", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("Profit_Center", typeof(string));
            aggregatedTable.Columns.Add("Amount_Local", typeof(decimal)); // The aggregated sum
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Industry", typeof(string));
            aggregatedTable.Columns.Add("Credit_Period", typeof(string));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("ICP_Name", typeof(string));
            aggregatedTable.Columns.Add("IsSNACompany", typeof(bool));

            foreach (var group in groupedDataForProcessing)
            {
                Guid group_guid = Guid.NewGuid();
                foreach (DataRow row in group)
                    row["Grouped_Invoice_Key_Original"] = group_guid;

                decimal totalAmountLocal = group.Sum(r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Local"];
                    if (amountObj != DBNull.Value && amountObj != null)
                        decimal.TryParse(amountObj.ToString(), out value);
                    return value;
                });

                aggregatedTable.Rows.Add(
                    group_guid,
                    group.Key.PurchasingDocument,
                    group.Key.Vendor,
                    group.Key.CompanyCode,
                    group.Key.GLAccount,
                    group.Key.Profit_Center, // Use group.Key since it's now part of the group key
                    totalAmountLocal,
                    group.First().Field<string>("Vendor_Description"),
                    group.First().Field<string>("GL_Description"),
                    group.First().Field<string>("Industry"),
                    group.First().Field<string>("Credit_Period"),
                    group.Key.RevisionNumber,
                    group.Key.Report_Date,
                    group.Key.ICP_Name,
                    group.Key.ISSNA
                );
            }

            // 5. Return the newly created aggregated table.
            return [newTable, aggregatedTable];
        }
        public static List<DataTable> GroupFAGLL03DataForSNA(DataTable snadata)
        {
            DataTable newTable = HelperFunctions.DeepCopyDataTable(snadata, "SNA data with Grouping Keys");
            if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original"))
                newTable.Columns.Add("Grouped_Invoice_Key_Original", typeof(Guid));

            var groupedDataForProcessing = newTable.AsEnumerable()
                .GroupBy(row => new
                {
                    Vendor = row.Field<string>("Vendor"),
                    Vertical = row.Field<string>("Vertical"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    GLAccount = row.Field<string>("GL_Account"),
                    Company_Code = row.Field<string>("Company_Code"),
                })
                .ToList();

            DataTable aggregatedTable = new("Grouped SNA Data");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key", typeof(Guid));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Amount_Local", typeof(decimal)); // The aggregated sum
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("Vertical", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Company_Code", typeof(string));

            aggregatedTable.Columns.Add("ICP_Name", typeof(string));
            aggregatedTable.Columns.Add("IsSNACompany", typeof(bool));
            aggregatedTable.Columns.Add("Purchasing_Document", typeof(string));
            aggregatedTable.Columns.Add("Profit_Center", typeof(string));
            aggregatedTable.Columns.Add("Industry", typeof(string));
            aggregatedTable.Columns.Add("Credit_Period", typeof(string));


            foreach (var group in groupedDataForProcessing)
            {
                Guid group_guid = Guid.NewGuid();
                foreach (DataRow row in group)
                    row["Grouped_Invoice_Key_Original"] = group_guid;

                decimal totalAmountLocal = group.Sum(r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Local"];
                    if (amountObj != DBNull.Value && amountObj != null)
                        decimal.TryParse(amountObj.ToString(), out value);
                    return value;
                });

                aggregatedTable.Rows.Add(
                    group_guid,
                    group.Key.Vendor,
                    totalAmountLocal,
                    group.First().Field<string>("Vendor_Description"),
                    group.Key.RevisionNumber,
                    group.Key.Report_Date,
                    group.Key.Vertical,
                    group.Key.GLAccount,
                    group.First().Field<string>("GL_Description"),
                    group.Key.Company_Code

                //group.Key.PurchasingDocument,
                //group.Key.Profit_Center,
                //group.Key.ICP_Name,
                //group.Key.ISSNA
                //group.First().Field<string>("Industry"),
                //group.First().Field<string>("Credit_Period"),
                );
            }

            // 5. Return the newly created aggregated table.
            return [newTable, aggregatedTable];
        }


        public DataTable PivotLiabilityGLDataWithoutProfitCenter(DataTable liabilityGLData)
        {
            DataTable pivotedLiabilityTable = new("Pivoted Liability GLs w/o PC");
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));
            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));

            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));
            pivotedLiabilityTable.Columns.Add("IsSNACompany", typeof(bool));


            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add(gl.GL_Code, typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
            }


            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Company_Code = row.Field<string>("Company_Code"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    RevisionNumber = row.Field<string>("RevisionNumber")
                    //ICP_Name = row.Field<string>("ICP_Name"),
                    //IsSNACompany = row.Field<bool>("IsSNACompany"),
                });

            foreach (var group in groupedData)
            {
                DataRow newRow = pivotedLiabilityTable.NewRow();
                newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                newRow["Vendor"] = group.Key.Vendor;
                newRow["Company_Code"] = group.Key.Company_Code;
                newRow["Profit_Center"] = group.First().Field<string>("Profit_Center");
                newRow["Report_Date"] = group.Key.Report_Date;
                newRow["RevisionNumber"] = group.Key.RevisionNumber;
                //newRow["ICP_Name"] = group.Key.ICP_Name;
                //newRow["IsSNACompany"] = group.Key.IsSNACompany;

                foreach (var row in group)
                {
                    var glAccount = row.Field<string>("GL_Account");
                    decimal amountLocal = row.Field<decimal>("Amount_Local");
                    newRow[glAccount!] = amountLocal;
                    newRow[$"Grouped_Key_{glAccount}"] = row.Field<Guid>("Grouped_Invoice_Key");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }
        public DataTable PivotLiabilityGLDataWithProfitCenter(DataTable liabilityGLData)
        {
            DataTable pivotedLiabilityTable = new("Pivoted Liability GLs with PC");
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));
            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));
            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));
            pivotedLiabilityTable.Columns.Add("IsSNACompany", typeof(bool));

            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add(gl.GL_Code, typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
            }

            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Company_Code = row.Field<string>("Company_Code"),
                    Profit_Center = row.Field<string>("Profit_Center"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    RevisionNumber = row.Field<string>("RevisionNumber")
                    //ICP_Name = row.Field<string>("ICP_Name"),
                    //ISSNA = row.Field<bool>("IsSNACompany"),
                });

            foreach (var group in groupedData)
            {
                DataRow newRow = pivotedLiabilityTable.NewRow();
                newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                newRow["Vendor"] = group.Key.Vendor;
                newRow["Company_Code"] = group.Key.Company_Code;
                newRow["Profit_Center"] = group.Key.Profit_Center;
                newRow["Report_Date"] = group.Key.Report_Date;
                newRow["RevisionNumber"] = group.Key.RevisionNumber;
                //newRow["ICP_Name"] = group.Key.ICP_Name;
                //newRow["IsSNACompany"] = group.Key.ISSNA;

                foreach (var row in group)
                {
                    var glAccount = row.Field<string>("GL_Account");
                    decimal amountLocal = row.Field<decimal>("Amount_Local");
                    newRow[glAccount!] = amountLocal;
                    newRow[$"Grouped_Key_{glAccount}"] = row.Field<Guid>("Grouped_Invoice_Key");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }
        public DataTable PivotFAGLL03SNAData(DataTable liabilityGLData)
        {
            DataTable pivotedLiabilityTable = new("Pivoted Liability SNA Data");
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vertical", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));

            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));
            pivotedLiabilityTable.Columns.Add("IsSNACompany", typeof(bool));


            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add(gl.GL_Code, typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}", typeof(Guid));
            }


            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    //Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Vertical = row.Field<string>("Vertical"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    Company_Code = row.Field<string>("Company_Code"),
                    //ICP_Name = row.Field<string>("ICP_Name"),
                    //IsSNACompany = row.Field<bool>("IsSNACompany"),
                });

            foreach (var group in groupedData)
            {
                DataRow newRow = pivotedLiabilityTable.NewRow();

                newRow["Vendor"] = group.Key.Vendor;
                newRow["Vertical"] = group.Key.Vertical;
                newRow["RevisionNumber"] = group.Key.RevisionNumber;
                newRow["Report_Date"] = group.Key.Report_Date;
                newRow["Company_Code"] = group.Key.Company_Code;

                //newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                //newRow["Profit_Center"] = group.First().Field<string>("Profit_Center");
                //newRow["ICP_Name"] = group.Key.ICP_Name;
                //newRow["IsSNACompany"] = group.Key.IsSNACompany;

                foreach (var row in group)
                {
                    var glAccount = row.Field<string>("GL_Account");
                    decimal amountLocal = row.Field<decimal>("Amount_Local");
                    newRow[glAccount!] = amountLocal;
                    newRow[$"Grouped_Key_{glAccount}"] = row.Field<Guid>("Grouped_Invoice_Key");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }


        private class GlAmountData
        {
            // Changed from property to field to be concise and mutable
            public decimal Amount;
            public Guid GuidKey;

            // Constructor for easy initialization
            public GlAmountData(decimal amount, Guid guidKey)
            {
                Amount = amount;
                GuidKey = guidKey;
            }
        }
        public DataTable PerformCascadedJoinWithoutProfitCenter(DataTable filteredAdvanceGLDataTable, DataTable pivotedLiabilityTable)
        {
            DataTable resultTable = filteredAdvanceGLDataTable.Clone();
            resultTable.TableName = "Raw FAGLL03 GIT Sheet Without Profit Center";

            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add(glColumn.GL_Code, typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type", typeof(string));

            pivotedLiabilityTable.ReplaceNullsWithZero();

            var mutableLiabilityDict = pivotedLiabilityTable.AsEnumerable()
                .ToDictionary(
                    r => new {
                        Purchasing_Document = r.Field<string>("Purchasing_Document"),
                        Vendor = r.Field<string>("Vendor"),
                        Company_Code = r.Field<string>("Company_Code"),
                        Report_Date = r.Field<DateTime>("Report_Date"),
                        RevisionNumber = r.Field<string>("RevisionNumber")
                        //ICP_Name = r.Field<string>("ICP_Name"),
                        //ISSNA = r.Field<bool>("IsSNACompany"),
                    },
                    r => _helper.LiabilityGLs.ToDictionary(
                            gl => gl.GL_Code,
                            gl => {
                                decimal? nullableValue = r.Field<decimal?>(gl.GL_Code);
                                decimal value = nullableValue ?? 0m;
                                Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}");
                                Guid guid = nullableGuid ?? Guid.Empty;
                                decimal returnAmount = value > 0 ? 0m : value;
                                return new GlAmountData(returnAmount, guid);
                            }
                        )
                );
            // MODIFICATION END

            // 2. Iterate through the *original* filteredAdvanceGLDataTable
            foreach (DataRow advRow in filteredAdvanceGLDataTable.Rows)
            {
                var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var vendor = advRow.Field<string>("Vendor");
                var companyCode = advRow.Field<string>("Company_Code");
                DateTime reportDate = advRow.Field<DateTime>("Report_Date");
                var revno = advRow.Field<string>("RevisionNumber");
                //var icp_name = advRow.Field<string>("ICP_Name");
                //var issna = advRow.Field<bool>("IsSNACompany");

                var fullCompositeKey = new
                {
                    Purchasing_Document = purchasingDoc,
                    Vendor = vendor,
                    Company_Code = companyCode,
                    Report_Date = reportDate,
                    RevisionNumber = revno
                    //ICP_Name = icp_name,
                    //ISSNA = issna
                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Local");
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[glColumn.GL_Code].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}"] = matchedGLs[glColumn.GL_Code].GuidKey;

                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;
                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                consumedAmount = -Math.Abs(advanceGLAmount);
                                decimal remainingAmount = liabilityGLValue - consumedAmount; // e.g., -1200 - (-1000) = -200
                                matchedGLs[glColumn.GL_Code].Amount = remainingAmount;
                                newResultRow[glColumn.GL_Code] = consumedAmount; // e.g., -1000
                            }
                            else 
                            {
                                consumedAmount = liabilityGLValue; // e.g., -1200 if Advance GL was 2000
                                matchedGLs[glColumn.GL_Code].Amount = 0;
                                newResultRow[glColumn.GL_Code] = consumedAmount;
                            }
                        }
                        else
                        {
                            newResultRow[glColumn.GL_Code] = 0m;
                        }
                    }
                    if (matchedAndProcessed)
                    {
                        resultTable.Rows.Add(newResultRow);
                    }
                    else
                    {
                        newResultRow["Join_Type"] = "No Match";
                        resultTable.Rows.Add(newResultRow);
                    }

                    if (matchedGLs.Values.All(v => v.Amount == 0))
                    {
                        mutableLiabilityDict.Remove(fullCompositeKey);
                    }
                }
                else
                {
                    DataRow noMatchRow = resultTable.NewRow();
                    noMatchRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    noMatchRow["Join_Type"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }
            return resultTable;
        }
        public DataTable PerformCascadedJoinWithProfitCenter(DataTable filteredAdvanceGLDataTable, DataTable pivotedLiabilityTable)
        {
            // 1. Initialize resultTable by cloning the structure of the left table.
            DataTable resultTable = filteredAdvanceGLDataTable.Clone();
            resultTable.TableName = "Raw FAGLL03 GIT Sheet With profit Center";

            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add(glColumn.GL_Code, typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type", typeof(string));

            // Use a mutable dictionary for the cascading logic.
            pivotedLiabilityTable.ReplaceNullsWithZero(); // Assuming this is a custom extension method

            // MODIFICATION START: Ensure all Liability GL values are negative or zero when initializing the dictionary.
            var mutableLiabilityDict = pivotedLiabilityTable.AsEnumerable()
                .ToDictionary(
                    // Key
                    r => new {
                        Purchasing_Document = r.Field<string>("Purchasing_Document"),
                        Vendor = r.Field<string>("Vendor"),
                        Company_Code = r.Field<string>("Company_Code"),
                        Profit_Center = r.Field<string>("Profit_Center"),
                        Report_Date = r.Field<DateTime>("Report_Date"),
                        RevisionNumber = r.Field<string>("RevisionNumber")
                        //ICP_Name = r.Field<string>("ICP_Name"),
                        //ISSNA = r.Field<bool>("IsSNACompany"),
                    },
                    // Value: Mutable Dictionary of GLs and their original amounts (Positive -> 0)
                    r => _helper.LiabilityGLs.ToDictionary(
                            gl => gl.GL_Code,
                            gl => {
                                decimal? nullableValue = r.Field<decimal?>(gl.GL_Code);
                                decimal value = nullableValue ?? 0m;

                                Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}");
                                Guid guid = nullableGuid ?? Guid.Empty;

                                // If the value is positive (> 0), set it to 0m; otherwise, use the value.
                                decimal returnAmount = value > 0 ? 0m : value;

                                // *** Instantiate the mutable class instead of an anonymous type ***
                                return new GlAmountData(returnAmount, guid);
                            }
                        )
                );
            // MODIFICATION END

            // 2. Iterate through the *original* filteredAdvanceGLDataTable
            foreach (DataRow advRow in filteredAdvanceGLDataTable.Rows)
            {
                var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var vendor = advRow.Field<string>("Vendor");
                var companyCode = advRow.Field<string>("Company_Code");
                var profitCenter = advRow.Field<string>("Profit_Center");
                DateTime reportDate = advRow.Field<DateTime>("Report_Date");
                var revno = advRow.Field<string>("RevisionNumber");
                //var icp_name = advRow.Field<string>("ICP_Name");
                //var issna = advRow.Field<bool>("IsSNACompany");

                var fullCompositeKey = new
                {
                    Purchasing_Document = purchasingDoc,
                    Vendor = vendor,
                    Company_Code = companyCode,
                    Profit_Center = profitCenter,
                    Report_Date = reportDate,
                    RevisionNumber = revno,
                    //ICP_Name = icp_name,
                    //ISSNA = issna
                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Local");

                    // Create the new row for the result table, copying all fields from the advance row
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    // Loop through each Liability GL for the cascading logic
                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[glColumn.GL_Code].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}"] = matchedGLs[glColumn.GL_Code].GuidKey;




                        // Check if the GL has a remaining amount to process (will only be negative now, based on init logic)
                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;
                            // If the absolute liability amount is greater than the Advance GL amount, a split is needed.
                            // Assuming Advance GL Amount (e.g., 1000) is positive for comparison, and liability is negative (e.g., -1200).
                            // We compare |liability| against |advance|.
                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                // 1. Determine the consumed amount (it's the Advance GL amount, maintaining the GL's sign)
                                // This assumes advanceGLAmount is consumed *against* the liability.
                                // Since Advance GL is typically positive (e.g., 1000) and liability is negative (e.g., -1200),
                                // the consumed amount will be the negative of the Advance GL amount.
                                consumedAmount = -Math.Abs(advanceGLAmount);

                                // 2. Calculate the remaining amount for the next match/row
                                decimal remainingAmount = liabilityGLValue - consumedAmount; // e.g., -1200 - (-1000) = -200

                                // 3. Update the mutable dictionary for the *next* advance row with the same key
                                matchedGLs[glColumn.GL_Code].Amount = remainingAmount;

                                // 4. Set the current result row's GL to the consumed portion
                                newResultRow[glColumn.GL_Code] = consumedAmount; // e.g., -1000
                            }
                            else // Liability amount is less than or equal to Advance GL amount
                            {
                                // 1. Consume the entire liability GL amount
                                consumedAmount = liabilityGLValue; // e.g., -1200 if Advance GL was 2000

                                // 2. Set the remaining amount in the dictionary to zero
                                matchedGLs[glColumn.GL_Code].Amount = 0;

                                // 3. Set the current result row's GL to the full liability amount
                                newResultRow[glColumn.GL_Code] = consumedAmount;
                            }
                        }
                        else
                        {
                            // GL value is zero, set the new result row's GL to zero
                            newResultRow[glColumn.GL_Code] = 0m;
                        }
                    }

                    // Since this row is based on a row from filteredAdvanceGLDataTable, 
                    // Grouped_Invoice_Key and Amount_Local are guaranteed to be non-null.
                    if (matchedAndProcessed)
                    {
                        resultTable.Rows.Add(newResultRow);
                    }
                    else
                    {
                        // If the key was found but all GL values were zero, treat as "No Match" and add the row.
                        newResultRow["Join_Type"] = "No Match";
                        resultTable.Rows.Add(newResultRow);
                    }

                    // Cleanup the dictionary if all GLs have been consumed
                    if (matchedGLs.Values.All(v => v.Amount == 0))
                    {
                        mutableLiabilityDict.Remove(fullCompositeKey);
                    }
                }
                else
                {
                    // No match found, add the original row with "No Match".
                    DataRow noMatchRow = resultTable.NewRow();
                    noMatchRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    noMatchRow["Join_Type"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }

            // Remaining Liability rows are skipped to maintain non-null Grouped_Invoice_Key and Amount_Local.

            return resultTable;
        }
        public DataTable PerformCascadedJoinSNAData(DataTable filteredSNAData, DataTable pivotedLiabilityTable)
        {
            // 1. Initialize resultTable by cloning the structure of the left table.
            DataTable resultTable = filteredSNAData.Clone();
            resultTable.TableName = "Raw FAGLL03 GIT Sheet for SNA Data";

            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add(glColumn.GL_Code, typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type", typeof(string));

            // Use a mutable dictionary for the cascading logic.
            pivotedLiabilityTable.ReplaceNullsWithZero(); // Assuming this is a custom extension method

            // MODIFICATION START: Ensure all Liability GL values are negative or zero when initializing the dictionary.
            var mutableLiabilityDict = pivotedLiabilityTable.AsEnumerable()
                .ToDictionary(
                    // Key
                    r => new {
                        Vendor = r.Field<string>("Vendor"),
                        Vertical = r.Field<string>("Vertical"),
                        RevisionNumber = r.Field<string>("RevisionNumber"),
                        Report_Date = r.Field<DateTime>("Report_Date"),
                        Company_Code = r.Field<string>("Company_Code"),

                        //Purchasing_Document = r.Field<string>("Purchasing_Document"),
                        //Profit_Center = r.Field<string>("Profit_Center"),
                        //ICP_Name = r.Field<string>("ICP_Name"),
                        //ISSNA = r.Field<bool>("IsSNACompany"),
                    },
                    // Value: Mutable Dictionary of GLs and their original amounts (Positive -> 0)
                    r => _helper.LiabilityGLs.ToDictionary(
                            gl => gl.GL_Code,
                            gl => {
                                decimal? nullableValue = r.Field<decimal?>(gl.GL_Code);
                                decimal value = nullableValue ?? 0m;

                                Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}");
                                Guid guid = nullableGuid ?? Guid.Empty;

                                // If the value is positive (> 0), set it to 0m; otherwise, use the value.
                                decimal returnAmount = value > 0 ? 0m : value;

                                // *** Instantiate the mutable class instead of an anonymous type ***
                                return new GlAmountData(returnAmount, guid);
                            }
                        )
                );
            // MODIFICATION END

            // 2. Iterate through the *original* filteredAdvanceGLDataTable
            foreach (DataRow advRow in filteredSNAData.Rows)
            {
                var vendor = advRow.Field<string>("Vendor");
                var vertical = advRow.Field<string>("Vertical");
                var revno = advRow.Field<string>("RevisionNumber");
                DateTime reportDate = advRow.Field<DateTime>("Report_Date");
                //var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var companyCode = advRow.Field<string>("Company_Code");
                //var profitCenter = advRow.Field<string>("Profit_Center");
                //var icp_name = advRow.Field<string>("ICP_Name");
                //var issna = advRow.Field<bool>("IsSNACompany");

                var fullCompositeKey = new
                {
                    Vendor = vendor,
                    Vertical=vertical,
                    RevisionNumber=revno,
                    Report_Date = reportDate,
                    //Purchasing_Document = purchasingDoc,
                    Company_Code = companyCode,
                    //Profit_Center = profitCenter,
                    //ICP_Name = icp_name,
                    //ISSNA = issna
                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Local");

                    // Create the new row for the result table, copying all fields from the advance row
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    // Loop through each Liability GL for the cascading logic
                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[glColumn.GL_Code].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}"] = matchedGLs[glColumn.GL_Code].GuidKey;


                        // Check if the GL has a remaining amount to process (will only be negative now, based on init logic)
                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;
                            // If the absolute liability amount is greater than the Advance GL amount, a split is needed.
                            // Assuming Advance GL Amount (e.g., 1000) is positive for comparison, and liability is negative (e.g., -1200).
                            // We compare |liability| against |advance|.
                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                // 1. Determine the consumed amount (it's the Advance GL amount, maintaining the GL's sign)
                                // This assumes advanceGLAmount is consumed *against* the liability.
                                // Since Advance GL is typically positive (e.g., 1000) and liability is negative (e.g., -1200),
                                // the consumed amount will be the negative of the Advance GL amount.
                                consumedAmount = -Math.Abs(advanceGLAmount);

                                // 2. Calculate the remaining amount for the next match/row
                                decimal remainingAmount = liabilityGLValue - consumedAmount; // e.g., -1200 - (-1000) = -200

                                // 3. Update the mutable dictionary for the *next* advance row with the same key
                                matchedGLs[glColumn.GL_Code].Amount = remainingAmount;

                                // 4. Set the current result row's GL to the consumed portion
                                newResultRow[glColumn.GL_Code] = consumedAmount; // e.g., -1000
                            }
                            else // Liability amount is less than or equal to Advance GL amount
                            {
                                // 1. Consume the entire liability GL amount
                                consumedAmount = liabilityGLValue; // e.g., -1200 if Advance GL was 2000

                                // 2. Set the remaining amount in the dictionary to zero
                                matchedGLs[glColumn.GL_Code].Amount = 0;

                                // 3. Set the current result row's GL to the full liability amount
                                newResultRow[glColumn.GL_Code] = consumedAmount;
                            }
                        }
                        else
                        {
                            // GL value is zero, set the new result row's GL to zero
                            newResultRow[glColumn.GL_Code] = 0m;
                        }
                    }

                    // Since this row is based on a row from filteredAdvanceGLDataTable, 
                    // Grouped_Invoice_Key and Amount_Local are guaranteed to be non-null.
                    if (matchedAndProcessed)
                    {
                        resultTable.Rows.Add(newResultRow);
                    }
                    else
                    {
                        // If the key was found but all GL values were zero, treat as "No Match" and add the row.
                        newResultRow["Join_Type"] = "No Match";
                        resultTable.Rows.Add(newResultRow);
                    }

                    // Cleanup the dictionary if all GLs have been consumed
                    if (matchedGLs.Values.All(v => v.Amount == 0))
                    {
                        mutableLiabilityDict.Remove(fullCompositeKey);
                    }
                }
                else
                {
                    // No match found, add the original row with "No Match".
                    DataRow noMatchRow = resultTable.NewRow();
                    noMatchRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    noMatchRow["Join_Type"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }

            // Remaining Liability rows are skipped to maintain non-null Grouped_Invoice_Key and Amount_Local.

            return resultTable;
        }
 
        public static DataTable ConvertToAdjustedDataTableFromDictionaryList(List<dynamic> data)
        {
            if (data == null || data.Count == 0)
            {
                // Return an empty table if no data is provided
                return new DataTable("AdjustedData");
            }

            var resultTable = new DataTable("AdjustedData");

            // --- Step 1: Define the DataTable Schema (Columns) ---

            // We only need the keys from the first dictionary to define all columns, 
            // assuming all subsequent dictionaries have the same keys.
            var firstRowKeys = data.First().Keys;

            foreach (var columnName in firstRowKeys)
            {
                // Try to infer the type from the first non-null value for robustness,
                // though generally 'object' is used initially for flexibility.
                Type columnType = typeof(string);

                // Find the first non-null value to infer the type for this column
                var sampleValue = data.Select(d => d.ContainsKey(columnName) ? d[columnName] : null)
                                      .FirstOrDefault(v => v != null);

                if (sampleValue != null)
                {
                    // Use the type of the first non-null value found
                    columnType = sampleValue.GetType();
                }

                // DataTable cannot store nullable types directly, only their underlying value types.
                // If the inferred type is nullable (e.g., decimal?), use the underlying type (decimal).
                if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    columnType = Nullable.GetUnderlyingType(columnType);
                }

                // Handle common conversions (e.g., int/long/double should often be decimal for financial data)
                if (columnType == typeof(int) || columnType == typeof(long) || columnType == typeof(double))
                {
                    columnType = typeof(decimal);
                }

                // Add the defined column to the DataTable
                resultTable.Columns.Add(columnName, columnType);
            }

            // --- Step 2: Populate the DataTable Rows ---

            foreach (var rowDictionary in data)
            {
                var newRow = resultTable.NewRow();

                foreach (var column in resultTable.Columns.Cast<DataColumn>())
                {
                    string columnName = column.ColumnName;

                    if (rowDictionary.ContainsKey(columnName))
                    {
                        object value = rowDictionary[columnName];

                        if (value == null)
                        {
                            // Set value to DBNull if it's C# null
                            newRow[columnName] = DBNull.Value;
                        }
                        else
                        {
                            try
                            {
                                // Convert value to the target column type defined in the schema
                                newRow[columnName] = Convert.ChangeType(value, column.DataType);
                            }
                            catch (Exception ex)
                            {
                                // Log or handle conversion error if a type mismatch occurs (e.g., string into decimal)
                                System.Diagnostics.Debug.WriteLine($"Conversion Error for column {columnName}: {ex.Message}");
                                newRow[columnName] = DBNull.Value;
                            }
                        }
                    }
                    else
                    {
                        // If a dictionary somehow missed a column (schema inconsistency), set to DBNull
                        newRow[columnName] = DBNull.Value;
                    }
                }

                resultTable.Rows.Add(newRow);
            }

            return resultTable;
        }
        public static DataTable GITAdvanceManipulationFAGLL03Data(DataTable GitData, bool issna=false)
        {
            var resultTable = HelperFunctions.DeepCopyDataTable(GitData, "Processed FAGLL03 GIT Sheet");

            // Ensure columns exist
            resultTable.Columns.Add("Adjusted_GL", typeof(string));
            resultTable.Columns.Add("Adjusted_Amount", typeof(decimal));
            resultTable.Columns.Add("Total_Adjustment", typeof(decimal));
            resultTable.Columns.Add("Balance_Local", typeof(string));

            // Define GL hierarchies
            string[] standardGLs = { "14005", "14006", "14007", "14012", "14021", "14701" };
            string[] snaGLs = { "14702", "14703", "14704", "14705" };
            foreach (DataRow row in resultTable.Rows)
            {
                string vendor = row["Vendor"]?.ToString() ?? "";
                decimal totalAmountLocal = decimal.TryParse(row["Amount_Local"]?.ToString(), out var val) ? val : 0m;
                bool isICP = issna;

                row["Total_Adjustment"] = 0m;
                row["Balance_Local"] = totalAmountLocal;

                // 1. Validation Logic
                bool isInvalidVendor = string.IsNullOrWhiteSpace(vendor) ||
                                       vendor.Contains("not", StringComparison.OrdinalIgnoreCase) ||
                                       vendor.Contains("null", StringComparison.OrdinalIgnoreCase);

                if (totalAmountLocal <= 0 || isInvalidVendor)
                {
                    foreach (var gl in standardGLs.Concat(snaGLs)) row[gl] = "0";

                    row["Adjusted_GL"] = totalAmountLocal <= 0 ? "All GL set to 0, negative Advance" : "All GL set to 0, no Vendor Found!";
                    row["Adjusted_Amount"] = 0m;
                    continue;
                }

                // 2. Sequential Adjustment Logic
                decimal currentBalance = totalAmountLocal;
                decimal totalAdjustedForThisRow = 0m;

                // Combine GL lists: Standard first, then SNA if applicable
                List<string> glToProcess = [.. standardGLs];
                if (isICP) glToProcess.AddRange(snaGLs);

                foreach (string glCode in glToProcess)
                {
                    decimal glValue = HelperFunctions.GetDecimalValue(row, glCode);

                    // Ignore positive values as per original logic
                    if (glValue > 0)
                    {
                        row[glCode] = 0;
                        continue;
                    }

                    decimal absGLValue = Math.Abs(glValue);

                    if (absGLValue <= currentBalance)
                    {
                        // Full amount of this GL can be adjusted
                        totalAdjustedForThisRow += absGLValue;
                        currentBalance -= absGLValue;
                        row["Balance_Local"] = currentBalance;
                    }
                    else
                    {
                        // GL exceeds remaining balance: Cap it and stop
                        decimal diff = absGLValue - currentBalance;

                        totalAdjustedForThisRow += currentBalance;
                        row[glCode] = -currentBalance; // Adjusting the GL value in the row
                        currentBalance = 0;

                        row["Balance_Local"] = currentBalance;
                        row["Adjusted_Amount"] = diff;
                        row["Adjusted_GL"] = glCode;

                        // Since balance is 0, zero out remaining GLs in the sequence
                        int index = glToProcess.IndexOf(glCode);
                        for (int i = index + 1; i < glToProcess.Count; i++)
                        {
                            row[glToProcess[i]] = 0;
                        }
                        break;
                    }
                }

                row["Total_Adjustment"] = totalAdjustedForThisRow;
            }
            return resultTable;
        }
        public List<DataRow> PivotAdvanceGroup(List<DataRow> advanceGroup)
        {
            if (advanceGroup == null || advanceGroup.Count == 0)
                return []; // Return empty if input is empty

            DataRow firstRow = advanceGroup[0];

            DataTable resultTable = new("Pivoted Advance Group");

            List<string> ColumnsToPivot = ["GL_Account", "Advance_Adjustment"];

            foreach (DataColumn column in firstRow.Table.Columns)
                if (column.ColumnName != ColumnsToPivot[0] && column.ColumnName != ColumnsToPivot[1])
                    resultTable.Columns.Add(column.ColumnName, column.DataType);

            foreach (var advanceGL in _helper.AdvanceGLs)
                resultTable.Columns.Add(advanceGL.GL_Code, typeof(decimal));

            resultTable.Columns.Add("TotalAdvanceAmount", typeof(decimal));

            DataRow pivotedRow = resultTable.NewRow();

            foreach (DataColumn column in firstRow.Table.Columns)
            {
                string columnName = column.ColumnName;
                if (columnName != ColumnsToPivot[0] && columnName != ColumnsToPivot[1])
                    pivotedRow[columnName] = firstRow[columnName];
            }


            decimal totalAdvanceAmount = 0m;
            foreach (DataRow row in advanceGroup)
            {
                var advanceGL = row[ColumnsToPivot[0]]?.ToString();
                decimal advanceAdjusted = Convert.ToDecimal(row[ColumnsToPivot[1]]);
                totalAdvanceAmount += advanceAdjusted;

                if (resultTable.Columns.Contains(advanceGL!))
                    pivotedRow[advanceGL!] = advanceAdjusted;
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Advance GL {advanceGL} does not exist in AdvanceGLs List");
                }
            }
            pivotedRow["TotalAdvanceAmount"] = totalAdvanceAmount;
            resultTable.Rows.Add(pivotedRow);

            return [.. resultTable.AsEnumerable()];
        }
        public DataTable LineItemWiseAdvanceAdjustment(DataTable populatedData, DataTable LiabilityData)
        {
            DataTable populatedDataCopy = HelperFunctions.DeepCopyDataTable(populatedData, "Populated data copy");
            DataTable LiabilityDataCopy = HelperFunctions.DeepCopyDataTable(LiabilityData, "Net Liability Data Copy");

            var consumableLiabilityList = LiabilityDataCopy.AsEnumerable().ToList();
            var consumablePopulatedList = populatedDataCopy.AsEnumerable().ToList();

            var joinedData = new List<Dictionary<string, object>>();

            // Define SNA-specific GLs to be used for validation
            string[] snaLiabilityGLs = { "14702", "14703", "14704", "14705" };

            // --- STEP 1: JOIN POPULATED DATA WITH PIVOTED ADVANCES ---
            foreach (DataRow pRow in consumablePopulatedList)
            {
                var pKey = pRow.Field<Guid>("Grouped_Invoice_Key_Original");

                var advances = consumableLiabilityList
                    .Where(lRow => lRow.Field<Guid>("Grouped_Key") == pKey)
                    .ToList();

                DataRow? pivotedAdvanceRow = PivotAdvanceGroup(advances).FirstOrDefault();
                var newRow = new Dictionary<string, object>();

                foreach (DataColumn column in pRow.Table.Columns)
                {
                    newRow.Add(column.ColumnName, pRow[column.ColumnName] == DBNull.Value ? null : pRow[column.ColumnName]);
                }

                newRow["Amount_Local"] = pRow.Field<decimal?>("Amount_Local") ?? 0m;

                if (pivotedAdvanceRow != null)
                {
                    foreach (DataColumn column in pivotedAdvanceRow.Table.Columns)
                    {
                        List<string> ignoreColumns = new List<string> { "Grouped_Invoice_Key", "Join_Type", "Liability_GL_Code", "Grouped_Key", "Invoice_Key" };

                        string columnName = column.ColumnName;
                        if (pRow.Table.Columns.Contains(columnName) || (ignoreColumns.Contains(columnName) && columnName != "TotalAdvanceAmount"))
                            continue;

                        string newColName = "Advance_" + columnName;
                        decimal advanceValue = pivotedAdvanceRow.Field<decimal?>(columnName) ?? 0m;

                        newRow[newColName] = advanceValue;
                    }
                }
                else
                {
                    foreach (var advanceGL in _helper.AdvanceGLs)
                        newRow.Add("Advance_" + advanceGL.GL_Code, 0.0m);
                    newRow.Add("Advance_TotalAdvanceAmount", 0.0m);
                }

                joinedData.Add(newRow);
            }

            // --- STEP 2: GROUP BY INVOICE AND APPLY ADJUSTMENT LOGIC ---
            var finalAdjustedData = new List<dynamic>();
            var groupedInvoices = joinedData.GroupBy(i => i["Grouped_Invoice_Key_Original"]);

            foreach (var group in groupedInvoices)
            {
                // Sort by the absolute value of the amount (largest items first)
                var sortedLineItems = group.OrderByDescending(i => Math.Abs(Convert.ToDecimal(i["Amount_Local"]))).ToList();

                // Initial remaining advance taken from the first item (since it's grouped)
                decimal remainingAdvance = 0m;
                if (sortedLineItems.Count > 0 && sortedLineItems.First().ContainsKey("Advance_TotalAdvanceAmount"))
                {
                    remainingAdvance = Convert.ToDecimal(sortedLineItems.First()["Advance_TotalAdvanceAmount"]);
                }

                foreach (var item in sortedLineItems)
                {
                    decimal advanceApplied = 0.0m;
                    decimal lineItemAmnt = Convert.ToDecimal(item["Amount_Local"]);
                    string adjustmentType = "";

                    // Identify company type and GL for the specific line item
                    string icpName = item.ContainsKey("ICP_Name") ? item["ICP_Name"]?.ToString() ?? "" : "";
                    string currentGL = item.ContainsKey("GL_Account") ? item["GL_Account"]?.ToString() ?? "" : "";

                    bool isSNACompany = !string.IsNullOrWhiteSpace(icpName) &&
                                        !icpName.Equals("null", StringComparison.OrdinalIgnoreCase) &&
                                        !icpName.Equals("not", StringComparison.OrdinalIgnoreCase);

                    // Determine eligibility: 
                    // 1. Must not start with '2'
                    // 2. If it's an SNA-Specific GL, the company must be an SNA company
                    bool isEligibleForAdjustment = true;

                    if (currentGL.StartsWith("2"))
                    {
                        isEligibleForAdjustment = false;
                    }
                    else if (!isSNACompany && snaLiabilityGLs.Contains(currentGL))
                    {
                        isEligibleForAdjustment = false;
                    }

                    // Perform Calculation only if eligible and advance balance exists
                    if (remainingAdvance > 0 && isEligibleForAdjustment)
                    {
                        // Logic: Only adjust against negative amounts (Credits/Advances) as per original logic
                        if (lineItemAmnt < 0)
                        {
                            decimal lineItemAbs = Math.Abs(lineItemAmnt);
                            advanceApplied = Math.Min(remainingAdvance, lineItemAbs);
                            remainingAdvance -= advanceApplied;
                            adjustmentType = "Advance Adjustment";
                        }
                    }

                    // Build the final result dictionary
                    var finalRow = new Dictionary<string, object>();
                    foreach (var kvp in item)
                    {
                        finalRow.TryAdd(kvp.Key, kvp.Value);
                    }

                    finalRow.Add("Advance_Applied", advanceApplied);
                    finalRow.Add("Amount_Local_Adjusted", lineItemAmnt + advanceApplied);
                    finalRow.Add("Adjustment_Type", adjustmentType);
                    finalRow.Add("Total_Remaining_Advance", remainingAdvance);

                    finalAdjustedData.Add(finalRow);
                }
            }

            return ConvertToAdjustedDataTableFromDictionaryList(finalAdjustedData);
        }
        public static DataTable UnpivotProcessedGIT2(DataTable processedGIT)
        {
            // 1. Define the structure for the output DataTable
            DataTable outputTable = new DataTable("Unpivoted_ProcessedGIT");

            // Add original columns
            outputTable.Columns.Add("Grouped_Invoice_Key", typeof(Guid)); // Assuming string/guid
            outputTable.Columns.Add("Purchasing_Document", typeof(string));
            outputTable.Columns.Add("Vendor", typeof(string));
            outputTable.Columns.Add("Company_Code", typeof(string));
            outputTable.Columns.Add("GL_Account", typeof(string));
            outputTable.Columns.Add("Profit_Center", typeof(string));
            outputTable.Columns.Add("Amount_Local", typeof(decimal));
            outputTable.Columns.Add("Report_Date", typeof(DateTime));
            outputTable.Columns.Add("Join_Type", typeof(string));
            outputTable.Columns.Add("Total_Adjustment", typeof(decimal));
            outputTable.Columns.Add("Balance_Local", typeof(decimal));

            // Add unpivoted/calculated columns
            outputTable.Columns.Add("Liability_GL_Code", typeof(string));
            outputTable.Columns.Add("Liability_Amount", typeof(decimal));
            outputTable.Columns.Add("Grouped_Key", typeof(Guid)); // The unpivoted Grouped_Key
            outputTable.Columns.Add("Advance_Adjustment", typeof(decimal)); // ABS(Liability_Amount)

            // Define the list of columns to be unpivoted (Liability Amount, Grouped Key)
            var glColumnMap = new[]
            {
        new { GLCode = "14005", AmountCol = "14005", KeyCol = "Grouped_Key_14005" },
        new { GLCode = "14006", AmountCol = "14006", KeyCol = "Grouped_Key_14006" },
        new { GLCode = "14007", AmountCol = "14007", KeyCol = "Grouped_Key_14007" },
        new { GLCode = "14012", AmountCol = "14012", KeyCol = "Grouped_Key_14012" },
        new { GLCode = "14021", AmountCol = "14021", KeyCol = "Grouped_Key_14021" },
        new { GLCode = "14701", AmountCol = "14701", KeyCol = "Grouped_Key_14701" },

        // Speacial Liability GLs for SNA Companies only
        new { GLCode = "14702", AmountCol = "14702", KeyCol = "Grouped_Key_14702" },
        new { GLCode = "14703", AmountCol = "14703", KeyCol = "Grouped_Key_14703" },
        new { GLCode = "14704", AmountCol = "14704", KeyCol = "Grouped_Key_14704" },
        new { GLCode = "14705", AmountCol = "14705", KeyCol = "Grouped_Key_14705" },



    };





            // 2. Use LINQ to DataSet to process the data (replicates the CROSS APPLY)
            var results =
                from row in processedGIT.AsEnumerable()
                from glMap in glColumnMap
                let liabilityAmount = row.Field<decimal?>(glMap.AmountCol) ?? 0m
                let groupedKey = row.Field<Guid?>(glMap.KeyCol) ?? Guid.Empty
                where liabilityAmount != 0m
                select new
                {
                    SourceRow = row,
                    GLMap = glMap,
                    LiabilityAmount = liabilityAmount,
                    GroupedKey = groupedKey
                };

            foreach (var item in results)
            {
                DataRow newRow = outputTable.NewRow();
                newRow["Grouped_Invoice_Key"] = item.SourceRow["Grouped_Invoice_Key"];
                newRow["Purchasing_Document"] = item.SourceRow["Purchasing_Document"] ?? "";
                newRow["Vendor"] = item.SourceRow["Vendor"];
                newRow["Company_Code"] = item.SourceRow["Company_Code"];
                newRow["GL_Account"] = item.SourceRow["GL_Account"];
                newRow["Profit_Center"] = item.SourceRow["Profit_Center"];
                newRow["Amount_Local"] = item.SourceRow["Amount_Local"];
                newRow["Report_Date"] = item.SourceRow["Report_Date"];
                newRow["Join_Type"] = item.SourceRow["Join_Type"];
                newRow["Total_Adjustment"] = item.SourceRow["Total_Adjustment"];
                newRow["Balance_Local"] = item.SourceRow["Balance_Local"];
                newRow["Liability_GL_Code"] = item.GLMap.GLCode;
                newRow["Liability_Amount"] = item.LiabilityAmount;
                newRow["Grouped_Key"] = item.GroupedKey;
                newRow["Advance_Adjustment"] = Math.Abs(item.LiabilityAmount);
                outputTable.Rows.Add(newRow);
            }
            return outputTable;
        }
        
        
        public static DataTable FilterForDataWithPO(DataTable table)
        {
            var dataFilter = table.AsEnumerable()
                                       .Where(row => {
                                           string po = row.Field<string>("Purchasing_Document")!;
                                           bool issna = row.Field<bool>("IsSNACompany")!;

                                           return !string.IsNullOrWhiteSpace(po) &&
                                                   !string.IsNullOrEmpty(po) &&
                                                   !po.StartsWith("no", StringComparison.CurrentCultureIgnoreCase) && !issna;
                                       });

            if (!dataFilter.Any()) throw new Exception("Filter resulted in empty datatable");

            DataTable data = HelperFunctions.DeepCopyDataTable(dataFilter.CopyToDataTable(), "Data with PO");
            return data;
        }
        public static DataTable FilterForDataWithoutPO(DataTable table)
        {
            var dataFilter = table.AsEnumerable()
                                        .Where(row => {
                                            string po = row.Field<string>("Purchasing_Document")!;
                                            bool issna = row.Field<bool>("IsSNACompany")!;

                                            return !issna && 
                                            (string.IsNullOrWhiteSpace(po) || string.IsNullOrEmpty(po) || po.StartsWith("no", StringComparison.CurrentCultureIgnoreCase));

                                        });

            if (!dataFilter.Any()) throw new Exception("Filter resulted in empty datatable");

            DataTable data = HelperFunctions.DeepCopyDataTable(dataFilter.CopyToDataTable(), "Data without PO: No PO");
            return data;
        }
        public static DataTable FilterForSNAData(DataTable table)
        {
            var dataFilter = table.AsEnumerable()
                                        .Where(row => {
                                            bool issna = row.Field<bool>("IsSNACompany");
                                            return issna;
                                        });

            if (!dataFilter.Any()) throw new Exception("Filter resulted in empty datatable");

            DataTable data = HelperFunctions.DeepCopyDataTable(dataFilter.CopyToDataTable(), "SNA ony Data");
            return data;
        }


        public static DataTable FilterForAdvanceGLs(DataTable table)
        {
            //Filter to only contain data for Adavance Gls
            var advanceGLDataFilter = table.AsEnumerable()
                               .Where(static row => row.Field<string>("GL_Account")!.StartsWith('2'));
            if (!advanceGLDataFilter.Any()) throw new Exception("No data for advance GLs present in the dataset");
            // A new DataTable containing only advance GL data
            DataTable advanceGLData = HelperFunctions.DeepCopyDataTable(advanceGLDataFilter.CopyToDataTable(), "Only Advance GLs");
            return advanceGLData;

        }
        public static DataTable FilterForLiabilityGLs(DataTable table, List<string> liabilityGls)
        {
            var liabilityGLDataFilter = table.AsEnumerable()
                               .Where(row => liabilityGls.Contains(row.Field<string>("GL_Account")??""));
            if (!liabilityGLDataFilter.Any()) throw new Exception("No data for Liability GLs present in the dataset");

            DataTable liabilityGLdata = HelperFunctions.DeepCopyDataTable(liabilityGLDataFilter.CopyToDataTable(), "Only Liability GLs");
            return liabilityGLdata;
        }
    }
}
