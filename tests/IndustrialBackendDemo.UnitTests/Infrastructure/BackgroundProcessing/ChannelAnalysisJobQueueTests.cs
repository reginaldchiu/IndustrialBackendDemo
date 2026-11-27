
using System.Threading.Channels;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Infrastructure.BackgroundProcessing;

namespace IndustrialBackendDemo.UnitTests.Infrastructure.BackgroundProcessing;

public sealed class ChannelAnalysisJobQueueTests
{
    [Fact]
    public async Task EnqueueAsyncShouldWriteJobIdToChannel()
    {
        Channel<AnalysisJobId> channel = CreateChannel();
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        var jobId = AnalysisJobId.New();

        await queue.EnqueueAsync(jobId);

        Assert.True(channel.Reader.TryRead(out AnalysisJobId actual));
        Assert.Equal(jobId, actual);
    }

    [Fact]
    public async Task EnqueueAsyncShouldPreserveJobId()
    {
        Channel<AnalysisJobId> channel = CreateChannel();
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        var expected = AnalysisJobId.New();

        await queue.EnqueueAsync(expected);

        AnalysisJobId actual = await channel.Reader.ReadAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task EnqueueAsyncShouldRejectDefaultJobId()
    {
        Channel<AnalysisJobId> channel = CreateChannel();
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        AnalysisJobId jobId = default;

        Func<Task> act = async () =>
            await queue.EnqueueAsync(jobId);

        ArgumentException exception =
            await Assert.ThrowsAsync<ArgumentException>(act);

        Assert.Equal("jobId", exception.ParamName);
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task EnqueueAsyncShouldThrowWhenTokenIsAlreadyCanceled()
    {
        Channel<AnalysisJobId> channel = CreateChannel(capacity: 1);
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        await queue.EnqueueAsync(AnalysisJobId.New());

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        Func<Task> act = async () =>
            await queue.EnqueueAsync(
                AnalysisJobId.New(),
                cancellationTokenSource.Token
            );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task EnqueueAsyncShouldWaitWhenChannelIsFull()
    {
        Channel<AnalysisJobId> channel = CreateChannel(capacity: 1);
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        await queue.EnqueueAsync(AnalysisJobId.New());

        ValueTask pendingWrite = queue.EnqueueAsync(AnalysisJobId.New());

        Assert.False(pendingWrite.IsCompleted);

        Assert.True(channel.Reader.TryRead(out _));

        await pendingWrite;

        Assert.True(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task EnqueueAsyncShouldCancelWhenWaitingForAvailableCapacity()
    {
        Channel<AnalysisJobId> channel = CreateChannel(capacity: 1);
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        await queue.EnqueueAsync(AnalysisJobId.New());

        using CancellationTokenSource cancellationTokenSource = new();

        ValueTask pendingWrite = queue.EnqueueAsync(
            AnalysisJobId.New(),
            cancellationTokenSource.Token
        );

        Assert.False(pendingWrite.IsCompleted);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await pendingWrite
        );

        Assert.True(channel.Reader.TryRead(out _));
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task EnqueueAsyncShouldResumeWhenCapacityBecomesAvailable()
    {
        Channel<AnalysisJobId> channel = CreateChannel(capacity: 1);
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        var firstJobId = AnalysisJobId.New();
        var secondJobId = AnalysisJobId.New();

        await queue.EnqueueAsync(firstJobId);

        ValueTask pendingWrite = queue.EnqueueAsync(secondJobId);

        Assert.False(pendingWrite.IsCompleted);

        AnalysisJobId firstResult = await channel.Reader.ReadAsync();

        Assert.Equal(firstJobId, firstResult);

        await pendingWrite;

        AnalysisJobId secondResult = await channel.Reader.ReadAsync();

        Assert.Equal(secondJobId, secondResult);
    }

    [Fact]
    public async Task EnqueueAsyncShouldSupportMultipleConcurrentWriters()
    {
        const int jobCount = 20;

        Channel<AnalysisJobId> channel = CreateChannel(capacity: 100);
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        AnalysisJobId[] expectedJobIds = Enumerable
            .Range(0, jobCount)
            .Select(_ => AnalysisJobId.New())
            .ToArray();

        Task[] producers = expectedJobIds
            .Select(jobId => queue.EnqueueAsync(jobId).AsTask())
            .ToArray();

        await Task.WhenAll(producers);

        HashSet<AnalysisJobId> actualJobIds = [];

        while (channel.Reader.TryRead(out AnalysisJobId jobId))
        {
            actualJobIds.Add(jobId);
        }

        Assert.Equal(jobCount, actualJobIds.Count);

        Assert.Equal(
            expectedJobIds.ToHashSet(),
            actualJobIds
        );
    }

    [Fact]
    public async Task EnqueueAsyncShouldThrowWhenChannelIsClosed()
    {
        Channel<AnalysisJobId> channel = CreateChannel();
        ChannelAnalysisJobQueue queue = new(channel.Writer);

        channel.Writer.TryComplete();

        Func<Task> act = async () =>
            await queue.EnqueueAsync(AnalysisJobId.New());

        await Assert.ThrowsAsync<ChannelClosedException>(act);
    }

    [Fact]
    public void ConstructorShouldRejectNullWriter()
    {
        Action act = () =>
        {
            _ = new ChannelAnalysisJobQueue(null!);
        };

        Assert.Throws<ArgumentNullException>(act);
    }

    private static Channel<AnalysisJobId> CreateChannel(int capacity = 100)
    {
        return Channel.CreateBounded<AnalysisJobId>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            }
        );
    }
}
