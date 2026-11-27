using System.Threading.Channels;
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Abstractions.Processing;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndustrialBackendDemo.Infrastructure.BackgroundProcessing;

public sealed partial class AnalysisJobBackgroundService(
    ChannelReader<AnalysisJobId> reader,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalysisJobBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await foreach (
                AnalysisJobId jobId in reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobAsync(jobId, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    UnexpectedProcessingError(
                        logger,
                        jobId.Value,
                        exception
                    );
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down.
        }
    }

    private async Task ProcessJobAsync(
        AnalysisJobId jobId,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope =
            scopeFactory.CreateAsyncScope();

        IAnalysisJobRepository repository =
            scope.ServiceProvider
                .GetRequiredService<IAnalysisJobRepository>();

        IAnalysisJobProcessor processor =
            scope.ServiceProvider
                .GetRequiredService<IAnalysisJobProcessor>();

        AnalysisJob? job = await repository.GetByIdAsync(
            jobId,
            cancellationToken
        );

        if (job is null)
        {
            JobNotFound(logger, jobId.Value);
            return;
        }

        if (job.Status != AnalysisJobStatus.Pending)
        {
            JobSkipped(logger, jobId.Value, job.Status);
            return;
        }

        job.Start();

        await repository.UpdateAsync(job, cancellationToken);

        try
        {
            await processor.ProcessAsync(job, cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown is not a business failure.
            // Keep the persisted job in Running state.
            throw;
        }
        catch (Exception exception)
        {
            var failureReason = GetFailureReason(exception);

            job.Fail(failureReason);

            await repository.UpdateAsync(job, cancellationToken);

            JobProcessingFailed(logger, jobId.Value, exception);
            return;
        }

        job.Complete();

        await repository.UpdateAsync(job, cancellationToken);

        JobCompleted(logger, jobId.Value);
    }

    private static string GetFailureReason(Exception exception)
    {
        return string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Analysis job {JobId} was not found. Skipping.")]
    private static partial void JobNotFound(
        ILogger logger,
        Guid jobId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Analysis job {JobId} has status {Status}. Skipping.")]
    private static partial void JobSkipped(
        ILogger logger,
        Guid jobId,
        AnalysisJobStatus status);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Analysis job {JobId} failed.")]
    private static partial void JobProcessingFailed(
        ILogger logger,
        Guid jobId,
        Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Analysis job {JobId} completed successfully.")]
    private static partial void JobCompleted(
        ILogger logger,
        Guid jobId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Unexpected error while processing analysis job {JobId}.")]
    private static partial void UnexpectedProcessingError(
        ILogger logger,
        Guid jobId,
        Exception exception);
}
