using Dapper;
using Domain.Aggregates;
using Domain.Models.ViewModels;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;



namespace Infrastructure.Repository
{
    public class SNABalanceApproveRepository(DapperContext dbcontext, IDataSettings datasettings) : ISNABalanceApproveRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _datasettings = datasettings;

        public async Task<Message> SaveSNABalanceDataAsync(IEnumerable<FAGLL03SNABalanceApproveEntity> snadata)
        {
            var msg = new Message();
            var tableName = _datasettings.SNA_Balance_Summary;
            if (string.IsNullOrEmpty(tableName))
            {
                msg.Success = false;
                msg.Text = "Problem getting SNA Summary table name";
                return msg;
            }
            var backupTableName = _datasettings.BackupTables.Backup_SNA_Balance_Summary;
            if (string.IsNullOrEmpty(backupTableName))
            {
                msg.Success = false;
                msg.Text = "Problem getting Backup SNA Summary table name";
                return msg;
            }
            using var connection = _dbcontext.CreateConnection("default");
            string sql = $@"
                TRUNCATE TABLE {tableName}
                
                INSERT INTO {tableName} (
                    SNAGroupedInvoiceKey,
                    Company_Code,
                    RevisionNumber,
                    Quarter_Date,
                    Document_Number,
                    Vendor,
                    Vendor_Description,
                    Base_Hyperion,
                    ICP_Hyperion,
                    ICP_Name,
                    User_Name,
                    Net_Amount_Doc,
                    Net_Amount_Doc_INR,
                    Net_Amount_Local,
                    Net_Amount_Local_INR,
                    Balance_Approved_Doc,
                    Balance_Approved_Local,
                    Approval_Date_Doc,
                    Approval_Date_Local,
                    Approver_Name,
                    Approver_PSNO,
                    Balance_Evidence_URL,
                    Approval_Comment
                ) VALUES (
                    @SNAGroupedInvoiceKey,
                    @Company_Code,
                    @RevisionNumber,
                    @Quarter_Date,
                    @Document_Number,
                    @Vendor,
                    @Vendor_Description,
                    @Base_Hyperion,
                    @ICP_Hyperion,
                    @ICP_Name,
                    @User_Name,
                    @Net_Amount_Doc,
                    @Net_Amount_Doc_INR,
                    @Net_Amount_Local,
                    @Net_Amount_Local_INR,
                    @Balance_Approved_Doc,
                    @Balance_Approved_Local,
                    @Approval_Date_Doc,
                    @Approval_Date_Local,
                    @Approver_Name,
                    @Approver_PSNO,
                    @Balance_Evidence_URL,
                    @Approval_Comment
                )";
            string backupSql = $@"
                INSERT INTO {tableName} (
                    SNAGroupedInvoiceKey,
                    Company_Code,
                    RevisionNumber,
                    Quarter_Date,
                    Document_Number,
                    Vendor,
                    Vendor_Description,
                    Base_Hyperion,
                    ICP_Hyperion,
                    ICP_Name,
                    User_Name,
                    Net_Amount_Doc,
                    Net_Amount_Doc_INR,
                    Net_Amount_Local,
                    Net_Amount_Local_INR,
                    Balance_Approved_Doc,
                    Balance_Approved_Local,
                    Approval_Date_Doc,
                    Approval_Date_Local,
                    Approver_Name,
                    Approver_PSNO,
                    Balance_Evidence_URL,
                    Approval_Comment
                ) VALUES (
                    @SNAGroupedInvoiceKey,
                    @Company_Code,
                    @RevisionNumber,
                    @Quarter_Date,
                    @Document_Number,
                    @Vendor,
                    @Vendor_Description,
                    @Base_Hyperion,
                    @ICP_Hyperion,
                    @ICP_Name,
                    @User_Name,
                    @Net_Amount_Doc,
                    @Net_Amount_Doc_INR,
                    @Net_Amount_Local,
                    @Net_Amount_Local_INR,
                    @Balance_Approved_Doc,
                    @Balance_Approved_Local,
                    @Approval_Date_Doc,
                    @Approval_Date_Local,
                    @Approver_Name,
                    @Approver_PSNO,
                    @Balance_Evidence_URL,
                    @Approval_Comment
                )";

            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(sql, snadata, transaction);
                await connection.ExecuteAsync(backupSql, snadata, transaction);
                transaction.Commit();
                msg.Success = true;
                msg.Text = "SNA Summary data saved successfully.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                msg.Success = false;
                msg.Text = $"Error saving SNA Summary data: {ex.Message}";
            }
            


