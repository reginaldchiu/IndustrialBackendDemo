
using System.Collections.Concurrent;
using System.Threading.Channels;
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Abstractions.Processing;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Domain.Devices;
using IndustrialBackendDemo.Infrastructure.BackgroundProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndustrialBackendDemo.UnitTests.Infrastructure.BackgroundProcessing;

public sealed class AnalysisJobBackgroundServiceTests
{
    [Fact]
    public async Task WorkerShouldStartAndStopNormally()
    {
        await using WorkerHarness harness = new();

        await harness.StartAsync();

        await harness.StopAsync();
    }

    [Fact]
    public async Task WorkerShouldProcessPendingJobSuccessfully()
    {
        await using WorkerHarness harness = new();

        AnalysisJob job = CreateJob();

        await harness.Store.AddAsync(job);

        Task completed = harness.Store.WaitForStatusAsync(
            job.Id,
            AnalysisJobStatus.Succeeded
        );

        await harness.StartAsync();
        await harness.EnqueueAsync(job.Id);

        await completed;

        AnalysisJob? actual = harness.Store.Get(job.Id);

        Assert.NotNull(actual);
        Assert.Equal(AnalysisJobStatus.Succeeded, actual.Status);
        Assert.NotNull(actual.StartedAtUtc);
        Assert.NotNull(actual.CompletedAtUtc);

        Assert.Equal(1, harness.Probe.ProcessCallCount);
    }

    [Fact]
    public async Task WorkerShouldMarkJobAsFailedWhenProcessorThrows()
    {
        await using WorkerHarness harness = new();

        AnalysisJob job = CreateJob();

        await harness.Store.AddAsync(job);

        const string failureReason = "Demo processing failed.";

        harness.Probe.ProcessAsync = (_, _) =>
            throw new InvalidOperationException(failureReason);

        Task failed = harness.Store.WaitForStatusAsync(
            job.Id,
            AnalysisJobStatus.Failed
        );

        await harness.StartAsync();
        await harness.EnqueueAsync(job.Id);

        await failed;

        AnalysisJob? actual = harness.Store.Get(job.Id);

        Assert.NotNull(actual);
        Assert.Equal(AnalysisJobStatus.Failed, actual.Status);
        Assert.Equal(failureReason, actual.FailureReason);
        Assert.NotNull(actual.StartedAtUtc);
        Assert.NotNull(actual.CompletedAtUtc);
    }

    [Fact]
    public async Task WorkerShouldSkipMissingAndNonPendingJobs()
    {
        await using WorkerHarness harness = new();

        AnalysisJob succeededJob = CreateJob();
        succeededJob.Start();
        succeededJob.Complete();

        AnalysisJob runningJob = CreateJob();
        runningJob.Start();

        AnalysisJob validJob = CreateJob();

        await harness.Store.AddAsync(succeededJob);
        await harness.Store.AddAsync(runningJob);
        await harness.Store.AddAsync(validJob);

        Task completed = harness.Store.WaitForStatusAsync(
            validJob.Id,
            AnalysisJobStatus.Succeeded
        );

        await harness.StartAsync();

        await harness.EnqueueAsync(AnalysisJobId.New());
        await harness.EnqueueAsync(succeededJob.Id);
        await harness.EnqueueAsync(runningJob.Id);
        await harness.EnqueueAsync(validJob.Id);

        await completed;

        Assert.Equal(1, harness.Probe.ProcessCallCount);

        Assert.Equal(
            AnalysisJobStatus.Succeeded,
            harness.Store.Get(succeededJob.Id)!.Status
        );

        Assert.Equal(
            AnalysisJobStatus.Running,
            harness.Store.Get(runningJob.Id)!.Status
        );

        Assert.Equal(
            AnalysisJobStatus.Succeeded,
            harness.Store.Get(validJob.Id)!.Status
        );
    }

