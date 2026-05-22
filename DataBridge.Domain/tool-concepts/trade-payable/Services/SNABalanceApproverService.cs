using Application.Extensions;
using Application.Services.MasterTableServices;
using Domain.Aggregates;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Contracts.ServiceContracts;
using Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Shared.Extensions;
using System.Data;

namespace Application.Services
{
    public class SNABalanceApproveService(
        //DapperContext dbcontext, 
        ISNABalanceApproveRepository snarepo,
        StaticMasterTableService master_service,
        IDataSettings datasettings,
        DataToDB datahelper,
        ISPStorageUtility sputility,
        ITradePayableRepository tradepayablerepo
        ) :ISNABalanceApproveService
    {

        //private readonly DapperContext _dbcontext = dbcontext;
        private readonly ISNABalanceApproveRepository _snarepo = snarepo;
        private readonly IDataSettings _datasettings = datasettings;
        private readonly DataToDB _datahelper = datahelper;
        private readonly StaticMasterTableService _master_service = master_service;
        private readonly ISPStorageUtility _sputility = sputility;
        private readonly ITradePayableRepository _tradepayablerepo = tradepayablerepo;

        // Save Data
        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> SaveSNABalanceData(IEnumerable<FAGLL03ProcessedResult> resultdata, Guid processId)
        {
            var vendordata = await _master_service.GetICPVendorMap();
            ArgumentNullException.ThrowIfNull(resultdata);

            // 1. Convert Vendor Data to a Dictionary for high-performance lookups
            // We use First() in case there are duplicate vendor codes in the mapping table
            var vendorLookup = vendordata
                .Where(v => !string.IsNullOrEmpty(v.Vendor_Code))
                .GroupBy(v => v.Vendor_Code.Trim())
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            // 2. Group and Aggregate the data
            var groupedData = resultdata
                .GroupBy(x => new
                {
                    x.Vendor,
                    x.Vendor_Description,
                    x.ICP_Name,
                    x.ICP_Hyperion,
                    //x.Document_Number,
                    //x.User_Name,
                    x.RevisionNumber,
                    x.Document_Currency,
                    x.QuarterEndDate,
                    x.Exchange_Rate,
                    x.Company_Code,
                    x.Base_Hyperion_Code
                })
                .Select(g =>
                {
                    string vendorKey = g.Key.Vendor?.Trim() ?? string.Empty;
                    vendorLookup.TryGetValue(vendorKey, out var mapping);

                    return new FAGLL03SNABalanceApproveEntity
                    {
                        SNAGroupedInvoiceKey = Guid.NewGuid(),
                        Vendor = g.Key.Vendor ?? null,
                        Vendor_Description = g.Key.Vendor_Description ?? null,
                        ICP_Name = g.Key.ICP_Name ?? null,
                        ICP_Hyperion = g.Key.ICP_Hyperion ?? null,
                        //Document_Number = g.Key.Document_Number ?? null,
                        //User_Name = g.Key.User_Name ?? null,
                        RevisionNumber = g.Key.RevisionNumber ?? null,
                        Document_Currency = g.Key.Document_Currency ?? null,
                        Quarter_Date = g.Key.QuarterEndDate!.Value,
                        Exchange_Rate = g.Key.Exchange_Rate,
                        Company_Code = g.Key.Company_Code ?? null,
                        Base_Hyperion = g.Key.Base_Hyperion_Code ?? null,

                        // Aggregated Financial Totals
                        Total_Amount_Local_SAP = g.Sum(x => x.Amount_Local ??0m),
                        Total_Advance_Applied_Local = g.Sum(x => x.Advance_Applied),
                        Net_Amount_Local = g.Sum(x => x.Amount_Local_Adjusted),
                        Total_Amount_Doc = g.Sum(x => x.Amount_Doc ?? 0m),
                        Total_Advance_Applied_Doc = g.Sum(x => x.Advance_Applied_Doc),
                        Net_Amount_Doc = g.Sum(x => x.Amount_Doc_Adjusted),
                        Net_Amount_Doc_INR = g.Sum(x => x.Amount_Doc_Adjusted_INR),
                        Net_Amount_Doc_ERV = g.Sum(x => x.Amount_Doc_Adjusted_ERV),

                        // Map Approver Details from Dictionary (using null-propagation)
                        Approver_PSNO = mapping?.Approver_PSNO ?? "NOT_FOUND",
                        Approver_Name = mapping?.Approver_Name ?? "Approver Not Assigned",

                        // Default Status
                        Balance_Approved_Local = false,
                        Balance_Approved_Doc = false
                    };
                })
                .Where(r => !string.IsNullOrEmpty(r.ICP_Name))
                .ToList();

            // 3. Prepare for DB Save
            var dbName = _datasettings.GetTableConfigDataByKey("TradePayableDbName");
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");
            var backupTable = _datasettings.GetTableConfigDataByKey("Backup_SNA_Balance_Summary");

            var SNADataTable = groupedData.ToDataTable();

            try
            {
                var latestDataQuery = await _datahelper.ProcessAndSaveLargeData(SNADataTable, tableName);
                var backupDataQuery = await _datahelper.ProcessAndInsertData(SNADataTable, backupTable, dbName, processId, 44);

                if (latestDataQuery && backupDataQuery.Success)
                {
                    return groupedData;
                }
                else
                {
                    return [];
                }
                //var saveMessage = await _snarepo.SaveSNABalanceDataAsync(groupedData);
                //if (saveMessage.Success)
                //{
                //    return groupedData;
                //}
                //return [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SaveSNABalanceData: {ex.Message}");
                return [];
            }
        }

        // Retrieve Data
        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetLatestSNABalanceData()
        {
           var data = await _snarepo.GetLatestSNADataAsync();
            return data;
        }
        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetSNABalanceForAllRunsByQuarterDate(DateTime Quarterdate)
        {
            var data = await _snarepo.GetAllForQuarter(Quarterdate);
            return data;
        }

        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetSNABalanceDataByGroupedKey(Guid groupedKey)
        {
            var data = await _snarepo.GetByGroupId(groupedKey);
            return data;
        }



        // comapre with previous revisions and history.

        /// <summary>
        /// Before saving SNA Balance the approvals from previous revisins and history is comapred and status is retained.
        /// </summary>
        /// <param name="latestData"></param>
        /// <param name="quarterDate"></param>
        /// <returns>true/false</returns>
        public async Task<bool> ProcessAndSaveLatestApprovals(IEnumerable<FAGLL03SNABalanceApproveEntity> latestData, DateTime quarterDate)
        {
            var processedData = await CompareSNABalanceAndApprovalStatus(latestData, quarterDate);
            var resultCount = await _snarepo.UpdateApprovalStatusAsync(processedData);
            return resultCount > 0;
        }



        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> CompareSNABalanceAndApprovalStatus(IEnumerable<FAGLL03SNABalanceApproveEntity> latestData, DateTime QuarterDate)
        {
            var allHistoricalData = await _snarepo.GetAllForQuarter(QuarterDate);

            // 1. Initial Pass: Carry-forward from DB history to the latest records
            foreach (var current in latestData)
            {
                var historyMatch = allHistoricalData
                    .Where(h => h.Vendor == current.Vendor &&
                                h.ICP_Name == current.ICP_Name &&
                                h.ICP_Hyperion == current.ICP_Hyperion &&
                                h.Document_Currency == current.Document_Currency &&
                                h.Balance_Approved_Doc == true &&
                                h.Net_Amount_Doc == current.Net_Amount_Doc)
                    .OrderByDescending(h => h.RevisionNumber)
                    .FirstOrDefault();

                if (historyMatch != null)
                {
                    ApplyApproval(current, historyMatch, $"Inherited from historical {historyMatch.RevisionNumber}");
                }
            }

            // 2. Cross-Revision Sync: Ensure R01, R02, R03 in the 'latestData' 
            // match each other if they share the same balance.
            var groups = latestData.GroupBy(x => new { x.Vendor, x.ICP_Name, x.ICP_Hyperion, x.Document_Currency });

            foreach (var group in groups)
            {
                // If any revision in this group is approved, find the "master" approval info
                var approvedVersion = group.FirstOrDefault(x => x.Balance_Approved_Doc);

                if (approvedVersion != null)
                {
                    foreach (var item in group)
                    {
                        // Only sync if the balance is actually the same
                        if (item.Net_Amount_Doc == approvedVersion.Net_Amount_Doc)
                        {
                            ApplyApproval(item, approvedVersion, $"Synced with {approvedVersion.RevisionNumber}");
                        }
                    }
                }
            }

            return latestData;
        }
        private static void ApplyApproval(FAGLL03SNABalanceApproveEntity target, FAGLL03SNABalanceApproveEntity source, string comment)
        {
            target.Balance_Approved_Doc = true;
            target.Balance_Approved_Local = true;
            target.Approval_Date_Doc = source.Approval_Date_Doc;
            target.Approval_Date_Local = source.Approval_Date_Local;
            target.Approver_Name = source.Approver_Name;
            target.Approver_PSNO = source.Approver_PSNO;
            target.Balance_Evidence_URL = source.Balance_Evidence_URL;
            target.Approval_Comment = comment;
        }
        

        public async Task<string> UploadEvidenceToSharePoint(IFormFile file, DateTime Quarter_Date, string ICP_Name, string username)
        {
            // Build Path: 2025/2025-12-31/ICP_Alpha/my_evidence.pdf
            string year = Quarter_Date.Year.ToString();
            string quarter = Quarter_Date.ToString("yyyy-MM-dd");
            string icpFolder = ICP_Name;

            string folderPath = $"{year}/{quarter}/{icpFolder}";

            // Logic to interact with SharePoint (using Graph API or CSOM)
            var uploadMessage = await _sputility.UploadFile(file, folderPath,username, "DocLib");
            var absoluteURI = uploadMessage.TempValue;

            return absoluteURI; // Return the resulting link
        }


        public async Task<bool> ApproveWithEvidence(Guid invoiceKey, string revisionNumber, string userPSNO, string fileurl)
        {
            var approvalDate = DateTime.Now;
            var comment = $"Manually approved by {userPSNO} on {approvalDate:yyyy-MM-dd}";

            // 1. Update the specific SNA record first to establish the "Master" approval state
            var success = await _snarepo.SetManualApprovalAsync(invoiceKey, revisionNumber, approvalDate, comment, userPSNO, fileurl);

            if (success)
            {
                // 2. Fetch the full entity details for the approved record
                // We need the Vendor, ICP, and Net_Amount_Doc_INR to perform the cross-sync
                var snadata = await GetSNABalanceDataByGroupedKey(invoiceKey);
                var sna = snadata.FirstOrDefault() ?? throw new Exception("Error: Approved record could not be retrieved for synchronization.");

                // 3. Trigger the Bulk Synchronization
                // This will update:
                // - TradePayable (Main & Backup) -> All detail rows for this group
                // - SNABalance (Main & Backup) -> All revisions with matching balances
                var syncMessage = await _snarepo.UpdateApprovalAcrossAllTables(sna);

                return syncMessage.Success;
            }

            return false;
        }

        //public async Task<Message> RemoveEvidenceAndResetApproval(Guid invoiceKey)
        //{
        //    // 1. Fetch the existing record to get the SharePoint URL
        //    var snadata = await GetSNABalanceDataByGroupedKey(invoiceKey);
        //    var sna = snadata.FirstOrDefault();

        //    if (sna == null || string.IsNullOrEmpty(sna.Balance_Evidence_URL))
        //    {
        //        return new Message { Success = false, Text = "No evidence found for this record." };
        //    }

        //    try
        //    {
        //        // 2. Delete file from SharePoint via Utility
        //        // The SPUtility should be able to parse the URL or take the full path to delete
        //        var deleteResult = await _sputility.DeleteFile(sna.Balance_Evidence_URL, "DocLib");

        //        if (!deleteResult.Success)
        //        {
        //            // Optional: You might still want to proceed with DB reset if the file is missing in SP
        //            System.Diagnostics.Debug.WriteLine($"SharePoint Delete Warning: {deleteResult.Text}");
        //        }

        //        // 3. Update Database Status using the Repository method created above
        //        var dbResult = await _snarepo.RemoveApprovalAcrossAllTables(sna);

        //        return dbResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        return new Message { Success = false, Text = $"Error during removal: {ex.Message}" };
        //    }
        ////}


        public async Task<bool> RemoveApprovalEvidenceAndStatus(Guid invoiceKey, string revisionNumber, string userPSNO)
        {
            var removalDate = DateTime.Now;
            var comment = $"Manually removed evidence by {userPSNO} on {removalDate:yyyy-MM-dd}";

            // 1. Update the specific SNA record first to establish the "Master" approval state
            var success = await _snarepo.SetManualApprovalAsync(invoiceKey, revisionNumber, removalDate, comment, userPSNO, "", true);

            if (success)
            {
                // 2. Fetch the full entity details for the approved record
                // We need the Vendor, ICP, and Net_Amount_Doc_INR to perform the cross-sync
                var snadata = await GetSNABalanceDataByGroupedKey(invoiceKey);
                var sna = snadata.FirstOrDefault() ?? throw new Exception("Error: Approved record could not be retrieved for synchronization.");

                // 3. Trigger the Bulk Synchronization
                // This will update:
                // - TradePayable (Main & Backup) -> All detail rows for this group
                // - SNABalance (Main & Backup) -> All revisions with matching balances
                var syncMessage = await _snarepo.UpdateApprovalAcrossAllTables(sna,true);

                return syncMessage.Success;
            }

            return false;

        }

        public async Task<IEnumerable<dynamic>> GetVarianceReport(DateTime quarterDate, string currentRev, string previousRev)
        {
            return await _snarepo.GetVarianceReport(quarterDate, currentRev, previousRev);
        }
    }
}
