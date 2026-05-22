using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Data_Cleaning
{
    public static class ReportingLogicFunctions
    {
        public static DataTable MSMEData(DataTable originalTable)
        {
            var filteredRows = from row in originalTable.AsEnumerable()
                               let hyperionCode = row.Field<string>("Hyperion_Code") // Get the string value
                               where hyperionCode != null && // Check for null before comparison
                                     (hyperionCode.Equals("2D170100") ||
                                      hyperionCode.Equals("2D170200") ||
                                      hyperionCode.Equals("2D190510"))
                               select row;

            // Create a new DataTable from the filtered rows
            if (filteredRows.Any())
            {
                DataTable MSMEData = filteredRows.CopyToDataTable();
                //DataTableExtensions.PrintColumnNames(MSMEData);

                return MSMEData;
            }
            else
            {
                // Return an empty DataTable with the same schema if no rows match
                return originalTable.Clone();
            }
        } //step 20
        public static DataTable GetMSMEReport(DataTable MSMEData)
        {
            // 1. Group by Hyperion_Code and sum Amount_Local
            var groupedResults = from row in MSMEData.AsEnumerable()
                                 group row by row.Field<string>("Hyperion_Code") into g
                                 select new
                                 {
                                     HyperionCode = g.Key,
                                     HyperionDescription = g.Max(r => r.Field<string>("Hyp_Code_Description") ?? ""),
                                     SumAmount = g.Sum(r => {
                                         decimal value = 0;
                                         // Get the value as an object first to handle DBNull explicitly
                                         object amountObj = r["Amount_Local"];

                                         if (amountObj != DBNull.Value && amountObj != null)
                                         {
                                             // Try to parse it to decimal. If it fails, 'value' remains 0.
                                             decimal.TryParse(amountObj.ToString(), out value);
                                         }
                                         return value;
                                     })
                                 };

            // Create a new DataTable for the pivoted results
            DataTable pivotTable = new DataTable("PivotResults");
            pivotTable.Columns.Add("Hyperion_Code", typeof(string));
            pivotTable.Columns.Add("Hyp_Code_Description", typeof(string));
            pivotTable.Columns.Add("Sum_Amount_Local", typeof(decimal));

            // Populate the pivot table with grouped results
            foreach (var item in groupedResults.OrderBy(x => x.HyperionCode)) // Optional: Order by Hyperion Code
            {
                pivotTable.Rows.Add(item.HyperionCode, item.HyperionDescription, item.SumAmount);
            }

            // 2. Calculate the Grand Total
            decimal grandTotal = pivotTable.AsEnumerable().Sum(row => row.Field<decimal>("Sum_Amount_Local"));

            // Add the Grand Total row
            pivotTable.Rows.Add("", "Grand Total", grandTotal);

            return pivotTable;
        } //step 21
        public static DataTable HyperionWiseBalances(DataTable data)
        {

            var hyperionWise = from row in data.AsEnumerable()
                               group row by row.Field<string>("Hyperion_Code") into g
                               select new
                               {
                                   Hyperion_Code = g.Key,
                                   Hyperion_Description = g.Max(r => r.Field<string>("Hyp_Code_Description") ?? "null"),
                                   //Adjustment = g.Max(r => r.Field<string>("Adjustment")?? "null"),
                                   SumAmount = g.Sum(r => {
                                       decimal value = 0;
                                       // Get the value as an object first to handle DBNull explicitly
                                       object amountObj = r["Amount_Local"];

                                       if (amountObj != DBNull.Value && amountObj != null)
                                       {
                                           // Try to parse it to decimal. If it fails, 'value' remains 0.
                                           decimal.TryParse(amountObj.ToString(), out value);
                                       }
                                       return value;
                                   })
                               };

            DataTable HyperionGroup = new DataTable("Hyperion Wise Grouping");
            HyperionGroup.Columns.Add("Hyperion_Code", typeof(string));
            HyperionGroup.Columns.Add("Hyperion_Description", typeof(string));
            //HyperionGroup.Columns.Add("Adjustment", typeof(string));
            HyperionGroup.Columns.Add("Sum_Amount_Local", typeof(decimal));

            foreach (var item in hyperionWise.OrderBy(x => x.Hyperion_Code))
                HyperionGroup.Rows.Add(item.Hyperion_Code, item.Hyperion_Description, item.SumAmount);

            decimal grandTotal = HyperionGroup.AsEnumerable().Sum(row => row.Field<decimal>("Sum_Amount_Local"));
            HyperionGroup.Rows.Add("", "Grand Total", grandTotal);

            return HyperionGroup;

        } //step 22
        public static List<DataTable> GLGrouping(DataTable originalTable, DataTable HyperionTable)
        {

            var groupedResultsBeforeGIT = from row in originalTable.AsEnumerable()
                                          group row by row.Field<string>("GL_Account") into g
                                          select new
                                          {
                                              GL_Account = g.Key,
                                              GL_Description = g.Max(r => r.Field<string>("GL_Description") ?? ""),
                                              SumAmount = g.Sum(r => {
                                                  decimal value = 0;
                                                  // Get the value as an object first to handle DBNull explicitly
                                                  object amountObj = r["Amount_Local"];

                                                  if (amountObj != DBNull.Value && amountObj != null)
                                                  {
                                                      // Try to parse it to decimal. If it fails, 'value' remains 0.
                                                      decimal.TryParse(amountObj.ToString(), out value);
                                                  }
                                                  return value;
                                              })
                                          };

            DataTable beforeGITGroup = new DataTable("grouping of GLs before GIT");
            beforeGITGroup.Columns.Add("GL_Account", typeof(string));
            beforeGITGroup.Columns.Add("GL_Description", typeof(string));
            beforeGITGroup.Columns.Add("Sum_Amount_Local", typeof(decimal));

            foreach (var item in groupedResultsBeforeGIT.OrderBy(x => x.GL_Account))
                beforeGITGroup.Rows.Add(item.GL_Account, item.GL_Description, item.SumAmount);

            decimal grandTotalBeforeGIT = beforeGITGroup.AsEnumerable().Sum(row => row.Field<decimal>("Sum_Amount_Local"));
            beforeGITGroup.Rows.Add("", "Grand Total", grandTotalBeforeGIT);


            //Grouping of all data after Advance GIT
            var groupedResultsAfterGIT = from row in HyperionTable.AsEnumerable()
                                         group row by row.Field<string>("GL_Account") into g
                                         select new
                                         {
                                             GL_Account = g.Key,
                                             GL_Description = g.Max(r => r.Field<string>("GL_Description") ?? ""),
                                             SumAmount = g.Sum(r =>
                                             {
                                                 decimal value = 0;
                                                 // Get the value as an object first to handle DBNull explicitly
                                                 object amountObj = r["Amount_Local"];

                                                 if (amountObj != DBNull.Value && amountObj != null)
                                                 {
                                                     // Try to parse it to decimal. If it fails, 'value' remains 0.
                                                     decimal.TryParse(amountObj.ToString(), out value);
                                                 }
                                                 return value;
                                             })
                                         };

            DataTable afterGITGroup = new DataTable("grouping of GLs after GIT Advance");
            afterGITGroup.Columns.Add("GL_Account", typeof(string));
            afterGITGroup.Columns.Add("GL_Description", typeof(string));
            afterGITGroup.Columns.Add("Sum_Amount_Local", typeof(decimal));

            foreach (var item in groupedResultsAfterGIT.OrderBy(x => x.GL_Account))
                afterGITGroup.Rows.Add(item.GL_Account, item.GL_Description, item.SumAmount);

            decimal grandTotalAfterGIT = afterGITGroup.AsEnumerable().Sum(row => row.Field<decimal>("Sum_Amount_Local"));
            afterGITGroup.Rows.Add("", "Grand Total", grandTotalAfterGIT);

            List<DataTable> DataTableArray = new List<DataTable>() { beforeGITGroup, afterGITGroup };

            return DataTableArray;
        }  //step23
        public static DataTable GroupDataByPOVendorAndGL(DataTable processedData)
        {

            var hyperionWise = from row in processedData.AsEnumerable()
                               group row by new
                               {
                                   Purchasing_Document = row.Field<string>("Purchasing_Document"),
                                   Vendor = row.Field<string>("Vendor"),
                                   GL_Account = row.Field<string>("GL_Account")
                               } into g
                               select new
                               {
                                   g.Key.Purchasing_Document,
                                   g.Key.Vendor,
                                   g.Key.GL_Account,
                                   SumAmount = g.Sum(r => {
                                       decimal value = 0;
                                       object amountObj = r["Amount_Local"];
                                       if (amountObj != DBNull.Value && amountObj != null)
                                           decimal.TryParse(amountObj.ToString(), out value);
                                       return value;
                                   })

                               };


            DataTable GroupedData = new DataTable();

            return GroupedData;

        }  //step 24
    }
}
