using IndustrialBackendDemo.Infrastructure;
using IndustrialBackendDemo.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IndustrialBackendDemo.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    private const string ValidConnectionString = "Host=localhost;Port=5432;Database=test;Username=test;Password=test";

    private static IConfiguration CreateConfiguration(string connectionString)
    {
        Dictionary<string, string?> settings = new()
        {
            [$"{DatabaseOptions.SectionName}:ConnectionString"] = connectionString
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public void AddInfrastructureShouldReturnSameServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructureShouldRegisterNpgsqlDataSourceAsSingleton()
    {
        ServiceCollection services = new();

        services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(NpgsqlDataSource)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructureShouldRegisterConnectionFactoryAsSingleton()
    {
        ServiceCollection services = new();

        services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(IDatabaseConnectionFactory)
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(DatabaseConnectionFactory), descriptor.ImplementationType);
    }

    [Fact]
    public void AddInfrastructureShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void AddInfrastructureShouldThrowWhenConfigurationIsNull()
    {
        ServiceCollection services = new();

        Action act = () => services.AddInfrastructure(null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddInfrastructureShouldRejectEmptyConnectionString(string connectionString)
    {
        ServiceCollection services = new();

        services.AddInfrastructure(
            CreateConfiguration(connectionString)
        );

        using ServiceProvider provider = services.BuildServiceProvider();

        Func<DatabaseOptions> act = () =>
            provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        Assert.Throws<OptionsValidationException>(act);
    }

    [Fact]
    public void AddInfrastructureShouldCreateNpgsqlDataSource()
    {
        ServiceCollection services = new();

        services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        using ServiceProvider provider = services.BuildServiceProvider();

        NpgsqlDataSource first =
            provider.GetRequiredService<NpgsqlDataSource>();

        NpgsqlDataSource second =
            provider.GetRequiredService<NpgsqlDataSource>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddInfrastructureShouldCreateConnectionFactory()
    {
        ServiceCollection services = new();

        services.AddInfrastructure(
            CreateConfiguration(ValidConnectionString)
        );

        using ServiceProvider provider = services.BuildServiceProvider();

        IDatabaseConnectionFactory factory =
            provider.GetRequiredService<IDatabaseConnectionFactory>();

        Assert.IsType<DatabaseConnectionFactory>(factory);
    }
}
