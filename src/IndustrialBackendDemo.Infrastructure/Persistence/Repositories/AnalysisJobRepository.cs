
using Dapper;
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Domain.Devices;
using Npgsql;

namespace IndustrialBackendDemo.Infrastructure.Persistence.Repositories;

public sealed class AnalysisJobRepository(IDatabaseConnectionFactory factory) : IAnalysisJobRepository
{
    private const string _insertSql = """
        INSERT INTO analysis_jobs
        (
            id,
            device_id,
            status,
            created_at_utc,
            started_at_utc,
            completed_at_utc,
            failure_reason
        )
        VALUES
        (
            @Id,
            @DeviceId,
            @Status,
            @CreatedAtUtc,
            @StartedAtUtc,
            @CompletedAtUtc,
            @FailureReason
        );
        """;

    private const string _selectColumns = """
        id AS Id,
        device_id AS DeviceId,
        status AS Status,
        created_at_utc AS CreatedAtUtc,
        started_at_utc AS StartedAtUtc,
        completed_at_utc AS CompletedAtUtc,
        failure_reason AS FailureReason
        """;

    private const string _updateSql = """
        UPDATE analysis_jobs
        SET
            status = @Status,
            started_at_utc = @StartedAtUtc,
            completed_at_utc = @CompletedAtUtc,
            failure_reason = @FailureReason
        WHERE id = @Id;
        """;

    public async Task AddAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                _insertSql,
                new
                {
                    Id = job.Id.Value,
                    DeviceId = job.DeviceId.Value,
                    Status = (short)job.Status,
                    job.CreatedAtUtc,
                    job.StartedAtUtc,
                    job.CompletedAtUtc,
                    job.FailureReason
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<AnalysisJob?> GetByIdAsync(
        AnalysisJobId id,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        var sql = $"""
            SELECT {_selectColumns}
            FROM analysis_jobs
            WHERE id = @Id;
            """;

        AnalysisJobRow? row =
            await connection.QuerySingleOrDefaultAsync<AnalysisJobRow>(
                new CommandDefinition(
                    sql,
                    new { Id = id.Value },
                    cancellationToken: cancellationToken
                )
            );

        return row is null ? null : MapToDomain(row);
    }

    public async Task UpdateAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                _updateSql,
                new
                {
                    Id = job.Id.Value,
                    Status = (short)job.Status,
                    job.StartedAtUtc,
                    job.CompletedAtUtc,
                    job.FailureReason
                },
                cancellationToken: cancellationToken
            )
        );

        if (affectedRows == 0)
        {
            throw new InvalidOperationException(
                $"Analysis job '{job.Id.Value}' does not exist.");
        }
    }

    private static AnalysisJob MapToDomain(AnalysisJobRow row)
    {
        return AnalysisJob.Reconstitute(
            new AnalysisJobId(row.Id),
            new DeviceId(row.DeviceId),
            (AnalysisJobStatus)row.Status,
            row.CreatedAtUtc,
            row.StartedAtUtc,
            row.CompletedAtUtc,
            row.FailureReason
        );
    }
}
