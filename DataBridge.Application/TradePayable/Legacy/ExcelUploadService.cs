using Domain.Aggregates;
using Domain.Aggregates.ExcelUpload;
using Domain.Models.DataUpload;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using System.Data;
namespace Application.Services
{
    public class ExcelUploadService(
            IExcelUploadRepository exceluploadrepo,
            IUploadAuditRepository uploadauditrepo,
            ISPStorageUtility sputility,
            IDataSettings datasettings
        )
    {
        private readonly IExcelUploadRepository _exceluploadrepo = exceluploadrepo;
        private readonly IUploadAuditRepository _uploadauditrepo = uploadauditrepo;
        private readonly ISPStorageUtility _sputility = sputility;
        private readonly IDataSettings _datasettings = datasettings;
        private static readonly string[] invalidDocTypes = ["KG", "KV", "KL"];
        private static readonly string[] localcurrCompanyCodes = ["1000", "2000", "3000", "4000", "6000", "C100", "A000"];

        public async Task<Message> UploadFAGLL03RawData(DataTable dt, string revision, DateTime quarterDate)
        {
            // --- PHASE 1: DIRECT MAPPING TO EXCEL ENTITY ---
            // Define the exact column names expected in the Excel sheet for FAGLL03ExcelEntity
            var expectedExcelColumns = new[]
            {
                "Purchasing Document", "Document Header Text", "Assignment", "Reference", "Vendor",
                "Text", "Vendor/Customer Description", "G/L Account", "G/L Description", "Company Code",
                "User Name", "Amount in Local Currency", "Valuated Amt in LC 3", "Document Type",
                "Document Number", "Industry", "Profit Center", "Document Date", "Posting Date",
                "Net Due Date", "Document Currency", "Amount in Doc. Curr."
            };

            // 1. Validate that all required columns exist in the DataTable
            var existingColumns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var missingColumns = expectedExcelColumns.Where(col => !existingColumns.Contains(col)).ToList();

            if (missingColumns.Count != 0)
            {
                return new Message
                {
                    Success = false,
                    Text = $"Excel file is missing required columns: {string.Join(", ", missingColumns)}"
                };
            }

            var excelEntities = new List<FAGLL03ExcelEntity>();

            foreach (DataRow row in dt.Rows)
            {
                // Apply Null Checks / Skipping Logic
                // In your SQL: WHERE [Document Type] NOT IN ('KG', 'KV', 'KL') AND [G/L Account] <> '' AND [G/L Account] <> '14724'
                var docType = row["Document Type"]?.ToString();
                var glAcc = row["G/L Account"]?.ToString();

                if (invalidDocTypes.Contains(docType)) continue;
                if (string.IsNullOrWhiteSpace(glAcc) || glAcc == "14724") continue;

                var excelRow = new FAGLL03ExcelEntity
                {
                    PurchasingDocument = GetNullIfEmpty(row["Purchasing Document"]),
                    DocumentHeaderText = GetNullIfEmpty(row["Document Header Text"]),
                    Assignment = GetNullIfEmpty(row["Assignment"]),
                    Reference = GetNullIfEmpty(row["Reference"]),
                    Vendor = GetNullIfEmpty(row["Vendor"]),
                    Text = GetNullIfEmpty(row["Text"]),
                    VendorCustomerDescription = GetNullIfEmpty(row["Vendor/Customer Description"]),
                    GLAccount = glAcc,
                    GLDescription = GetNullIfEmpty(row["G/L Description"]),
                    CompanyCode = GetNullIfEmpty(row["Company Code"]),
                    UserName = GetNullIfEmpty(row["User Name"]),
                    AmountInLocalCurrency = ParseDecimal(row["Amount in Local Currency"].ToString() ?? ""),
                    ValuatedAmtInLC3 = ParseDecimal(row["Valuated Amt in LC 3"].ToString() ?? ""),
                    DocumentType = docType,
                    DocumentNumber = GetNullIfEmpty(row["Document Number"]),
                    Industry = GetNullIfEmpty(row["Industry"]),
                    ProfitCenter = GetNullIfEmpty(row["Profit Center"]),
                    DocumentDate = GetNullIfEmpty(row["Document Date"]),
                    PostingDate = GetNullIfEmpty(row["Posting Date"]),
                    NetDueDate = GetNullIfEmpty(row["Net Due Date"]),
                    DocumentCurrency = GetNullIfEmpty(row["Document Currency"]),
                    AmountInDocCurr = ParseDecimal(row["Amount in Doc. Curr."].ToString()??"")
                };

                excelEntities.Add(excelRow);
            }

            // --- PHASE 2: MAP TO RAW ENTITY BASED ON LOGIC ---
            var rawList = excelEntities.Select(ex => new FAGLL03RAWEntity
            {
                Invoice_Key = Guid.NewGuid(),
                RevisionNumber = revision,
                QuarterEndDate = quarterDate,
                UploadedDate = DateTime.Now,
                SOURCE = "FAGLL03 Raw Excel Upload",
                Report_Date = DateTime.Now,
                Edited = "False",

                // Logic Mappings
                Document_Number = ex.DocumentNumber,
                Purchasing_Document = ex.PurchasingDocument,
                Document_Header = ex.DocumentHeaderText,
                Assignment = ex.Assignment,
                Invoice_Reference = ex.Reference,
                Vendor = ex.Vendor,
                Invoice_Description = ex.Text,
                Vendor_Description = ex.VendorCustomerDescription,
                GL_Account = ex.GLAccount,
                GL_Description = ex.GLDescription,
                Company_Code = ex.CompanyCode,
                User_Name = ex.UserName,
                Document_Type = ex.DocumentType,
                Industry = ex.Industry,
                Profit_Center = ex.ProfitCenter,
                Document_Currency = ex.DocumentCurrency,
                Payment_Terms = ex.Payment_Terms ??null,

                // Conditional Logic for Amount_Local
                Amount_Local = (localcurrCompanyCodes.Contains(ex.CompanyCode))
                                ? ex.AmountInLocalCurrency!.Value
                                : ex.ValuatedAmtInLC3!.Value,

                Amount_Doc = ex.AmountInDocCurr!.Value,

                // Date Parsing (mimicking TRY_CONVERT 105 - dd-mm-yyyy)
                Document_Date = ParseDateExact(ex.DocumentDate),
                Posting_Date = ParseDateExact(ex.PostingDate),
                Payment_Date = ParseDateExact(ex.NetDueDate)
            }).ToList();
            return await _exceluploadrepo.ReplaceRAWFAGLL03RevisionData(rawList, revision, quarterDate);
        }
        public async Task<string> UploadFileToSharepoint(IFormFile file, DateTime Quarter_Date, string username, string spolibname)
        {
            string year = Quarter_Date.Year.ToString();
            string quarter = Quarter_Date.ToString("yyyy-MM-dd");
            string folderPath = $"{year}/{quarter}/Excel_Upload_Data";
            try
            {
                var uploadMessage = await _sputility.UploadFile(file, folderPath, username, spolibname);
                var absoluteURI = uploadMessage.TempValue;

                var uploadAudit = new DataUploadAuditEntity
                {
                    UploadType = "FAGLL03 Raw Data Upload",
                    UploadedBy = username,
                    UploadedDate = DateTime.Now,
                    SourceFileName = file.FileName,
                    FileURL = absoluteURI,
                    QuarterEndDate = Quarter_Date,
                    AuditId = new Guid(),
                    FileSizeKB = (int)(file.Length / 1024),
                    IsActive = true,
                    RecordCount = null,
                    TargetSqlTableName = _datasettings.TradePayable_RawDataTableName,
                    RevisionNumber = ""
                };
                await _uploadauditrepo.LogUploadAsync(uploadAudit);
                return absoluteURI; // Return the resulting link
            }

            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("exception occured " + ex.Message);
                return "";
            }
        }



        // Helper methods
        private static string? GetNullIfEmpty(object? val)
        {
            if (val == null || val == DBNull.Value) return null;
            if (val is DateTime dt) return dt.ToString("dd.MM.yyyy");
            string str = val.ToString()?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }
        private static DateTime? ParseDateExact(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;

            // Define the formats that match your Excel data
            string[] formats = {
                "dd-MM-yyyy HH:mm:ss", // Matches "02-03-2011 00:00:00"
                "dd-MM-yyyy",
                "dd.MM.yyyy",          // Backup for other variations
                "yyyy-MM-dd HH:mm:ss"
            };

            if (DateTime.TryParseExact(dateStr, formats,
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None,
                                       out DateTime dt))
            {
                return dt;
            }

            // Fallback to general parsing if Exact fails
            if (DateTime.TryParse(dateStr, out DateTime fallbackDt))
                return fallbackDt;

            return null;
        }
        private static decimal ParseDecimal(string s) => decimal.TryParse(s, out var d) ? d : 0;
    }
}
