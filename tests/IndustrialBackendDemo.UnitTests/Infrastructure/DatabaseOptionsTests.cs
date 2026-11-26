using IndustrialBackendDemo.Infrastructure.Persistence;

namespace IndustrialBackendDemo.UnitTests.Infrastructure;

public sealed class DatabaseOptionsTests
{
    [Fact]
    public void SectionNameShouldDatabase()
    {
        Assert.Equal("Database", DatabaseOptions.SectionName);
    }

    [Fact]
    public void ConnectionStringShouldDefaultToEmpty()
    {
        DatabaseOptions options = new();

        Assert.Equal(string.Empty, options.ConnectionString);
    }
}
