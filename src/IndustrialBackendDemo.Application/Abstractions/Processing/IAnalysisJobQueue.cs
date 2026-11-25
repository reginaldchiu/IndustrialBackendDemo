using IndustrialBackendDemo.Domain.AnalysisJobs;

#pragma warning disable CA1711

namespace IndustrialBackendDemo.Application.Abstractions.Processing;

public interface IAnalysisJobQueue
{
    ValueTask EnqueueAsync(AnalysisJobId jobId, CancellationToken cancellationToken = default);
}

#pragma warning restore CA1711
