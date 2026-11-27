
using IndustrialBackendDemo.Domain.AnalysisJobs;

namespace IndustrialBackendDemo.Application.Abstractions.Processing;

public interface IAnalysisJobProcessor
{
    Task ProcessAsync(
        AnalysisJob job,
        CancellationToken cancellationToken = default
    );
}
