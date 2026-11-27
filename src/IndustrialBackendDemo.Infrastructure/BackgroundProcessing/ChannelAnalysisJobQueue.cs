
using System.Threading.Channels;
using IndustrialBackendDemo.Application.Abstractions.Processing;
using IndustrialBackendDemo.Domain.AnalysisJobs;

namespace IndustrialBackendDemo.Infrastructure.BackgroundProcessing;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class ChannelAnalysisJobQueue : IAnalysisJobQueue
{
    private readonly ChannelWriter<AnalysisJobId> _writer;

    public ChannelAnalysisJobQueue(ChannelWriter<AnalysisJobId> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
    }

    public ValueTask EnqueueAsync(
        AnalysisJobId jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Analysis job ID cannot be empty.",
                nameof(jobId)
            );
        }

        return _writer.WriteAsync(jobId, cancellationToken);
    }
}

#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
