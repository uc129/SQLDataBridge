using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Graph.Models;
using System.Data;


namespace Application.Services
{
    public class ExcelReaderService
    {
        public static DataTable ReadExcelToDataTable(Stream fileStream, string sheetname)
        {
            var dataTable = new DataTable();

            using (var workbook = new XLWorkbook(fileStream))
            {
                // Check if the workbook contains the requested sheet name
                if (!workbook.Worksheets.Contains(sheetname))
                {
                    throw new Exception($"The worksheet '{sheetname}' was not found in the Excel file.");
                }

                var worksheet = workbook.Worksheet(sheetname); // Access by name instead of index

                bool firstRow = true;
                foreach (IXLRow row in worksheet.RowsUsed())
                {
                    if (firstRow)
                    {
                        foreach (IXLCell cell in row.Cells())
                        {
                            // Ensure unique column names to avoid DataTable errors
                            string colName = cell.Value.ToString() ?? $"Column_{dataTable.Columns.Count}";
                            dataTable.Columns.Add(colName);
                        }
                        firstRow = false;
                    }
                    else
                    {
                        var dataRow = dataTable.NewRow();
                        int i = 0;

                        // Read from cell 1 to the count of defined columns
                        foreach (IXLCell cell in row.Cells(1, dataTable.Columns.Count))
                        {
                            dataRow[i] = cell.Value.ToString();
                            i++;
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
            }
            return dataTable;
        }
    }
}
