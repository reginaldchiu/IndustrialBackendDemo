namespace IndustrialBackendDemo.Domain.AnalysisJobs;

public readonly record struct AnalysisJobId
{
    public Guid Value { get; }

    public AnalysisJobId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Analysis job ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static AnalysisJobId New()
    {
        return new AnalysisJobId(Guid.NewGuid());
    }
}
