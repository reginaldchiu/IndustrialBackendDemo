using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Domain.AnalysisJobs;

public sealed class AnalysisJob
{
    public AnalysisJobId Id { get; }

    public DeviceId DeviceId { get; }

    public AnalysisJobStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompleteAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public AnalysisJob(AnalysisJobId id, DeviceId deviceId)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Analysis job ID cannot be empty.", nameof(id));
        }

        if (deviceId.Value == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(deviceId));
        }

        Id = id;
        DeviceId = deviceId;

        Status = AnalysisJobStatus.Pending;

        CreatedAtUtc = DateTimeOffset.UtcNow;
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
        CompleteAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        EnsureRunning();

        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        Status = AnalysisJobStatus.Failed;
        CompleteAtUtc = DateTimeOffset.UtcNow;
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
}