    [Fact]
    public async Task WorkerShouldContinueAfterProcessorFailure()
    {
        await using WorkerHarness harness = new();

        AnalysisJob failedJob = CreateJob();
        AnalysisJob successfulJob = CreateJob();

        await harness.Store.AddAsync(failedJob);
        await harness.Store.AddAsync(successfulJob);

        harness.Probe.ProcessAsync = (job, _) =>
        {
            if (job.Id == failedJob.Id)
            {
                throw new InvalidOperationException("Expected failure.");
            }

            return Task.CompletedTask;
        };

        Task failed = harness.Store.WaitForStatusAsync(
            failedJob.Id,
            AnalysisJobStatus.Failed
        );

        Task succeeded = harness.Store.WaitForStatusAsync(
            successfulJob.Id,
            AnalysisJobStatus.Succeeded
        );

        await harness.StartAsync();

        await harness.EnqueueAsync(failedJob.Id);
        await harness.EnqueueAsync(successfulJob.Id);

        await Task.WhenAll(failed, succeeded);

        Assert.Equal(
            AnalysisJobStatus.Failed,
            harness.Store.Get(failedJob.Id)!.Status
        );

        Assert.Equal(
            AnalysisJobStatus.Succeeded,
            harness.Store.Get(successfulJob.Id)!.Status
        );

        Assert.Equal(2, harness.Probe.ProcessCallCount);
    }

    [Fact]
    public async Task WorkerShouldKeepJobRunningWhenHostStopsDuringProcessing()
    {
        await using WorkerHarness harness = new();

        AnalysisJob job = CreateJob();

        await harness.Store.AddAsync(job);

        Task running = harness.Store.WaitForStatusAsync(
            job.Id,
            AnalysisJobStatus.Running
        );

        harness.Probe.ProcessAsync = async (_, cancellationToken) =>
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken
            );
        };

        await harness.StartAsync();
        await harness.EnqueueAsync(job.Id);

        await running;

        await harness.Probe.WaitForProcessorStartAsync(job.Id);

        await harness.StopAsync();

        AnalysisJob? actual = harness.Store.Get(job.Id);

        Assert.NotNull(actual);
        Assert.Equal(AnalysisJobStatus.Running, actual.Status);
        Assert.Null(actual.CompletedAtUtc);
        Assert.Null(actual.FailureReason);
    }

    [Fact]
    public async Task WorkerShouldCreateIndependentScopeForEachJob()
    {
        await using WorkerHarness harness = new();

        AnalysisJob firstJob = CreateJob();
        AnalysisJob secondJob = CreateJob();

        await harness.Store.AddAsync(firstJob);
        await harness.Store.AddAsync(secondJob);

        Task firstCompleted = harness.Store.WaitForStatusAsync(
            firstJob.Id,
            AnalysisJobStatus.Succeeded
        );

        Task secondCompleted = harness.Store.WaitForStatusAsync(
            secondJob.Id,
            AnalysisJobStatus.Succeeded
        );

        await harness.StartAsync();

        await harness.EnqueueAsync(firstJob.Id);
        await harness.EnqueueAsync(secondJob.Id);

        await Task.WhenAll(firstCompleted, secondCompleted);

        Guid[] repositoryScopes =
            harness.Probe.RepositoryScopeIds.ToArray();

        Guid[] processorScopes =
            harness.Probe.ProcessorScopeIds.ToArray();

        Assert.Equal(2, repositoryScopes.Length);
        Assert.Equal(2, processorScopes.Length);

        // Repository and Processor in the same job share one scope.
        Assert.Equal(repositoryScopes, processorScopes);

        // Different jobs must use different scopes.
        Assert.Equal(2, repositoryScopes.Distinct().Count());
    }

    [Fact]
    public async Task WorkerShouldContinueWhenRepositoryUpdateThrows()
    {
        await using WorkerHarness harness = new();

        AnalysisJob failedJob = CreateJob();
        AnalysisJob successfulJob = CreateJob();

        await harness.Store.AddAsync(failedJob);
        await harness.Store.AddAsync(successfulJob);

        harness.Store.ShouldFailUpdate = job =>
            job.Id == failedJob.Id;

        Task succeeded = harness.Store.WaitForStatusAsync(
            successfulJob.Id,
            AnalysisJobStatus.Succeeded
        );

        await harness.StartAsync();

        await harness.EnqueueAsync(failedJob.Id);
        await harness.EnqueueAsync(successfulJob.Id);

        await succeeded;

        // The first job's initial Running update failed.
        // The stored job therefore remains Pending.
        Assert.Equal(
            AnalysisJobStatus.Pending,
            harness.Store.Get(failedJob.Id)!.Status
        );

        // The worker continued and processed the second job.
        Assert.Equal(
            AnalysisJobStatus.Succeeded,
            harness.Store.Get(successfulJob.Id)!.Status
        );

        Assert.Equal(1, harness.Probe.ProcessCallCount);
    }

    [Fact]
    public async Task WorkerShouldMarkJobAsFailedWhenProcessorCancelsIndependently()
    {
        await using WorkerHarness harness = new();

        AnalysisJob job = CreateJob();

        await harness.Store.AddAsync(job);

        harness.Probe.ProcessAsync = (_, _) =>
            throw new OperationCanceledException("Processor canceled.");

        Task failed = harness.Store.WaitForStatusAsync(
            job.Id,
            AnalysisJobStatus.Failed
        );

        await harness.StartAsync();
        await harness.EnqueueAsync(job.Id);

        await failed;

        AnalysisJob? actual = harness.Store.Get(job.Id);

        Assert.NotNull(actual);
        Assert.Equal(AnalysisJobStatus.Failed, actual.Status);
        Assert.Equal("Processor canceled.", actual.FailureReason);
    }

    private static AnalysisJob CreateJob()
    {
        return new AnalysisJob(
            AnalysisJobId.New(),
            DeviceId.New()
        );
    }
}

