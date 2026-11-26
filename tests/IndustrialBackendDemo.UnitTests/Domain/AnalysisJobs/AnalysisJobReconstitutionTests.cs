
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Domain.AnalysisJobs;

public sealed class AnalysisJobReconstitutionTests
{
    private static readonly DateTimeOffset _createdAtUtc =
        new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset _startedAtUtc =
        new(2026, 1, 1, 10, 5, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset _completedAtUtc =
        new(2026, 1, 1, 10, 10, 0, TimeSpan.Zero);

    [Fact]
    public void ReconstituteShouldRestorePendingJob()
    {
        AnalysisJob job = CreateJob(
            AnalysisJobStatus.Pending);

        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.CompletedAtUtc);
        Assert.Null(job.FailureReason);
    }

    [Fact]
    public void ReconstituteShouldRestoreRunningJob()
    {
        AnalysisJob job = CreateJob(
            AnalysisJobStatus.Running);

        Assert.Equal(AnalysisJobStatus.Running, job.Status);
        Assert.Equal(_startedAtUtc, job.StartedAtUtc);
        Assert.Null(job.CompletedAtUtc);
        Assert.Null(job.FailureReason);
    }

    [Fact]
    public void ReconstituteShouldRestoreSucceededJob()
    {
        AnalysisJob job = CreateJob(
            AnalysisJobStatus.Succeeded);

        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);
        Assert.Equal(_startedAtUtc, job.StartedAtUtc);
        Assert.Equal(_completedAtUtc, job.CompletedAtUtc);
        Assert.Null(job.FailureReason);
    }

    [Fact]
    public void ReconstituteShouldRestoreFailedJob()
    {
        const string failureReason = "Analysis failed.";

        AnalysisJob job = CreateJob(
            AnalysisJobStatus.Failed,
            failureReason);

        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal(_startedAtUtc, job.StartedAtUtc);
        Assert.Equal(_completedAtUtc, job.CompletedAtUtc);
        Assert.Equal(failureReason, job.FailureReason);
    }

    [Fact]
    public void ReconstituteShouldRestoreAllProperties()
    {
        var id = AnalysisJobId.New();
        var deviceId = DeviceId.New();

        const string failureReason = "Device communication failed.";

        var job = AnalysisJob.Reconstitute(
            id,
            deviceId,
            AnalysisJobStatus.Failed,
            _createdAtUtc,
            _startedAtUtc,
            _completedAtUtc,
            failureReason);

        Assert.Equal(id, job.Id);
        Assert.Equal(deviceId, job.DeviceId);
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal(_createdAtUtc, job.CreatedAtUtc);
        Assert.Equal(_startedAtUtc, job.StartedAtUtc);
        Assert.Equal(_completedAtUtc, job.CompletedAtUtc);
        Assert.Equal(failureReason, job.FailureReason);
    }

    [Fact]
    public void ReconstituteShouldRejectEmptyAnalysisJobId()
    {
        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                default,
                DeviceId.New(),
                AnalysisJobStatus.Pending,
                _createdAtUtc,
                null,
                null,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectEmptyDeviceId()
    {
        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                default,
                AnalysisJobStatus.Pending,
                _createdAtUtc,
                null,
                null,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                DeviceId.New(),
                (AnalysisJobStatus)999,
                _createdAtUtc,
                null,
                null,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectNonUtcCreatedAt()
    {
        DateTimeOffset localTime =
            new(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                DeviceId.New(),
                AnalysisJobStatus.Pending,
                localTime,
                null,
                null,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectNonUtcStartedAt()
    {
        DateTimeOffset localTime =
            new(2026, 1, 1, 10, 5, 0, TimeSpan.FromHours(8));

        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                DeviceId.New(),
                AnalysisJobStatus.Running,
                _createdAtUtc,
                localTime,
                null,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectNonUtcCompletedAt()
    {
        DateTimeOffset localTime =
            new(2026, 1, 1, 10, 10, 0, TimeSpan.FromHours(8));

        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                DeviceId.New(),
                AnalysisJobStatus.Succeeded,
                _createdAtUtc,
                _startedAtUtc,
                localTime,
                null));
    }

    [Fact]
    public void ReconstituteShouldRejectPendingJobWithStartedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Pending,
            _startedAtUtc,
            null,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectPendingJobWithCompletedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Pending,
            null,
            _completedAtUtc,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectPendingJobWithFailureReason()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Pending,
            null,
            null,
            "Analysis failed.");
    }

    [Fact]
    public void ReconstituteShouldRejectRunningJobWithoutStartedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Running,
            null,
            null,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectRunningJobWithCompletedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Running,
            _startedAtUtc,
            _completedAtUtc,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectRunningJobWithFailureReason()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Running,
            _startedAtUtc,
            null,
            "Analysis failed.");
    }

    [Fact]
    public void ReconstituteShouldRejectSucceededJobWithoutStartedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Succeeded,
            null,
            _completedAtUtc,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectSucceededJobWithoutCompletedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Succeeded,
            _startedAtUtc,
            null,
            null);
    }

    [Fact]
    public void ReconstituteShouldRejectSucceededJobWithFailureReason()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Succeeded,
            _startedAtUtc,
            _completedAtUtc,
            "Analysis failed.");
    }

    [Fact]
    public void ReconstituteShouldRejectFailedJobWithoutStartedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Failed,
            null,
            _completedAtUtc,
            "Analysis failed.");
    }

    [Fact]
    public void ReconstituteShouldRejectFailedJobWithoutCompletedAt()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Failed,
            _startedAtUtc,
            null,
            "Analysis failed.");
    }

    [Fact]
    public void ReconstituteShouldRejectFailedJobWithoutFailureReason()
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Failed,
            _startedAtUtc,
            _completedAtUtc,
            null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ReconstituteShouldRejectFailedJobWithBlankFailureReason(
        string failureReason)
    {
        AssertInvalidLifecycle(
            AnalysisJobStatus.Failed,
            _startedAtUtc,
            _completedAtUtc,
            failureReason);
    }

    private static AnalysisJob CreateJob(
        AnalysisJobStatus status,
        string? failureReason = null)
    {
        DateTimeOffset? startedAtUtc =
            status == AnalysisJobStatus.Pending
                ? null
                : _startedAtUtc;

        DateTimeOffset? completedAtUtc =
            status is AnalysisJobStatus.Succeeded
                or AnalysisJobStatus.Failed
                    ? _completedAtUtc
                    : null;

        return AnalysisJob.Reconstitute(
            AnalysisJobId.New(),
            DeviceId.New(),
            status,
            _createdAtUtc,
            startedAtUtc,
            completedAtUtc,
            failureReason);
    }

    private static void AssertInvalidLifecycle(
        AnalysisJobStatus status,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? failureReason)
    {
        Assert.Throws<ArgumentException>(() =>
            AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                DeviceId.New(),
                status,
                _createdAtUtc,
                startedAtUtc,
                completedAtUtc,
                failureReason));
    }
}
