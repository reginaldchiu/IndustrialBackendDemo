using IndustrialBackendDemo.Domain.AnalysisJobs;

namespace IndustrialBackendDemo.Application.Abstractions.Persistence;

public interface IAnalysisJobRepository
{
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken = default);

    Task<AnalysisJob?> GetByIdAsync(
        AnalysisJobId id,
        CancellationToken cancellationToken = default
    );

    Task UpdateAsync(AnalysisJob job, CancellationToken cancellationToken = default);
}
