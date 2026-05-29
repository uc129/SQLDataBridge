using Dapper;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class ProcessSummaryRepository(TradePayableDbContext db) : IProcessSummaryRepository
{
    public async Task UpsertAsync(Guid runId, DateTime quarterDate, ProcessResultSummary summary)
    {
        await EnsureTableAsync();

        const string sql = """
            IF EXISTS (SELECT 1 FROM TP_ProcessSummary WHERE RunId = @RunId)
                UPDATE TP_ProcessSummary SET
                    QuarterDate                        = @QuarterDate,
                    RevisionNumber                     = @RevisionNumber,
                    SavedAt                            = @SavedAt,
                    Original_SAP_AmountLocal           = @Original_SAP_AmountLocal,
                    TotalAdvanceAdjustedLocal          = @TotalAdvanceAdjustedLocal,
                    NetLiabilityAmountLocal            = @NetLiabilityAmountLocal,
                    MSME_Hyperion_2D170100_Net_Balance  = @MSME_Hyperion_2D170100_Net_Balance,
                    MSME_Hyperion_2D170200_Net_Balance  = @MSME_Hyperion_2D170200_Net_Balance,
                    MSME_Hyperion_2D190510_Net_Balance  = @MSME_Hyperion_2D190510_Net_Balance,
                    CapRev_Hyperion_2D190300_Net_Balance = @CapRev_Hyperion_2D190300_Net_Balance,
                    GIT_Total_SAP_Amount_Local         = @GIT_Total_SAP_Amount_Local,
                    GIT_Total_Adjusted_Amount_Local    = @GIT_Total_Adjusted_Amount_Local,
                    GIT_Total_Net_Balance              = @GIT_Total_Net_Balance,
                    SNA_Original_SAP_Amount_Local      = @SNA_Original_SAP_Amount_Local,
                    SNA_Advance_Adjusted_Local         = @SNA_Advance_Adjusted_Local,
                    SNA_Net_Balance_Local              = @SNA_Net_Balance_Local,
                    SNA_Net_Balance_Doc_INR            = @SNA_Net_Balance_Doc_INR,
                    SNA_Net_ERV                        = @SNA_Net_ERV
                WHERE RunId = @RunId
            ELSE
                INSERT INTO TP_ProcessSummary (
                    RunId, QuarterDate, RevisionNumber, SavedAt,
                    Original_SAP_AmountLocal, TotalAdvanceAdjustedLocal, NetLiabilityAmountLocal,
                    MSME_Hyperion_2D170100_Net_Balance, MSME_Hyperion_2D170200_Net_Balance, MSME_Hyperion_2D190510_Net_Balance,
                    CapRev_Hyperion_2D190300_Net_Balance,
                    GIT_Total_SAP_Amount_Local, GIT_Total_Adjusted_Amount_Local, GIT_Total_Net_Balance,
                    SNA_Original_SAP_Amount_Local, SNA_Advance_Adjusted_Local, SNA_Net_Balance_Local,
                    SNA_Net_Balance_Doc_INR, SNA_Net_ERV
                ) VALUES (
                    @RunId, @QuarterDate, @RevisionNumber, @SavedAt,
                    @Original_SAP_AmountLocal, @TotalAdvanceAdjustedLocal, @NetLiabilityAmountLocal,
                    @MSME_Hyperion_2D170100_Net_Balance, @MSME_Hyperion_2D170200_Net_Balance, @MSME_Hyperion_2D190510_Net_Balance,
                    @CapRev_Hyperion_2D190300_Net_Balance,
                    @GIT_Total_SAP_Amount_Local, @GIT_Total_Adjusted_Amount_Local, @GIT_Total_Net_Balance,
                    @SNA_Original_SAP_Amount_Local, @SNA_Advance_Adjusted_Local, @SNA_Net_Balance_Local,
                    @SNA_Net_Balance_Doc_INR, @SNA_Net_ERV
                )
            """;

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new
        {
            RunId                              = runId,
            QuarterDate                        = quarterDate,
            RevisionNumber                     = summary.RevisionNumber,
            SavedAt                            = DateTime.UtcNow,
            Original_SAP_AmountLocal           = summary.Original_SAP_AmountLocal,
            TotalAdvanceAdjustedLocal          = summary.TotalAdvanceAdjustedLocal,
            NetLiabilityAmountLocal            = summary.NetLiabilityAmountLocal,
            MSME_Hyperion_2D170100_Net_Balance  = summary.MSMEResults.Hyperion_2D170100_Net_Balance,
            MSME_Hyperion_2D170200_Net_Balance  = summary.MSMEResults.Hyperion_2D170200_Net_Balance,
            MSME_Hyperion_2D190510_Net_Balance  = summary.MSMEResults.Hyperion_2D190510_Net_Balance,
            CapRev_Hyperion_2D190300_Net_Balance = summary.CapitalRevenueResults.Hyperion_2D190300_Net_Balance,
            GIT_Total_SAP_Amount_Local         = summary.GITAdvanceAdjustmentResults.Total_SAP_Amount_Local,
            GIT_Total_Adjusted_Amount_Local    = summary.GITAdvanceAdjustmentResults.Total_Adjusted_Amount_Local,
            GIT_Total_Net_Balance              = summary.GITAdvanceAdjustmentResults.Total_Net_Balance,
            SNA_Original_SAP_Amount_Local      = summary.SNACompanyResults.Original_SAP_Amount_Local,
            SNA_Advance_Adjusted_Local         = summary.SNACompanyResults.Advance_Adjusted_Local,
            SNA_Net_Balance_Local              = summary.SNACompanyResults.Net_Balance_Local,
            SNA_Net_Balance_Doc_INR            = summary.SNACompanyResults.Net_Balance_Doc_INR,
            SNA_Net_ERV                        = summary.SNACompanyResults.Net_ERV,
        });
    }

    private async Task EnsureTableAsync()
    {
        const string ddl = """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TP_ProcessSummary')
                CREATE TABLE TP_ProcessSummary (
                    Id                                  INT IDENTITY(1,1) PRIMARY KEY,
                    RunId                               UNIQUEIDENTIFIER NOT NULL,
                    QuarterDate                         DATETIME NOT NULL,
                    RevisionNumber                      NVARCHAR(50) NOT NULL,
                    SavedAt                             DATETIME NOT NULL,
                    Original_SAP_AmountLocal            DECIMAL(18,2) NOT NULL,
                    TotalAdvanceAdjustedLocal           DECIMAL(18,2) NOT NULL,
                    NetLiabilityAmountLocal             DECIMAL(18,2) NOT NULL,
                    MSME_Hyperion_2D170100_Net_Balance   DECIMAL(18,2) NOT NULL,
                    MSME_Hyperion_2D170200_Net_Balance   DECIMAL(18,2) NOT NULL,
                    MSME_Hyperion_2D190510_Net_Balance   DECIMAL(18,2) NOT NULL,
                    CapRev_Hyperion_2D190300_Net_Balance DECIMAL(18,2) NOT NULL,
                    GIT_Total_SAP_Amount_Local          DECIMAL(18,2) NOT NULL,
                    GIT_Total_Adjusted_Amount_Local     DECIMAL(18,2) NOT NULL,
                    GIT_Total_Net_Balance               DECIMAL(18,2) NOT NULL,
                    SNA_Original_SAP_Amount_Local       DECIMAL(18,2) NOT NULL,
                    SNA_Advance_Adjusted_Local          DECIMAL(18,2) NOT NULL,
                    SNA_Net_Balance_Local               DECIMAL(18,2) NOT NULL,
                    SNA_Net_Balance_Doc_INR             DECIMAL(18,2) NOT NULL,
                    SNA_Net_ERV                         DECIMAL(18,2) NOT NULL
                );
            """;

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync(ddl);
    }
}
