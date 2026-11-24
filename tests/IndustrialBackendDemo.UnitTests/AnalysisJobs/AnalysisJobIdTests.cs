using IndustrialBackendDemo.Domain.AnalysisJobs;

namespace IndustrialBackendDemo.UnitTests.AnalysisJobs;

public sealed class AnalysisJobIdTests
{
    [Fact]
    public void ConstructorWithValidGuidShouldCreateId()
    {
        var value = Guid.NewGuid();

        AnalysisJobId id = new(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void ConstructorWithEmptyGuidShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AnalysisJobId(Guid.Empty));
    }

    [Fact]
    public void NewShouldReturnNonEmptyId()
    {
        var id = AnalysisJobId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void NewShouldReturnUniqueIds()
    {
        var firstId = AnalysisJobId.New();
        var secondId = AnalysisJobId.New();

        Assert.NotEqual(firstId, secondId);
    }
}
