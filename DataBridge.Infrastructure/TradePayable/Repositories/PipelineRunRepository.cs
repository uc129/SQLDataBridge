using Dapper;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.Models;

namespace DataBridge.Infrastructure.TradePayable.Repositories;

internal sealed class PipelineRunRepository(TradePayableDbContext db) : IPipelineRunRepository
{
    public async Task CreateAsync(PipelineRun run)
    {
        const string sql = """
            INSERT INTO TP_PipelineRun
                (RunId, QuarterDate, RevisionNumber, CurrentStepIndex, Status, StartedBy, StartedAt)
            VALUES
                (@RunId, @QuarterDate, @RevisionNumber, @CurrentStepIndex, @Status, @StartedBy, @StartedAt)
            """;

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new
        {
            run.RunId,
            run.QuarterDate,
            run.RevisionNumber,
            run.CurrentStepIndex,
            Status = run.Status.ToString(),
            run.StartedBy,
            run.StartedAt,
        });
    }

    public async Task<PipelineRun?> GetByRunIdAsync(Guid runId)
    {
        const string sql = "SELECT * FROM TP_PipelineRun WHERE RunId = @runId";
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { runId });
        return row is null ? null : MapRow(row);
    }

    public async Task<IEnumerable<PipelineRun>> GetAllAsync()
    {
        const string sql = "SELECT * FROM TP_PipelineRun ORDER BY StartedAt DESC";
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<dynamic>(sql);
        return rows.Select(MapRow);
    }

    public async Task UpdateStepIndexAsync(Guid runId, int stepIndex)
    {
        const string sql = "UPDATE TP_PipelineRun SET CurrentStepIndex = @stepIndex WHERE RunId = @runId";
        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new { runId, stepIndex });
    }

    public async Task UpdateStatusAsync(Guid runId, PipelineRunStatus status)
    {
        var completedAt = status is PipelineRunStatus.Completed or PipelineRunStatus.Failed or PipelineRunStatus.Cancelled
            ? (DateTime?)DateTime.UtcNow
            : null;

        const string sql = """
            UPDATE TP_PipelineRun
            SET Status = @status, CompletedAt = @completedAt
            WHERE RunId = @runId
            """;

        await using var conn = db.OpenDefault();
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new { runId, status = status.ToString(), completedAt });
    }

    private static PipelineRun MapRow(dynamic r) => new()
    {
        RunId            = r.RunId,
        QuarterDate      = r.QuarterDate,
        RevisionNumber   = r.RevisionNumber,
        CurrentStepIndex = r.CurrentStepIndex,
        Status           = Enum.Parse<PipelineRunStatus>((string)r.Status),
        StartedBy        = r.StartedBy,
        StartedAt        = r.StartedAt,
        CompletedAt      = r.CompletedAt,
    };
}