internal sealed class WorkerHarness : IAsyncDisposable
{
    private readonly Channel<AnalysisJobId> _channel =
        Channel.CreateUnbounded<AnalysisJobId>();

    private readonly ServiceProvider _serviceProvider;

    private readonly AnalysisJobBackgroundService _worker;

    private bool _started;
    private bool _stopped;

    public AnalysisJobStore Store { get; }

    public WorkerProbe Probe { get; }

    public WorkerHarness()
    {
        Store = new AnalysisJobStore();
        Probe = new WorkerProbe();

        ServiceCollection services = new();

        services.AddLogging();

        services.AddSingleton(Store);
        services.AddSingleton(Probe);

        services.AddScoped<ScopeMarker>();

        services.AddScoped<
            IAnalysisJobRepository,
            FakeAnalysisJobRepository
        >();

        services.AddScoped<
            IAnalysisJobProcessor,
            FakeAnalysisJobProcessor
        >();

        _serviceProvider = services.BuildServiceProvider();

        _worker = new AnalysisJobBackgroundService(
            _channel.Reader,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AnalysisJobBackgroundService>.Instance
        );
    }

    public async Task StartAsync()
    {
        await _worker.StartAsync(CancellationToken.None);
        _started = true;
    }

    public async Task EnqueueAsync(AnalysisJobId jobId)
    {
        await _channel.Writer.WriteAsync(jobId);
    }

    public async Task StopAsync()
    {
        if (!_started || _stopped)
        {
            return;
        }

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));

        await _worker.StopAsync(timeout.Token);

        _stopped = true;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        _worker.Dispose();
        await _serviceProvider.DisposeAsync();
    }
}

internal sealed class AnalysisJobStore
{
    private readonly ConcurrentDictionary<AnalysisJobId, AnalysisJob> _jobs = [];

