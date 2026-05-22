using Dapper;
using Domain.Aggregates;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Dapper;
using Infrastructure.Database;

namespace Infrastructure.Repository
{
    public class TradePayableResultsRepository(DapperContext dbcontext, IDataSettings datasettings):ITradePayableRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        private readonly IDataSettings _datasettings = datasettings;
        public async Task<Message> UpdateApprovalAcrossAllTables(FAGLL03SNABalanceApproveEntity approvedRecord)
        {
            if (string.IsNullOrEmpty(approvedRecord.Balance_Evidence_URL))
            {
                return new Message { Success = false, Text = "Approval failed: No evidence file URL provided." };
            }

            var tpTable = _datasettings.TradePayable_ResultsTable;
            var tpBackupTable = _datasettings.Backup_TradePayable_ResultsTable;
            var snaTable = "FAGLL03SNABalanceTable";
            var snaBackupTable = "FAGLL03SNABalanceBackupTable";

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
          AND ICP_Name = @ICP_Name
          AND ICP_Hyperion = @ICP
          AND Quarter_Date = @Quarter
          AND Net_Amount_Doc_INR = @Amount";

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
                    Vendor = approvedRecord.Vendor,
                    ICP_Name = approvedRecord.ICP_Name,
                    ICP = approvedRecord.ICP_Hyperion,
                    Quarter = approvedRecord.Quarter_Date,
                    Amount = approvedRecord.Net_Amount_Doc_INR
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
    }

    public class UpdateTradePayableRequest

    {

        public string Vendor { get; set; } = null!;

        public string RevisionNumber { get; set; } = null!;

        public string Document_Number { get; set; } = null!;

        public string User_Name { get; set; } = null!;

        public DateTime Quarter_Date { get; set; } = DateTime.MinValue;

        public string ICP_Name { get; set; } = null!;

        public string Approver_Name { get; set; } = null!;

        public string Approver_PSNO { get; set; } = null!;

        public bool Balance_Approved_Local { get; set; }

        public DateTime Approval_Date_Local { get; set; }

        public bool Balance_Approved_Doc { get; set; }

        public DateTime Approval_Date_Doc { get; set; }

        public string? Approval_Comment { get; set; } = null!;

        public string Balance_Evidence_URL { get; set; } = null!;

    }
}