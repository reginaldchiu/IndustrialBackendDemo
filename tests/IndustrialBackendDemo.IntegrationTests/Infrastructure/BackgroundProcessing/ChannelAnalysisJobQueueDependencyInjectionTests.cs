
using System.Threading.Channels;
using IndustrialBackendDemo.Application.Abstractions.Processing;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Infrastructure;
using IndustrialBackendDemo.Infrastructure.BackgroundProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialBackendDemo.UnitTests.Infrastructure.BackgroundProcessing;

public sealed class ChannelAnalysisJobQueueDependencyInjectionTests
{
    private const string _connectionString =
        "Host=localhost;Port=5432;Database=test;Username=test;Password=test";

    [Fact]
    public void AddInfrastructureShouldRegisterChannelAsSingleton()
    {
        ServiceCollection services = CreateServices();

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(Channel<AnalysisJobId>)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructureShouldRegisterReaderAsSingleton()
    {
        ServiceCollection services = CreateServices();

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(ChannelReader<AnalysisJobId>)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructureShouldRegisterWriterAsSingleton()
    {
        ServiceCollection services = CreateServices();

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(ChannelWriter<AnalysisJobId>)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructureShouldRegisterQueueAsSingleton()
    {
        ServiceCollection services = CreateServices();

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(IAnalysisJobQueue)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(ChannelAnalysisJobQueue),
            descriptor.ImplementationType
        );
    }

    [Fact]
    public void AddInfrastructureShouldUseSameChannelInstance()
    {
        ServiceCollection services = CreateServices();

        using ServiceProvider provider = services.BuildServiceProvider();

        Channel<AnalysisJobId> channel =
            provider.GetRequiredService<Channel<AnalysisJobId>>();

        ChannelReader<AnalysisJobId> reader =
            provider.GetRequiredService<ChannelReader<AnalysisJobId>>();

        ChannelWriter<AnalysisJobId> writer =
            provider.GetRequiredService<ChannelWriter<AnalysisJobId>>();

        Assert.Same(channel.Reader, reader);
        Assert.Same(channel.Writer, writer);
    }

    [Fact]
    public async Task AddInfrastructureShouldConnectQueueToRegisteredChannel()
    {
        ServiceCollection services = CreateServices();

        using ServiceProvider provider = services.BuildServiceProvider();

        IAnalysisJobQueue queue =
            provider.GetRequiredService<IAnalysisJobQueue>();

        ChannelReader<AnalysisJobId> reader =
            provider.GetRequiredService<ChannelReader<AnalysisJobId>>();

        var expected = AnalysisJobId.New();

        await queue.EnqueueAsync(expected);

        AnalysisJobId actual = await reader.ReadAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AddInfrastructureShouldResolveSameQueueInstance()
    {
        ServiceCollection services = CreateServices();

        using ServiceProvider provider = services.BuildServiceProvider();

        IAnalysisJobQueue first =
            provider.GetRequiredService<IAnalysisJobQueue>();

        IAnalysisJobQueue second =
            provider.GetRequiredService<IAnalysisJobQueue>();

        Assert.Same(first, second);
    }

    private static ServiceCollection CreateServices()
    {
        Dictionary<string, string?> settings = new()
        {
            ["Database:ConnectionString"] = _connectionString
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();

        services.AddInfrastructure(configuration);

        return services;
    }
}
