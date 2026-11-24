using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.AnalysisJobs;

public sealed class AnalysisJobTests
{
    #region Constructor

    [Fact]
    public void ConstructorWithValidArgumentsShouldCreateJob()
    {
        var id = AnalysisJobId.New();
        var deviceId = DeviceId.New();

        AnalysisJob job = new(id, deviceId);

        Assert.Equal(id, job.Id);
        Assert.Equal(deviceId, job.DeviceId);
    }

    [Fact]
    public void ConstructorWithDefaultAnalysisJobIdShouldThrowArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AnalysisJob(default, DeviceId.New()));
    }

    [Fact]
    public void ConstructorWithDefaultDeviceIdShouldThrowArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AnalysisJob(AnalysisJobId.New(), default));
    }

    [Fact]
    public void ConstructorShouldInitializePendingStatus()
    {
        AnalysisJob job = CreateJob();

        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
    }

    [Fact]
    public void ConstructorShouldInitializeCreatedAtUtc()
    {
        DateTimeOffset beforeCreation = DateTimeOffset.UtcNow;

        AnalysisJob job = CreateJob();

        DateTimeOffset afterCreation = DateTimeOffset.UtcNow;

        Assert.InRange(job.CreatedAtUtc, beforeCreation, afterCreation);

        Assert.Equal(TimeSpan.Zero, job.CreatedAtUtc.Offset);
    }

    [Fact]
    public void ConstructorShouldInitializeStartedAtUtcAsNull()
    {
        AnalysisJob job = CreateJob();

        Assert.Null(job.StartedAtUtc);
    }

    [Fact]
    public void ConstructorShouldInitializeCompletedAtUtcAsNull()
    {
        AnalysisJob job = CreateJob();

        Assert.Null(job.CompletedAtUtc);
    }

    [Fact]
    public void ConstructorShouldInitializeFailureReasonAsNull()
    {
        AnalysisJob job = CreateJob();

        Assert.Null(job.FailureReason);
    }

    #endregion

    #region Start

    [Fact]
    public void StartShouldChangeStatusToRunning()
    {
        AnalysisJob job = CreateJob();

        job.Start();

        Assert.Equal(AnalysisJobStatus.Running, job.Status);
    }

    [Fact]
    public void StartShouldSetStartedAtUtc()
    {
        AnalysisJob job = CreateJob();

        DateTimeOffset beforeStart = DateTimeOffset.UtcNow;

        job.Start();

        DateTimeOffset afterStart = DateTimeOffset.UtcNow;

        Assert.NotNull(job.StartedAtUtc);

        Assert.InRange(job.StartedAtUtc.Value, beforeStart, afterStart);

        Assert.Equal(TimeSpan.Zero, job.StartedAtUtc.Value.Offset);
    }

    [Fact]
    public void StartShouldKeepCompletedAtUtcAsNull()
    {
        AnalysisJob job = CreateJob();

        job.Start();

        Assert.Null(job.CompletedAtUtc);
    }

    [Fact]
    public void StartWhenRunningShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateRunningJob();

        Assert.Throws<InvalidOperationException>(() => job.Start());
    }

    [Fact]
    public void StartWhenSucceededShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateSucceededJob();

        Assert.Throws<InvalidOperationException>(() => job.Start());
    }

    [Fact]
    public void StartWhenFailedShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateFailedJob();

        Assert.Throws<InvalidOperationException>(() => job.Start());
    }

    #endregion

    #region Complete

    [Fact]
    public void CompleteWhenPendingShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateJob();

        Assert.Throws<InvalidOperationException>(() => job.Complete());
    }

    [Fact]
    public void CompleteShouldChangeStatusToSucceeded()
    {
        AnalysisJob job = CreateRunningJob();

        job.Complete();

        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);
    }

    [Fact]
    public void CompleteShouldSetCompletedAtUtc()
    {
        AnalysisJob job = CreateRunningJob();

        DateTimeOffset beforeCompletion = DateTimeOffset.UtcNow;

        job.Complete();

        DateTimeOffset afterCompletion = DateTimeOffset.UtcNow;

        Assert.NotNull(job.CompletedAtUtc);

        Assert.InRange(job.CompletedAtUtc.Value, beforeCompletion, afterCompletion);

        Assert.Equal(TimeSpan.Zero, job.CompletedAtUtc.Value.Offset);
    }

    [Fact]
    public void CompleteShouldKeepFailureReasonAsNull()
    {
        AnalysisJob job = CreateRunningJob();

        job.Complete();

        Assert.Null(job.FailureReason);
    }

    [Fact]
    public void CompleteWhenSucceededShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateSucceededJob();

        Assert.Throws<InvalidOperationException>(() => job.Complete());
    }

    [Fact]
    public void CompleteWhenFailedShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateFailedJob();

        Assert.Throws<InvalidOperationException>(() => job.Complete());
    }

    #endregion

    #region Fail

    [Fact]
    public void FailWhenPendingShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateJob();

        Assert.Throws<InvalidOperationException>(() => job.Fail("Analysis failed."));
    }

    [Fact]
    public void FailShouldChangeStatusToFailed()
    {
        AnalysisJob job = CreateRunningJob();

        job.Fail("Analysis failed.");

        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
    }

    [Fact]
    public void FailShouldStoreFailureReason()
    {
        const string reason = "Analysis failed.";

        AnalysisJob job = CreateRunningJob();

        job.Fail(reason);

        Assert.Equal(reason, job.FailureReason);
    }

    [Fact]
    public void FailShouldSetCompletedAtUtc()
    {
        AnalysisJob job = CreateRunningJob();

        DateTimeOffset beforeFailure = DateTimeOffset.UtcNow;

        job.Fail("Analysis failed.");

        DateTimeOffset afterFailure = DateTimeOffset.UtcNow;

        Assert.NotNull(job.CompletedAtUtc);

        Assert.InRange(job.CompletedAtUtc.Value, beforeFailure, afterFailure);

        Assert.Equal(TimeSpan.Zero, job.CompletedAtUtc.Value.Offset);
    }

    [Fact]
    public void FailWithNullReasonShouldThrowArgumentException()
    {
        AnalysisJob job = CreateRunningJob();

        Assert.ThrowsAny<ArgumentException>(() => job.Fail(null!));
    }

    [Fact]
    public void FailWithEmptyReasonShouldThrowArgumentException()
    {
        AnalysisJob job = CreateRunningJob();

        Assert.Throws<ArgumentException>(() => job.Fail(string.Empty));
    }

    [Fact]
    public void FailWithWhitespaceReasonShouldThrowArgumentException()
    {
        AnalysisJob job = CreateRunningJob();

        Assert.Throws<ArgumentException>(() => job.Fail("  "));
    }

    [Fact]
    public void FailWhenSucceededShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateSucceededJob();

        Assert.Throws<InvalidOperationException>(() => job.Fail("Analysis failed."));
    }

    [Fact]
    public void FailWhenAlreadyFailedShouldThrowInvalidOperationException()
    {
        AnalysisJob job = CreateFailedJob();

        Assert.Throws<InvalidOperationException>(() => job.Fail("Another failure."));
    }

    #endregion

    #region State Consistency

    [Fact]
    public void InvalidStartShouldNotChangeStatusOrTimestamps()
    {
        AnalysisJob job = CreateRunningJob();

        DateTimeOffset? previousStartAt = job.StartedAtUtc;
        DateTimeOffset? previousCompletedAt = job.CompletedAtUtc;

        Assert.Throws<InvalidOperationException>(() => job.Start());

        Assert.Equal(AnalysisJobStatus.Running, job.Status);
        Assert.Equal(previousStartAt, job.StartedAtUtc);
        Assert.Equal(previousCompletedAt, job.CompletedAtUtc);
    }

    [Fact]
    public void InvalidCompleteShouldNotChangeStatusOrTimestamps()
    {
        AnalysisJob job = CreateJob();

        DateTimeOffset? previousStartAt = job.StartedAtUtc;
        DateTimeOffset? previousCompletedAt = job.CompletedAtUtc;

        Assert.Throws<InvalidOperationException>(() => job.Complete());

        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(previousStartAt, job.StartedAtUtc);
        Assert.Equal(previousCompletedAt, job.CompletedAtUtc);
    }

    [Fact]
    public void InvalidFailShouldNotChangeStatusTimestampOrFailureReason()
    {
        AnalysisJob job = CreateJob();

        DateTimeOffset? previousStartedAt = job.StartedAtUtc;
        DateTimeOffset? previousCompletedAt = job.CompletedAtUtc;
        var previousFailureReason = job.FailureReason;

        Assert.Throws<InvalidOperationException>(() => job.Fail("Analysis failed."));

        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(previousStartedAt, job.StartedAtUtc);
        Assert.Equal(previousCompletedAt, job.CompletedAtUtc);
        Assert.Equal(previousFailureReason, job.FailureReason);
    }

    #endregion

    #region Test Helpers

    private static AnalysisJob CreateJob()
    {
        return new AnalysisJob(AnalysisJobId.New(), DeviceId.New());
    }

    private static AnalysisJob CreateRunningJob()
    {
        AnalysisJob job = CreateJob();

        job.Start();

        return job;
    }

    private static AnalysisJob CreateSucceededJob()
    {
        AnalysisJob job = CreateRunningJob();

        job.Complete();

        return job;
    }

    private static AnalysisJob CreateFailedJob()
    {
        AnalysisJob job = CreateRunningJob();

        job.Fail("Analysis failed.");

        return job;
    }

    #endregion
}
