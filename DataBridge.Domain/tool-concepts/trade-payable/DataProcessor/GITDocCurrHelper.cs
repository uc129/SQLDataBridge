using Application.Extensions;
using Dapper;
using Microsoft.VisualBasic;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataProcessor
{
    public class GITDocCurrHelper(HelperFunctions helper)
    {
        private readonly HelperFunctions _helper = helper;

        public static List<DataTable> GroupFAGLL03DocCurrDataWithoutProfitCenter(DataTable data)
        {
            DataTable originalTable = HelperFunctions.DeepCopyDataTable(data, "Data with Doc Grouping Keys");

            if (!originalTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
                originalTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

            var groupedDataForProcessing = originalTable.AsEnumerable()
                .GroupBy(row => new
                {
                    PurchasingDocument = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    CompanyCode = row.Field<string>("Company_Code"),
                    GLAccount = row.Field<string>("GL_Account"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    Document_Currency = row.Field<string>("Document_Currency"),
                    //ICP_Name = row.Field<string?>("ICP_Name"),
                })
                .ToList();

            // 3. Initialize the new aggregated DataTable structure.
            DataTable aggregatedTable = new DataTable("Grouped FAGLL03 Table With DocCurr Without PC");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
            aggregatedTable.Columns.Add("Purchasing_Document", typeof(string));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Company_Code", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("Profit_Center", typeof(string));
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Industry", typeof(string));
            aggregatedTable.Columns.Add("Payment_Terms", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Document_Currency", typeof(string));
            aggregatedTable.Columns.Add("Amount_Doc", typeof(decimal));
            aggregatedTable.Columns.Add("ICP_Name", typeof(string));



            foreach (var group in groupedDataForProcessing)
            {
                Guid group_guid = Guid.NewGuid();

                foreach (DataRow row in group)
                    row["Grouped_Invoice_Key_Original_Doc"] = group_guid;

                decimal totalAmountDoc = group.Sum(r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Doc"];
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
                    group.First().Field<string>("Vendor_Description"),
                    group.First().Field<string>("GL_Description"),
                    group.First().Field<string>("Industry"),
                    group.First().Field<string>("Payment_Terms"),
                    group.Key.Report_Date,
                    group.Key.RevisionNumber,
                    group.Key.Document_Currency,
                    totalAmountDoc
                    //group.Key.ICP_Name
                );
            }

            // 5. Return the newly created aggregated table.
            return [originalTable,aggregatedTable];
        }
        public static List<DataTable> GroupFAGLL03DocCurrDataWithProfitCenter(DataTable data)
        {
            DataTable originalTable = HelperFunctions.DeepCopyDataTable(data, "Data with Doc Grouping Keys");

            if (!originalTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
                originalTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

            var groupedDataForProcessing = originalTable.AsEnumerable()
                .GroupBy(row => new
                {
                    PurchasingDocument = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    CompanyCode = row.Field<string>("Company_Code"),
                    GLAccount = row.Field<string>("GL_Account"),
                    ProfitCenter = row.Field<string>("Profit_Center"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    Document_Currency = row.Field<string>("Document_Currency"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),

                    //ICP_Name = row.Field<string?>("ICP_Name"),
                })
                .ToList();

            // 3. Initialize the new aggregated DataTable structure.
            DataTable aggregatedTable = new DataTable("Grouped FAGLL03 Table With DocCurr With PC");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
            aggregatedTable.Columns.Add("Purchasing_Document", typeof(string));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Company_Code", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("Profit_Center", typeof(string));
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Industry", typeof(string));
            aggregatedTable.Columns.Add("Payment_Terms", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Document_Currency", typeof(string));
            aggregatedTable.Columns.Add("Amount_Doc", typeof(decimal));
            aggregatedTable.Columns.Add("ICP_Name", typeof(string));


            foreach (var group in groupedDataForProcessing)
            {
                Guid group_guid = Guid.NewGuid();

                foreach (DataRow row in group)
                    row["Grouped_Invoice_Key_Original_Doc"] = group_guid;

                decimal totalAmountDoc = group.Sum(r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Doc"];
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
                    group.Key.ProfitCenter,
                    group.First().Field<string>("Vendor_Description"),
                    group.First().Field<string>("GL_Description"),
                    group.First().Field<string>("Industry"),
                    group.First().Field<string>("Payment_Terms"),
                    group.Key.Report_Date,
                    group.Key.RevisionNumber,
                    group.Key.Document_Currency,
                    totalAmountDoc
                );
            }

            // 5. Return the newly created aggregated table.
            return [originalTable, aggregatedTable];
        }
        public static List<DataTable> GroupFAGLL03DocCurrDataForSNA(DataTable snadata)
        {
            DataTable newTable = HelperFunctions.DeepCopyDataTable(snadata, "SNA Doc Curr data with Grouping Keys");
            if (!newTable.Columns.Contains("Grouped_Invoice_Key_Original_Doc"))
                newTable.Columns.Add("Grouped_Invoice_Key_Original_Doc", typeof(Guid));

            var groupedDataForProcessing = newTable.AsEnumerable()
                .GroupBy(row => new
                {
                    Vendor = row.Field<string>("Vendor"),
                    Vertical = row.Field<string>("Vertical"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    GLAccount = row.Field<string>("GL_Account"),
                    Document_Currency = row.Field<string>("Document_Currency"),
                    Company_Code = row.Field<string>("Company_Code")
                })
                .ToList();

            DataTable aggregatedTable = new("Grouped SNA Data");
            aggregatedTable.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid));
            aggregatedTable.Columns.Add("Vendor", typeof(string));
            aggregatedTable.Columns.Add("Amount_Doc", typeof(decimal)); // The aggregated sum
            aggregatedTable.Columns.Add("Vendor_Description", typeof(string));
            aggregatedTable.Columns.Add("RevisionNumber", typeof(string));
            aggregatedTable.Columns.Add("Report_Date", typeof(DateTime));
            aggregatedTable.Columns.Add("Vertical", typeof(string));
            aggregatedTable.Columns.Add("GL_Account", typeof(string));
            aggregatedTable.Columns.Add("GL_Description", typeof(string));
            aggregatedTable.Columns.Add("Document_Currency", typeof(string));
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
                    row["Grouped_Invoice_Key_Original_Doc"] = group_guid;

                decimal totalAmountDoc = group.Sum(r =>
                {
                    decimal value = 0;
                    object amountObj = r["Amount_Doc"];
                    if (amountObj != DBNull.Value && amountObj != null)
                        decimal.TryParse(amountObj.ToString(), out value);
                    return value;
                });

                aggregatedTable.Rows.Add(
                    group_guid,
                    group.Key.Vendor,
                    totalAmountDoc,
                    group.First().Field<string>("Vendor_Description"),
                    group.Key.RevisionNumber,
                    group.Key.Report_Date,
                    group.Key.Vertical,
                    group.Key.GLAccount,
                    group.First().Field<string>("GL_Description"),
                    group.Key.Document_Currency,
                    group.Key.Company_Code

                //group.Key.PurchasingDocument,
                //group.Key.CompanyCode,
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



        //Pivot Grouped Liability Data
        public  DataTable PivotLiabilityGLDocCurrDataWithoutProfitCenter(DataTable liabilityGLData)
        {
            DataTable pivotedLiabilityTable = new DataTable("Pivoted Liability GLs With Doc Curr | No PC");

            // Add the key columns
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));
            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));
            pivotedLiabilityTable.Columns.Add("Document_Currency", typeof(string));
            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));

            // Add a new column for each Liability GL
            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add($"{gl.GL_Code}_Doc", typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}_Doc", typeof(Guid));
            }


            //2.Group the data by the key columns using LINQ
            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Company_Code = row.Field<string>("Company_Code"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    Document_Currency = row.Field<string>("Document_Currency"),
                    RevisionNumber = row.Field<string>("RevisionNumber")
                    //ICP_Name = row.Field<string?>("ICP_Name"),
                });

            // 3. Populate the new pivoted DataTable
            foreach (var group in groupedData)
            {
                DataRow newRow = pivotedLiabilityTable.NewRow();

                // Set the key values
                newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                newRow["Vendor"] = group.Key.Vendor;
                newRow["Company_Code"] = group.Key.Company_Code;
                newRow["Profit_Center"] = group.First().Field<string>("Profit_Center");
                newRow["Report_Date"] = group.Key.Report_Date;
                newRow["Document_Currency"] = group.Key.Document_Currency;
                newRow["RevisionNumber"] = group.Key.RevisionNumber;
                //newRow["ICP_Name"] = group.Key.ICP_Name;


                // Populate the pivoted GL columns
                foreach (var row in group)
                {
                    var gl = row.Field<string>("GL_Account");
                    decimal amountDoc = row.Field<decimal>("Amount_Doc");
                    newRow[$"{gl}_Doc"] = amountDoc;
                    newRow[$"Grouped_Key_{gl}_Doc"] = row.Field<Guid>("Grouped_Invoice_Key_Doc");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }
        public  DataTable PivotLiabilityGLDocCurrDataWithProfitCenter(DataTable liabilityGLData)
        {
            // Pivot LiabilityGL table, such that each GL will have their own column.
            DataTable pivotedLiabilityTable = new DataTable("Pivoted Liability GLs With Doc Curr And PC");

            // Add the key columns
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));
            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));


            pivotedLiabilityTable.Columns.Add("Document_Currency", typeof(string));
            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));

            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add($"{gl.GL_Code}_Doc", typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}_Doc", typeof(Guid));
            }


            //2.Group the data by the key columns using LINQ
            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Company_Code = row.Field<string>("Company_Code"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    ProfitCenter = row.Field<string>("Profit_Center"),
                    Document_Currency = row.Field<string>("Document_Currency"),
                    RevisionNumber = row.Field<string>("RevisionNumber")
                    //ICP_Name = row.Field<string?>("ICP_Name"),
                });

            // 3. Populate the new pivoted DataTable
            foreach (var group in groupedData)
            {
                DataRow newRow = pivotedLiabilityTable.NewRow();

                // Set the key values
                newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                newRow["Vendor"] = group.Key.Vendor;
                newRow["Company_Code"] = group.Key.Company_Code;
                newRow["Profit_Center"] = group.Key.ProfitCenter;
                newRow["Report_Date"] = group.Key.Report_Date;
                newRow["Document_Currency"] = group.Key.Document_Currency;
                newRow["RevisionNumber"] = group.Key.RevisionNumber;

                //newRow["ICP_Name"] = group.Key.ICP_Name;


                // Populate the pivoted GL columns
                foreach (var row in group)
                {
                    var gl = row.Field<string>("GL_Account");
                    decimal amountDoc = row.Field<decimal>("Amount_Doc");
                    newRow[$"{gl}_Doc"] = amountDoc;
                    newRow[$"Grouped_Key_{gl}_Doc"] = row.Field<Guid>("Grouped_Invoice_Key_Doc");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }
        public DataTable PivotFAGLL03DocCurrSNAData(DataTable liabilityGLData)
        {
            DataTable pivotedLiabilityTable = new("Pivoted Liability SNA Doc Curr Data");
            pivotedLiabilityTable.Columns.Add("Vendor", typeof(string));
            pivotedLiabilityTable.Columns.Add("Vertical", typeof(string));
            pivotedLiabilityTable.Columns.Add("Report_Date", typeof(DateTime));
            pivotedLiabilityTable.Columns.Add("RevisionNumber", typeof(string));
            pivotedLiabilityTable.Columns.Add("Document_Currency", typeof(string));
            pivotedLiabilityTable.Columns.Add("Company_Code", typeof(string));


            pivotedLiabilityTable.Columns.Add("Profit_Center", typeof(string));
            pivotedLiabilityTable.Columns.Add("Purchasing_Document", typeof(string));
            pivotedLiabilityTable.Columns.Add("ICP_Name", typeof(string));
            pivotedLiabilityTable.Columns.Add("IsSNACompany", typeof(bool));


            foreach (var gl in _helper.LiabilityGLs)
            {
                pivotedLiabilityTable.Columns.Add($"{gl.GL_Code}_Doc", typeof(decimal));
                pivotedLiabilityTable.Columns.Add($"Grouped_Key_{gl.GL_Code}_Doc", typeof(Guid));
            }


            var groupedData = liabilityGLData.AsEnumerable()
                .GroupBy(row => new
                {
                    //Purchasing_Document = row.Field<string>("Purchasing_Document"),
                    Vendor = row.Field<string>("Vendor"),
                    Vertical = row.Field<string>("Vertical"),
                    RevisionNumber = row.Field<string>("RevisionNumber"),
                    Report_Date = row.Field<DateTime>("Report_Date"),
                    Document_Currency = row.Field<string>("Document_Currency"),
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
                newRow["Document_Currency"] = group.Key.Document_Currency;
                newRow["Company_Code"] = group.Key.Company_Code;

                //newRow["Purchasing_Document"] = group.Key.Purchasing_Document;
                //newRow["Profit_Center"] = group.First().Field<string>("Profit_Center");
                //newRow["ICP_Name"] = group.Key.ICP_Name;
                //newRow["IsSNACompany"] = group.Key.IsSNACompany;


                foreach (var row in group)
                {
                    var gl = row.Field<string>("GL_Account");
                    decimal amountDoc = row.Field<decimal>("Amount_Doc");
                    newRow[$"{gl}_Doc"] = amountDoc;
                    newRow[$"Grouped_Key_{gl}_Doc"] = row.Field<Guid>("Grouped_Invoice_Key_Doc");
                }
                pivotedLiabilityTable.Rows.Add(newRow);
            }
            return pivotedLiabilityTable;
        }



        private class GlAmountData(decimal amount, Guid guidKey)
        {
            public decimal Amount = amount;
            public Guid GuidKey = guidKey;
        }
        public  DataTable PerformDocCurrCascadedJoinWithoutProfitCenter(DataTable filteredAdvanceGLDataTable, DataTable pivotedLiabilityTable)
        {
            // 1. Initialize resultTable by cloning the structure of the left table.
            DataTable resultTable = HelperFunctions.DeepCopyDataTable(filteredAdvanceGLDataTable, "Raw FAGLL03 GIT Sheet WIth Doc Currency | Without Profit Center");


            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add($"{glColumn.GL_Code}_Doc", typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}_Doc", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type_Doc", typeof(string));

            pivotedLiabilityTable.ReplaceNullsWithZero();

            // MODIFICATION START: Ensure all Liability GL values are negative or zero when initializing the dictionary.
            var mutableLiabilityDict = pivotedLiabilityTable.AsEnumerable()
                .ToDictionary(
                    // Key
                    r => new {
                        Purchasing_Document = r.Field<string>("Purchasing_Document"),
                        Vendor = r.Field<string>("Vendor"),
                        Company_Code = r.Field<string>("Company_Code"),
                        Report_Date = r.Field<DateTime>("Report_Date"),
                        RevisionNumber = r.Field<string>("RevisionNumber"),
                        Document_Currency = r.Field<string>("Document_Currency"),
                        //ICP_Name = r.Field<string?>("ICP_Name")
                    },
                    // Value: Mutable Dictionary of GLs and their original amounts (Positive -> 0)
                    r => _helper.LiabilityGLs.ToDictionary(
                            gl => gl.GL_Code,
                            gl => {
                                decimal? nullableValue = r.Field<decimal?>($"{gl.GL_Code}_Doc");
                                decimal value = nullableValue ?? 0m;

                                Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}_Doc");
                                Guid guid = nullableGuid ?? Guid.Empty;

                                // If the value is positive (> 0), set it to 0m; otherwise, use the value.
                                decimal returnAmount = value > 0 ? 0m : value;

                                // *** Instantiate the mutable class instead of an anonymous type ***
                                return new GlAmountData(returnAmount, guid);
                            }
                        )
                );

            // 2. Iterate through the *original* filteredAdvanceGLDataTable
            foreach (DataRow advRow in filteredAdvanceGLDataTable.Rows)
            {
                var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var vendor = advRow.Field<string>("Vendor");
                var companyCode = advRow.Field<string>("Company_Code");
                DateTime reportDate = advRow.Field<DateTime>("Report_Date");
                var docCurr = advRow.Field<string>("Document_Currency");
                var revno = advRow.Field<string>("RevisionNumber");
                //var icpname = advRow.Field<string?>("ICP_Name");

                var fullCompositeKey = new
                {
                    Purchasing_Document = purchasingDoc,
                    Vendor = vendor,
                    Company_Code = companyCode,
                    Report_Date = reportDate,
                    RevisionNumber = revno,
                    Document_Currency = docCurr,
                    //ICP_Name = icpname
                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Doc");
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type_Doc"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    // Loop through each Liability GL for the cascading logic
                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[$"{glColumn.GL_Code}"].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}_Doc"] = matchedGLs[$"{glColumn.GL_Code}"].GuidKey;

                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;

                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                consumedAmount = -Math.Abs(advanceGLAmount);
                                decimal remainingAmount = liabilityGLValue - consumedAmount;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = remainingAmount;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                            else // Liability amount is less than or equal to Advance GL amount
                            {
                                consumedAmount = liabilityGLValue;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = 0;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                        }
                        else
                        {
                            newResultRow[$"{glColumn.GL_Code}_Doc"] = 0m;
                        }
                    }

                    if (matchedAndProcessed)
                        resultTable.Rows.Add(newResultRow);
                    else
                    {
                        newResultRow["Join_Type_Doc"] = "No Match";
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
                    noMatchRow["Join_Type_Doc"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }

            // Remaining Liability rows are skipped to maintain non-null Grouped_Invoice_Key and Amount_Doc.

            return resultTable;
        }
        public  DataTable PerformDocCurrCascadedJoinWithProfitCenter(DataTable filteredAdvanceGLDataTable, DataTable pivotedLiabilityTable)
        {
            // 1. Initialize resultTable by cloning the structure of the left table.
            DataTable resultTable = HelperFunctions.DeepCopyDataTable(filteredAdvanceGLDataTable, "Raw FAGLL03 GIT Sheet WIth Doc Currency | Without Profit Center");

            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add($"{glColumn.GL_Code}_Doc", typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}_Doc", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type_Doc", typeof(string));

            pivotedLiabilityTable.ReplaceNullsWithZero();

            // MODIFICATION START: Ensure all Liability GL values are negative or zero when initializing the dictionary.
            var mutableLiabilityDict = pivotedLiabilityTable.AsEnumerable()
                .ToDictionary(
                    // Key
                    r => new {
                        Purchasing_Document = r.Field<string>("Purchasing_Document"),
                        Vendor = r.Field<string>("Vendor"),
                        Company_Code = r.Field<string>("Company_Code"),
                        Report_Date = r.Field<DateTime>("Report_Date"),
                        RevisionNumber = r.Field<string>("RevisionNumber"),
                        Document_Currency = r.Field<string>("Document_Currency"),
                        ProfitCenter = r.Field<string>("Profit_Center"),
                        //ICP_Name = r.Field<string?>("ICP_Name")
                    },
                    // Value: Mutable Dictionary of GLs and their original amounts (Positive -> 0)
                    r => _helper.LiabilityGLs.ToDictionary(
                            gl => gl.GL_Code,
                            gl => {
                                decimal? nullableValue = r.Field<decimal?>($"{gl.GL_Code}_Doc");
                                decimal value = nullableValue ?? 0m;

                                Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}_Doc");
                                Guid guid = nullableGuid ?? Guid.Empty;

                                // If the value is positive (> 0), set it to 0m; otherwise, use the value.
                                decimal returnAmount = value > 0 ? 0m : value;

                                // *** Instantiate the mutable class instead of an anonymous type ***
                                return new GlAmountData(returnAmount, guid);
                            }
                        )
                );

            // 2. Iterate through the *original* filteredAdvanceGLDataTable
            foreach (DataRow advRow in filteredAdvanceGLDataTable.Rows)
            {
                var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var vendor = advRow.Field<string>("Vendor");
                var companyCode = advRow.Field<string>("Company_Code");
                DateTime reportDate = advRow.Field<DateTime>("Report_Date");
                var docCurr = advRow.Field<string>("Document_Currency");
                var profitCenter = advRow.Field<string>("Profit_Center");
                var revno = advRow.Field<string>("RevisionNumber");
                //var icpname = advRow.Field<string?>("ICP_Name");

                var fullCompositeKey = new
                {
                    Purchasing_Document = purchasingDoc,
                    Vendor = vendor,
                    Company_Code = companyCode,
                    Report_Date = reportDate,
                    RevisionNumber= revno,
                    Document_Currency = docCurr,
                    ProfitCenter = profitCenter,
                    //ICP_Name = icpname

                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Doc");
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type_Doc"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    // Loop through each Liability GL for the cascading logic
                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[$"{glColumn.GL_Code}"].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}_Doc"] = matchedGLs[$"{glColumn.GL_Code}"].GuidKey;

                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;

                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                consumedAmount = -Math.Abs(advanceGLAmount);
                                decimal remainingAmount = liabilityGLValue - consumedAmount;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = remainingAmount;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                            else // Liability amount is less than or equal to Advance GL amount
                            {
                                consumedAmount = liabilityGLValue;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = 0;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                        }
                        else
                        {
                            newResultRow[$"{glColumn.GL_Code}_Doc"] = 0m;
                        }
                    }

                    if (matchedAndProcessed)
                        resultTable.Rows.Add(newResultRow);
                    else
                    {
                        newResultRow["Join_Type_Doc"] = "No Match";
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
                    noMatchRow["Join_Type_Doc"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }

            // Remaining Liability rows are skipped to maintain non-null Grouped_Invoice_Key and Amount_Doc.

            return resultTable;
        }
        public DataTable PerformCascadedJoinDocCurrSNAData(DataTable filteredSNAData, DataTable pivotedLiabilityTable)
        {
            // 1. Initialize resultTable by cloning the structure of the left table.
            DataTable resultTable = filteredSNAData.Clone();
            resultTable.TableName = "Raw FAGLL03 GIT Sheet for SNA Data";

            // Add new columns for the liability GLs and the join type.
            foreach (var glColumn in _helper.LiabilityGLs)
            {
                resultTable.Columns.Add($"{glColumn.GL_Code}_Doc", typeof(decimal));
                resultTable.Columns.Add($"Grouped_Key_{glColumn.GL_Code}_Doc", typeof(Guid));
            }

            resultTable.Columns.Add("Join_Type_Doc", typeof(string));

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
                        Document_Currency = r.Field<string>("Document_Currency"),
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
                               decimal? nullableValue = r.Field<decimal?>($"{gl.GL_Code}_Doc");
                               decimal value = nullableValue ?? 0m;

                               Guid? nullableGuid = r.Field<Guid?>($"Grouped_Key_{gl.GL_Code}_Doc");
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
                var doccurr = advRow.Field<string>("Document_Currency");
                //var purchasingDoc = advRow.Field<string>("Purchasing_Document");
                var companyCode = advRow.Field<string>("Company_Code");
                //var profitCenter = advRow.Field<string>("Profit_Center");
                //var icp_name = advRow.Field<string>("ICP_Name");
                //var issna = advRow.Field<bool>("IsSNACompany");

                var fullCompositeKey = new
                {
                    Vendor = vendor,
                    Vertical = vertical,
                    RevisionNumber = revno,
                    Report_Date = reportDate,
                    Document_Currency = doccurr,
                    //Purchasing_Document = purchasingDoc,
                    Company_Code = companyCode,
                    //Profit_Center = profitCenter,
                    //ICP_Name = icp_name,
                    //ISSNA = issna
                };

                // Attempt a Full Composite match using the mutable dictionary
                if (mutableLiabilityDict.TryGetValue(fullCompositeKey, out var matchedGLs))
                {
                    decimal advanceGLAmount = advRow.Field<decimal>("Amount_Doc");
                    DataRow newResultRow = resultTable.NewRow();
                    newResultRow.ItemArray = advRow.ItemArray.Clone() as object[];
                    newResultRow["Join_Type_Doc"] = "Full Composite";
                    bool matchedAndProcessed = false;

                    // Loop through each Liability GL for the cascading logic
                    foreach (var glColumn in _helper.LiabilityGLs)
                    {
                        decimal liabilityGLValue = matchedGLs[$"{glColumn.GL_Code}"].Amount;
                        decimal consumedAmount = 0m;
                        newResultRow[$"Grouped_Key_{glColumn.GL_Code}_Doc"] = matchedGLs[$"{glColumn.GL_Code}"].GuidKey;

                        if (liabilityGLValue != 0)
                        {
                            matchedAndProcessed = true;

                            if (Math.Abs(liabilityGLValue) > Math.Abs(advanceGLAmount))
                            {
                                consumedAmount = -Math.Abs(advanceGLAmount);
                                decimal remainingAmount = liabilityGLValue - consumedAmount;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = remainingAmount;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                            else // Liability amount is less than or equal to Advance GL amount
                            {
                                consumedAmount = liabilityGLValue;
                                matchedGLs[$"{glColumn.GL_Code}"].Amount = 0;
                                newResultRow[$"{glColumn.GL_Code}_Doc"] = consumedAmount;
                            }
                        }
                        else
                        {
                            newResultRow[$"{glColumn.GL_Code}_Doc"] = 0m;
                        }
                    }

                    if (matchedAndProcessed)
                        resultTable.Rows.Add(newResultRow);
                    else
                    {
                        newResultRow["Join_Type_Doc"] = "No Match";
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
                    noMatchRow["Join_Type_Doc"] = "No Match";
                    resultTable.Rows.Add(noMatchRow);
                }
            }

            // Remaining Liability rows are skipped to maintain non-null Grouped_Invoice_Key and Amount_Local.

            return resultTable;
        }



        public static DataTable GITAdvanceManipulationFAGLL03DocCurrData(DataTable GitData, bool isSnaCompany = false)
        {
            var resultTable = HelperFunctions.DeepCopyDataTable(GitData, "Processed FAGLL03 GIT Sheet With Doc Curr");

            // Maintaining original column names
            resultTable.Columns.Add("Adjusted_GL_Doc", typeof(string));
            resultTable.Columns.Add("Adjusted_Amount_Doc", typeof(decimal));
            resultTable.Columns.Add("Total_Adjustment_Doc", typeof(decimal));
            resultTable.Columns.Add("Balance_Doc", typeof(string));

            // Define hierarchies specifically for Doc Currency columns
            string[] standardGLs = { "14005_Doc", "14006_Doc", "14007_Doc", "14701_Doc", "14702_Doc", "14703_Doc", "14704_Doc", "14705_Doc", "14012_Doc", "14021_Doc" };

            // Note: The logic handles SNA exclusion dynamically inside the loop 
            // to match your original requirement of skipping specific indices if !isSnaCompany.

            foreach (DataRow row in resultTable.Rows)
            {
                var vendor = row["Vendor"]?.ToString();
                decimal total_amount_doc = decimal.TryParse(row["Amount_Doc"]?.ToString(), out var val) ? val : 0m;

                row["Total_Adjustment_Doc"] = 0m;
                row["Balance_Doc"] = total_amount_doc.ToString();

                // 1. Validation Logic
                bool isInvalidVendor = string.IsNullOrWhiteSpace(vendor) ||
                                       vendor.Contains("not", StringComparison.OrdinalIgnoreCase) ||
                                       vendor.Contains("null", StringComparison.OrdinalIgnoreCase);

                if (total_amount_doc <= 0 || isInvalidVendor)
                {
                    foreach (var gl in standardGLs)
                    {
                        if (resultTable.Columns.Contains(gl)) row[gl] = "0";
                    }

                    //foreach (var gl in standardGLs.Concat(snaGLs)) row[gl] = "0";

                    row["Adjusted_GL_Doc"] = total_amount_doc <= 0 ? "All GL set to 0, as Advance Amount was negative" : "All GL set to 0, as no Vendor Found!";
                    row["Adjusted_Amount_Doc"] = 0m;
                    continue;
                }

                // 2. Sequential Waterfall Logic
                decimal currentBalance = total_amount_doc;
                decimal adjusted_amount = 0m;

                // Determine list of GLs to process based on SNA flag
                List<string> glToProcess = [];
                foreach (var gl in standardGLs)
                {
                    // Skip SNA GLs if the flag is false
                    if (!isSnaCompany && (gl == "14702_Doc" || gl == "14703_Doc" || gl == "14704_Doc" || gl == "14705_Doc"))
                        continue;

                    glToProcess.Add(gl);
                }

                foreach (string glCode in glToProcess)
                {
                    if (!resultTable.Columns.Contains(glCode)) continue;

                    decimal glValue = HelperFunctions.GetDecimalValue(row, glCode);

                    // Constraint: Ignore positive values (row[glCode] = 0)
                    if (glValue > 0)
                    {
                        row[glCode] = 0;
                        continue;
                    }

                    decimal absGLValue = Math.Abs(glValue);

                    if (absGLValue <= currentBalance && currentBalance > 0)
                    {
                        // Full adjustment possible
                        adjusted_amount += absGLValue;
                        currentBalance -= absGLValue;
                        row["Balance_Doc"] = currentBalance.ToString();
                    }
                    else if (currentBalance > 0)
                    {
                        // Partial adjustment (GL exceeds balance)
                        decimal diff = absGLValue - currentBalance;

                        adjusted_amount += currentBalance;
                        row[glCode] = -currentBalance; // Adjusting the GL value in the row

                        // For documentation purposes
                        row["Adjusted_Amount_Doc"] = diff;
                        row["Adjusted_GL_Doc"] = glCode;

                        currentBalance = 0;
                        row["Balance_Doc"] = "0";

                        // Zero out all subsequent GLs
                        int index = glToProcess.IndexOf(glCode);
                        for (int i = index + 1; i < glToProcess.Count; i++)
                        {
                            if (resultTable.Columns.Contains(glToProcess[i])) row[glToProcess[i]] = 0;
                        }
                        break;
                    }
                    else
                    {
                        // Balance already zero, ensure remaining GLs are cleared
                        row[glCode] = 0;
                    }
                }

                row["Total_Adjustment_Doc"] = adjusted_amount;
            }

            return resultTable;
        }
        private  List<DataRow> PivotDocCurrAdvanceGroup(List<DataRow> advanceGroup)
        {
            if (advanceGroup == null || advanceGroup.Count == 0)
                return new List<DataRow>(); // Return empty if input is empty

            DataRow firstRow = advanceGroup[0];

            DataTable resultTable = new("Pivoted Advance Group");

            List<string> ColumnsToPivot = ["GL_Account", "Advance_Adjustment_Doc"];

            // a. Add all common columns from the original rows (except the pivot columns)
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
                string advanceGL = row[ColumnsToPivot[0]]?.ToString()!;
                decimal advanceAdjusted = Convert.ToDecimal(row[ColumnsToPivot[1]]);
                totalAdvanceAmount += advanceAdjusted;

                if (resultTable.Columns.Contains(advanceGL))
                    pivotedRow[advanceGL] = advanceAdjusted;
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Advance GL {advanceGL} does not exist in AdvanceGLs List");
                }
            }
            pivotedRow["TotalAdvanceAmount"] = totalAdvanceAmount;
            resultTable.Rows.Add(pivotedRow);

            return [.. resultTable.AsEnumerable()];
        }
        public DataTable DocCurrLineItemWiseAdvanceAdjustment(DataTable populatedData, DataTable LiabilityData)
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
                var pKey = pRow.Field<Guid>("Grouped_Invoice_Key_Original_Doc");

                var advances = consumableLiabilityList
                    .Where(lRow => lRow.Field<Guid>("Grouped_Key_Doc") == pKey)
                    .ToList();

                DataRow? pivotedAdvanceRow = PivotDocCurrAdvanceGroup(advances).FirstOrDefault();
                var newRow = new Dictionary<string, object>();

                foreach (DataColumn column in pRow.Table.Columns)
                {
                    newRow.Add(column.ColumnName, pRow[column.ColumnName] == DBNull.Value ? null : pRow[column.ColumnName]);
                }

                newRow["Amount_Doc"] = pRow.Field<decimal?>("Amount_Doc") ?? 0m;

                if (pivotedAdvanceRow != null)
                {
                    foreach (DataColumn column in pivotedAdvanceRow.Table.Columns)
                    {
                        List<string> IgnoreColumns = new List<string> {
                    "Grouped_Invoice_Key", "Join_Type", "Liability_GL_Code", "Grouped_Key",
                    "Grouped_Key_Doc", "Invoice_Key","Grouped_Invoice_Key_Doc",
                    "Join_Type_Doc", "Liability_GL_Code", "Grouped_Invoice_Key_Original_Doc"
                };

                        string columnName = column.ColumnName;
                        if (pRow.Table.Columns.Contains(columnName) || (IgnoreColumns.Contains(columnName) && columnName != "TotalAdvanceAmount"))
                            continue;

                        string newColName = "Advance_" + columnName + "_Doc";
                        decimal advanceValue = pivotedAdvanceRow.Field<decimal?>(columnName) ?? 0m;

                        newRow[newColName] = advanceValue;
                    }
                }
                else
                {
                    foreach (var advanceGL in _helper.AdvanceGLs)
                        newRow.Add("Advance_" + advanceGL.GL_Code + "_Doc", 0.0m);
                    newRow.Add("Advance_TotalAdvanceAmount_Doc", 0.0m);
                }

                joinedData.Add(newRow);
            }

            // --- STEP 2: GROUP BY INVOICE AND APPLY ADJUSTMENT LOGIC ---
            var finalAdjustedData = new List<dynamic>();
            var groupedInvoices = joinedData.GroupBy(i => i["Grouped_Invoice_Key_Original_Doc"]);

            foreach (var group in groupedInvoices)
            {
                // Sort by the absolute value of the amount (largest items first)
                var sortedLineItems = group.OrderByDescending(i => Math.Abs(Convert.ToDecimal(i["Amount_Doc"]))).ToList();

                // Initial remaining advance taken from the first item (since it's grouped)
                decimal remainingAdvance = 0m;
                if (sortedLineItems.Count > 0 && sortedLineItems.First().ContainsKey("Advance_TotalAdvanceAmount_Doc"))
                {
                    remainingAdvance = Convert.ToDecimal(sortedLineItems.First()["Advance_TotalAdvanceAmount_Doc"]);
                }

                foreach (var item in sortedLineItems)
                {
                    decimal advanceApplied = 0.0m;
                    decimal lineItemAmnt = Convert.ToDecimal(item["Amount_Doc"]);
                    string adjustmentType = "";

                    // Identify company type and GL for the specific line item
                    string icpName = item.ContainsKey("ICP_Name") ? item["ICP_Name"]?.ToString() ?? "" : "";
                    string currentGL = item.ContainsKey("GL_Account") ? item["GL_Account"]?.ToString() ?? "" : "";

                    bool isSNACompany = !string.IsNullOrWhiteSpace(icpName) &&
                                        !icpName.Equals("null", StringComparison.OrdinalIgnoreCase) &&
                                        !icpName.Equals("not", StringComparison.OrdinalIgnoreCase);

                    // Eligibility Rules
                    bool isEligibleForAdjustment = true;

                    if (currentGL.StartsWith("2"))
                    {
                        isEligibleForAdjustment = false;
                    }
                    else if (!isSNACompany && snaLiabilityGLs.Contains(currentGL))
                    {
                        isEligibleForAdjustment = false;
                    }

                    // Calculation Logic
                    if (remainingAdvance > 0 && isEligibleForAdjustment)
                    {
                        if (lineItemAmnt < 0)
                        {
                            decimal lineItemAbs = Math.Abs(lineItemAmnt);
                            advanceApplied = Math.Min(remainingAdvance, lineItemAbs);
                            remainingAdvance -= advanceApplied;
                            adjustmentType = "Advance Adjustment Doc Currency";
                        }
                    }

                    // Build the final result
                    var finalRow = new Dictionary<string, object>();
                    foreach (var kvp in item)
                    {
                        finalRow.TryAdd(kvp.Key, kvp.Value);
                    }

                    finalRow.Add("Advance_Applied_Doc", advanceApplied);
                    finalRow.Add("Amount_Doc_Adjusted", lineItemAmnt + advanceApplied);
                    finalRow.Add("Adjustment_Type", adjustmentType);
                    finalRow.Add("Total_Remaining_Advance_Doc", remainingAdvance);

                    finalAdjustedData.Add(finalRow);
                }
            }

            return ConvertToAdjustedDataTableFromDictionaryList(finalAdjustedData);
        }

        // Unpivot Processed GIT
        public static DataTable UnpivotProcessedGIT2(DataTable processedGIT)
        {
            // 1. Define the structure for the output DataTable
            DataTable outputTable = new("Unpivoted_ProcessedGIT_DocCurr");

            // Add original columns
            outputTable.Columns.Add("Grouped_Invoice_Key_Doc", typeof(Guid)); // Assuming string/guid
            outputTable.Columns.Add("Purchasing_Document", typeof(string));
            outputTable.Columns.Add("Vendor", typeof(string));
            outputTable.Columns.Add("Company_Code", typeof(string));
            outputTable.Columns.Add("GL_Account", typeof(string));
            outputTable.Columns.Add("Profit_Center", typeof(string));
            outputTable.Columns.Add("Amount_Doc", typeof(decimal));
            outputTable.Columns.Add("Report_Date", typeof(DateTime));
            outputTable.Columns.Add("Join_Type_Doc", typeof(string));
            outputTable.Columns.Add("Total_Adjustment_Doc", typeof(decimal));
            outputTable.Columns.Add("Balance_Doc", typeof(decimal));

            // Add unpivoted/calculated columns
            outputTable.Columns.Add("Liability_GL_Code", typeof(string));
            outputTable.Columns.Add("Liability_Amount", typeof(decimal));
            outputTable.Columns.Add("Grouped_Key_Doc", typeof(Guid)); // The unpivoted Grouped_Key
            outputTable.Columns.Add("Advance_Adjustment_Doc", typeof(decimal)); // ABS(Liability_Amount)

            // Define the list of columns to be unpivoted (Liability Amount, Grouped Key)
            var glColumnMap = new[]
            {
                new { GLCode = "14005", AmountCol = "14005_Doc", KeyCol = "Grouped_Key_14005_Doc" },
                new { GLCode = "14006", AmountCol = "14006_Doc", KeyCol = "Grouped_Key_14006_Doc" },
                new { GLCode = "14007", AmountCol = "14007_Doc", KeyCol = "Grouped_Key_14007_Doc" },
                new { GLCode = "14012", AmountCol = "14012_Doc", KeyCol = "Grouped_Key_14012_Doc" },
                new { GLCode = "14021", AmountCol = "14021_Doc", KeyCol = "Grouped_Key_14021_Doc" },
                new { GLCode = "14701", AmountCol = "14701_Doc", KeyCol = "Grouped_Key_14701_Doc" },

                // Special Liability GLs for S&A Comapnies only
                new { GLCode = "14702", AmountCol = "14702_Doc", KeyCol = "Grouped_Key_14702_Doc" },
                new { GLCode = "14703", AmountCol = "14703_Doc", KeyCol = "Grouped_Key_14703_Doc" },
                new { GLCode = "14704", AmountCol = "14704_Doc", KeyCol = "Grouped_Key_14704_Doc" },
                new { GLCode = "14705", AmountCol = "14705_Doc", KeyCol = "Grouped_Key_14705_Doc" }
            };





            // 2. Use LINQ to DataSet to process the data (replicates the CROSS APPLY)
            var results =
                from row in processedGIT.AsEnumerable()
                    // CROSS APPLY is replicated by selecting all GL mappings for each source row
                from glMap in glColumnMap
                let liabilityAmount = row.Field<decimal?>(glMap.AmountCol) ?? 0m
                let groupedKey = row.Field<Guid?>(glMap.KeyCol) ?? Guid.Empty

                // WHERE pvt.Liability_Amount <> 0
                where liabilityAmount != 0m

                select new
                {
                    SourceRow = row,
                    GLMap = glMap,
                    LiabilityAmount = liabilityAmount,
                    GroupedKey = groupedKey
                };

            // 3. Populate the output DataTable
            foreach (var item in results)
            {
                DataRow newRow = outputTable.NewRow();

                // Populate base columns from the original row (t.*)
                newRow["Grouped_Invoice_Key_Doc"] = item.SourceRow["Grouped_Invoice_Key_Doc"];
                newRow["Purchasing_Document"] = item.SourceRow["Purchasing_Document"];
                newRow["Vendor"] = item.SourceRow["Vendor"];
                newRow["Company_Code"] = item.SourceRow["Company_Code"];
                newRow["GL_Account"] = item.SourceRow["GL_Account"];
                newRow["Profit_Center"] = item.SourceRow["Profit_Center"];
                newRow["Amount_Doc"] = item.SourceRow["Amount_Doc"];
                newRow["Report_Date"] = item.SourceRow["Report_Date"];
                newRow["Join_Type_Doc"] = item.SourceRow["Join_Type_Doc"];
                newRow["Total_Adjustment_Doc"] = item.SourceRow["Total_Adjustment_Doc"];
                newRow["Balance_Doc"] = item.SourceRow["Balance_Doc"];

                // Populate unpivoted columns (pvt.*)
                newRow["Liability_GL_Code"] = item.GLMap.GLCode;
                newRow["Liability_Amount"] = item.LiabilityAmount;
                newRow["Grouped_Key_Doc"] = item.GroupedKey;

                // Populate calculated column (ABS(pvt.Liability_Amount) AS Adjusted_Amount)
                newRow["Advance_Adjustment_Doc"] = Math.Abs(item.LiabilityAmount);

                outputTable.Rows.Add(newRow);
            }

            return outputTable;
        }

        // Business Logic Functions
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
    }
}