    private readonly ConcurrentDictionary<
        (AnalysisJobId JobId, AnalysisJobStatus Status),
        TaskCompletionSource
    > _statusSignals = [];

    public Func<AnalysisJob, bool>? ShouldFailUpdate { get; set; }

    public Task AddAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _jobs[job.Id] = Clone(job);

        return Task.CompletedTask;
    }

    public Task<AnalysisJob?> GetByIdAsync(
        AnalysisJobId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _jobs.TryGetValue(id, out AnalysisJob? job);

        return Task.FromResult(
            job is null ? null : Clone(job)
        );
    }

    public Task UpdateAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFailUpdate?.Invoke(job) == true)
        {
            throw new InvalidOperationException(
                "Simulated repository update failure."
            );
        }

        _jobs[job.Id] = Clone(job);

        if (_statusSignals.TryGetValue(
            (job.Id, job.Status),
            out TaskCompletionSource? signal))
        {
            signal.TrySetResult();
        }

        return Task.CompletedTask;
    }

    public AnalysisJob? Get(AnalysisJobId id)
    {
        return _jobs.TryGetValue(id, out AnalysisJob? job)
            ? Clone(job)
            : null;
    }

    public async Task WaitForStatusAsync(
        AnalysisJobId id,
        AnalysisJobStatus status)
    {
        TaskCompletionSource signal = _statusSignals.GetOrAdd(
            (id, status),
            _ => new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            )
        );

        AnalysisJob? current = Get(id);

        if (current?.Status == status)
        {
            return;
        }

        await signal.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static AnalysisJob Clone(AnalysisJob job)
    {
        return AnalysisJob.Reconstitute(
            job.Id,
            job.DeviceId,
            job.Status,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.FailureReason
        );
    }
}

internal sealed class FakeAnalysisJobRepository(
    AnalysisJobStore store,
    ScopeMarker scopeMarker,
    WorkerProbe probe
) : IAnalysisJobRepository
{
    public Task AddAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        return store.AddAsync(job, cancellationToken);
    }

    public Task<AnalysisJob?> GetByIdAsync(
        AnalysisJobId id,
        CancellationToken cancellationToken = default)
    {
        probe.RepositoryScopeIds.Enqueue(scopeMarker.Id);

        return store.GetByIdAsync(id, cancellationToken);
    }

    public Task UpdateAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(job, cancellationToken);
    }
}

internal sealed class FakeAnalysisJobProcessor(
    WorkerProbe probe,
    ScopeMarker scopeMarker
) : IAnalysisJobProcessor
{
    public async Task ProcessAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        probe.RecordProcessorStart(job.Id, scopeMarker.Id);

        await probe.ProcessAsync(job, cancellationToken);
    }
}

internal sealed class WorkerProbe
{
    private readonly ConcurrentDictionary<
        AnalysisJobId,
        TaskCompletionSource
    > _processorStartedSignals = [];

    private int _processCallCount;

    public int ProcessCallCount =>
        Volatile.Read(ref _processCallCount);

    public Func<AnalysisJob, CancellationToken, Task> ProcessAsync { get; set; } =
        (_, _) => Task.CompletedTask;

    public ConcurrentQueue<Guid> RepositoryScopeIds { get; } = [];

    public ConcurrentQueue<Guid> ProcessorScopeIds { get; } = [];

    public void RecordProcessorStart(
        AnalysisJobId jobId,
        Guid scopeId)
    {
        Interlocked.Increment(ref _processCallCount);

        ProcessorScopeIds.Enqueue(scopeId);

        TaskCompletionSource signal = _processorStartedSignals.GetOrAdd(
            jobId,
            _ => new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            )
        );

        signal.TrySetResult();
    }

    public async Task WaitForProcessorStartAsync(AnalysisJobId jobId)
    {
        TaskCompletionSource signal = _processorStartedSignals.GetOrAdd(
            jobId,
            _ => new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            )
        );

        await signal.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}

internal sealed class ScopeMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}
