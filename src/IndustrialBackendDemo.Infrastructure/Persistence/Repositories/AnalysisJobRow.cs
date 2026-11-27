namespace IndustrialBackendDemo.Infrastructure.Persistence.Repositories;

internal sealed class AnalysisJobRow
{
    public Guid Id { get; init; }

    public Guid DeviceId { get; init; }

    public short Status { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public string? FailureReason { get; init; }
}
