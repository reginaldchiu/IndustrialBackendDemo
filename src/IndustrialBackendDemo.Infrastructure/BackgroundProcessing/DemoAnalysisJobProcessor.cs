
using IndustrialBackendDemo.Application.Abstractions.Processing;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using Microsoft.Extensions.Logging;

namespace IndustrialBackendDemo.Infrastructure.BackgroundProcessing;

public sealed partial class DemoAnalysisJobProcessor(
    ILogger<DemoAnalysisJobProcessor> logger
) : IAnalysisJobProcessor
{
    public async Task ProcessAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        AnalysisStarted(
            logger,
            job.Id.Value,
            job.DeviceId.Value
        );

        await Task.Delay(
            TimeSpan.FromMilliseconds(250),
            cancellationToken
        );

        AnalysisFinished(logger, job.Id.Value);
    }

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Starting demo analysis for job {JobId}, device {DeviceId}.")]
    private static partial void AnalysisStarted(
        ILogger logger,
        Guid jobId,
        Guid deviceId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Demo analysis for job {JobId} finished.")]
    private static partial void AnalysisFinished(
        ILogger logger,
        Guid jobId);
}