            return msg;
        }
        
        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetLatestSNADataAsync()
        {
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");
            var sql = @$"SELECT * FROM {tableName}";
            using var connection = _dbcontext.CreateConnection("default");
            var result = await connection.QueryAsync<FAGLL03SNABalanceApproveEntity>(sql);
            return result;
        }
        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetAllForQuarter(DateTime QuarterDate)
        {
            var tableName = _datasettings.GetTableConfigDataByKey("Backup_SNA_Balance_Summary");
            var sql = $@"SELECT * FROM {tableName} WHERE Quarter_Date = @QuarterDate";
            using var connection = _dbcontext.CreateConnection("default");
            var result = await connection.QueryAsync<FAGLL03SNABalanceApproveEntity>(sql, new { QuarterDate });
            return result;
        }

        public async Task<IEnumerable<FAGLL03SNABalanceApproveEntity>> GetByGroupId(Guid GroupedKey)
        {
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");
            var sql = $@"SELECT * FROM {tableName} WHERE [SNAGroupedInvoiceKey] = @GroupedKey";
            try
            {
                using var connection = _dbcontext.CreateConnection("default");
                var result = await connection.QueryAsync<FAGLL03SNABalanceApproveEntity>(sql,
                    new { GroupedKey });
                return result;
            }

            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw new Exception("Error getting SNA Data By ID");
            }
            
        }
        public async Task<int> UpdateApprovalStatusAsync(IEnumerable<FAGLL03SNABalanceApproveEntity> entities)
        {
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");

            if (string.IsNullOrEmpty(tableName)) throw new Exception("Problem getting SNA Summary table name");

            string sql = @$"
                UPDATE {tableName} 
                SET 
                    Balance_Approved_Doc = @Balance_Approved_Doc,
                    Balance_Approved_Local = @Balance_Approved_Local,
                    Approval_Date_Doc = @Approval_Date_Doc,
                    Approval_Date_Local = @Approval_Date_Local,
                    Approval_Comment = @Approval_Comment
                WHERE SNAGroupedInvoiceKey = @SNAGroupedInvoiceKey 
                  AND RevisionNumber = @RevisionNumber 
                  AND Quarter_Date = @Quarter_Date";

            using var connection = _dbcontext.CreateConnection("default");
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, entities, transaction);
                transaction.Commit();
                return affectedRows;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
      




        public async Task<bool> SetManualApprovalAsync(Guid groupedKey, string revisionNumber, DateTime approvalDate, string comment, string approverPSNO, string fileurl, bool removeEvidence)
        {
            var tableName = _datasettings.SNA_Balance_Summary;
            string sql = @$"
                UPDATE {tableName}
                SET 
                    Balance_Approved_Doc = 1,
                    Balance_Approved_Local = 1,
                    Approval_Date_Doc = @ApprovalDate,
                    Approval_Date_Local = @ApprovalDate,
                    Approval_Comment = @Comment,
                    Approver_PSNO = @UserRef,
                    Balance_Evidence_URL =@FileURL
                    WHERE SNAGroupedInvoiceKey = @GroupedKey 
                    AND RevisionNumber = @RevisionNumber";

            if (removeEvidence)
            {
                sql = @$"
                UPDATE {tableName}
                SET 
                    Balance_Approved_Doc = 0,
                    Balance_Approved_Local = 0,
                    Approval_Date_Doc = null,
                    Approval_Date_Local = null,
                    Approval_Comment = @Comment,
                    Approver_PSNO = @UserRef,
                    Balance_Evidence_URL =null
                    WHERE SNAGroupedInvoiceKey = @GroupedKey 
                    AND RevisionNumber = @RevisionNumber";
            }

           

            using var connection = _dbcontext.CreateConnection("default");

            try
            {
                int affectedRows = await connection.ExecuteAsync(sql, new
                {
                    GroupedKey = groupedKey,
                    RevisionNumber = revisionNumber,
                    ApprovalDate = approvalDate,
                    Comment = comment,
                    UserRef = approverPSNO,
                    FileURL = fileurl,
                });

                return affectedRows > 0;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw new Exception("Error while approving.");
            }
            
        }

        public async Task<Message> UpdateApprovalAcrossAllTables(FAGLL03SNABalanceApproveEntity approvedRecord, bool removeEvidence = false)
        {
            if (string.IsNullOrEmpty(approvedRecord.Balance_Evidence_URL) && removeEvidence == false)
            {
                return new Message { Success = false, Text = "Approval failed: No evidence file URL provided." };
            }

            var tpTable = _datasettings.TradePayable_ResultsTable;
            var tpBackupTable = _datasettings.Backup_TradePayable_ResultsTable;
            var snaTable = _datasettings.SNA_Balance_Summary;
            var snaBackupTable = _datasettings.BackupTables.Backup_SNA_Balance_Summary;

            // 1. Detail Table SQL (Trade Payables)
            // Updates all line items for this business group across ALL revisions
            string sqlTradePayable = $@"
                    UPDATE {{0}}
                    SET Approver_Name = @Name,
                        Approver_PSNO = @Psno,
                        Balance_Approved_Doc = 1,
                        Balance_Approved_Local = 1,
                        Approval_Date_Doc = @Date,
                        Approval_Date_Local = @Date,
                        Approval_Comment = @Comment,
                        Balance_Evidence_URL = @Url
                    WHERE 
                      Vendor = @Vendor
                      AND Hyperion_Code = @Base_Hyperion
                      AND Company_Code =@Company_Code
                      AND Hyperion_Code =@ICP
                      AND ICP_Name = @ICP_Name
                      AND QuarterEndDate = @Quarter";


            // 2. Summary Table SQL (SNA Summary)
            // Updates ONLY revisions where the net balance matches the approved amount
            string sqlSNA = $@"
        UPDATE {{0}}
        SET Balance_Approved_Doc = 1, 
            Balance_Approved_Local = 1,
            Approval_Date_Doc = @Date,
            Approval_Date_Local = @Date,
            Approver_Name = @Name,
            Approver_PSNO = @Psno,
            Balance_Evidence_URL = @Url,
            Approval_Comment = @Comment
        WHERE Vendor = @Vendor
          AND Base_Hyperion = @Base_Hyperion
          AND Company_Code =@Company_Code
          AND ICP_Name = @ICP_Name
          AND ICP_Hyperion = @ICP
          AND Quarter_Date = @Quarter
          AND Net_Amount_Doc_INR = @Amount";


            if (removeEvidence)
            {
                sqlTradePayable = $@"
                    UPDATE {{0}}
                    SET Approver_Name = @Name,
                        Approver_PSNO = @Psno,
                        Balance_Approved_Doc = 0,
                        Balance_Approved_Local = 0,
                        Approval_Date_Doc = null,
                        Approval_Date_Local = null,
                        Approval_Comment = @Comment,
                        Balance_Evidence_URL = null
                    WHERE 
                      Vendor = @Vendor
                      AND Hyperion_Code = @Base_Hyperion
                      AND Company_Code =@Company_Code
                      AND Hyperion_Code =@ICP
                      AND ICP_Name = @ICP_Name
                      AND QuarterEndDate = @Quarter";

                sqlSNA = $@"
        UPDATE {{0}}
        SET Balance_Approved_Doc = 0, 
            Balance_Approved_Local = 0,
            Approval_Date_Doc = null,
            Approval_Date_Local = null,
            Approver_Name = @Name,
            Approver_PSNO = @Psno,
            Balance_Evidence_URL = null,
            Approval_Comment = @Comment
        WHERE Vendor = @Vendor
          AND Base_Hyperion = @Base_Hyperion
          AND Company_Code =@Company_Code
          AND ICP_Name = @ICP_Name
          AND ICP_Hyperion = @ICP
          AND Quarter_Date = @Quarter
          AND Net_Amount_Doc_INR = @Amount";
            }

            using var connection = _dbcontext.CreateConnection("default");
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var parameters = new
                {
                    Date = DateTime.Now,
                    Name = approvedRecord.Approver_Name,
                    Psno = approvedRecord.Approver_PSNO,
                    Url = approvedRecord.Balance_Evidence_URL,
                    Comment = $"Bulk approved via {approvedRecord.RevisionNumber}",
                    ICP = approvedRecord.ICP_Hyperion,
                    Quarter = approvedRecord.Quarter_Date,
                    Amount = approvedRecord.Net_Amount_Doc_INR,
                    Base_Hyperion = approvedRecord.Base_Hyperion,
                    Company_Code = approvedRecord.Company_Code,
                    Vendor = approvedRecord.Vendor,
                    ICP_Name = approvedRecord.ICP_Name,
                };

                await connection.ExecuteAsync(string.Format(sqlTradePayable, tpTable), parameters, transaction);
                await connection.ExecuteAsync(string.Format(sqlTradePayable, tpBackupTable), parameters, transaction);
                await connection.ExecuteAsync(string.Format(sqlSNA, snaTable), parameters, transaction);
                await connection.ExecuteAsync(string.Format(sqlSNA, snaBackupTable), parameters, transaction);

                transaction.Commit();
                return new Message { Success = true, Text = "Approval successfully synced across all detail and summary records." };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new Message { Success = false, Text = $"Transaction failed: {ex.Message}" };
            }
        }


        



        ///
        public async Task<SNABalanceSummary> GetSummaryFromDbAsync(DateTime quarterDate)
        {
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");

            string sql = @$"
                SELECT 
                    COUNT(*) as TotalInvoices,
                    SUM(CASE WHEN Balance_Approved_Doc = 1 THEN 1 ELSE 0 END) as ApprovedCount,
                    SUM(Net_Amount_Doc) as TotalAmountDoc,
                    SUM(CASE WHEN Balance_Approved_Doc = 1 THEN Net_Amount_Doc ELSE 0 END) as ApprovedAmountDoc
                FROM {tableName}
                WHERE Quarter_Date = @quarterDate 
                AND RevisionNumber = (SELECT MAX(RevisionNumber) FROM {tableName} WHERE Quarter_Date = @quarterDate)";

            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QuerySingleAsync<SNABalanceSummary>(sql, new { quarterDate });
        }
        public async Task<IEnumerable<dynamic>> GetVarianceReport(DateTime quarterDate, string currentRev, string previousRev)
        {
            var tableName = _datasettings.GetTableConfigDataByKey("SNA_Balance_Summary");

            string sql = @$"SELECT 
                        curr.Document_Number,
                        curr.Vendor_Description,
                        curr.User_Name,
                        prev.RevisionNumber AS Old_Rev,
                        curr.RevisionNumber AS New_Rev,
                        prev.Net_Amount_Doc AS Old_Balance,
                        curr.Net_Amount_Doc AS New_Balance,
                        (curr.Net_Amount_Doc - prev.Net_Amount_Doc) AS Variance,
                        curr.Approval_Comment
                    FROM {tableName} curr
                    INNER JOIN {tableName} prev 
                        ON curr.Vendor = prev.Vendor
                        AND curr.Document_Number = prev.Document_Number
                        AND curr.ICP_Name = prev.ICP_Name
                        AND curr.User_Name = prev.User_Name
                        AND curr.Quarter_Date = prev.Quarter_Date
                    WHERE curr.Quarter_Date = @QuarterDate
                      AND curr.RevisionNumber = @CurrentRev
                      AND prev.RevisionNumber = @PreviousRev
                      AND curr.Net_Amount_Doc <> prev.Net_Amount_Doc;";

            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryAsync(sql, new
            {
                QuarterDate = quarterDate,
                CurrentRev = currentRev,
                PreviousRev = previousRev
            });
        }
    }
}
