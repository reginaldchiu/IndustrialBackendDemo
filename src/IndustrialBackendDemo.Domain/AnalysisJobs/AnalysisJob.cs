using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Domain.AnalysisJobs;

public sealed class AnalysisJob
{
    public AnalysisJobId Id { get; }

    public DeviceId DeviceId { get; }

    public AnalysisJobStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public AnalysisJob(AnalysisJobId id, DeviceId deviceId)
    {
        ValidateId(id, deviceId);

        Id = id;
        DeviceId = deviceId;

        Status = AnalysisJobStatus.Pending;

        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private AnalysisJob(
        AnalysisJobId id,
        DeviceId deviceId,
        AnalysisJobStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? failureReason)
    {
        ValidateId(id, deviceId);
        ValidateStatus(status);
        ValidateTimestamps(createdAtUtc, startedAtUtc, completedAtUtc);
        ValidateLifecycle(status, startedAtUtc, completedAtUtc, failureReason);

        Id = id;
        DeviceId = deviceId;
        Status = status;

        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        FailureReason = failureReason;
    }

    public static AnalysisJob Reconstitute(
        AnalysisJobId id,
        DeviceId deviceId,
        AnalysisJobStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? failureReason)
    {
        return new AnalysisJob(id, deviceId, status, createdAtUtc, startedAtUtc, completedAtUtc, failureReason);
    }

    public void Start()
    {
        EnsurePending();

        Status = AnalysisJobStatus.Running;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        EnsureRunning();

        Status = AnalysisJobStatus.Succeeded;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        EnsureRunning();

        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        Status = AnalysisJobStatus.Failed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        FailureReason = reason;
    }

    private void EnsurePending()
    {
        if (Status != AnalysisJobStatus.Pending)
        {
            throw new InvalidOperationException("Analysis job must be pending.");
        }
    }

    private void EnsureRunning()
    {
        if (Status != AnalysisJobStatus.Running)
        {
            throw new InvalidOperationException("Analysis job must be running.");
        }
    }

    private static void ValidateId(AnalysisJobId id, DeviceId deviceId)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Analysis job ID cannot be empty.", nameof(id));
        }

        if (deviceId.Value == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(deviceId));
        }
    }

    private static void ValidateStatus(AnalysisJobStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Invalid analysis job status.");
        }
    }

    private static void ValidateTimestamps(
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("CreatedAtUtc must use UTC.", nameof(createdAtUtc));
        }

        if (startedAtUtc.HasValue && startedAtUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("StartedAtUtc must use UTC.", nameof(startedAtUtc));
        }

        if (completedAtUtc.HasValue && completedAtUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("CompletedAtUtc must use UTC", nameof(completedAtUtc));
        }
    }

    private static void ValidateLifecycle(
        AnalysisJobStatus status,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? failureReason)
    {
        var isValid = status switch
        {
            AnalysisJobStatus.Pending =>
                startedAtUtc is null &&
                completedAtUtc is null &&
                failureReason is null,

            AnalysisJobStatus.Running =>
                startedAtUtc is not null &&
                completedAtUtc is null &&
                failureReason is null,

            AnalysisJobStatus.Succeeded =>
                startedAtUtc is not null &&
                completedAtUtc is not null &&
                failureReason is null,

            AnalysisJobStatus.Failed =>
                startedAtUtc is not null &&
                completedAtUtc is not null &&
                !string.IsNullOrWhiteSpace(failureReason),

            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException("Analysis job lifecycle properties are inconsistent.");
        }
    }
}
