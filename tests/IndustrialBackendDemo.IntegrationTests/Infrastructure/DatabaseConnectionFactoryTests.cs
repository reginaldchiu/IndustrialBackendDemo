using System.Data;
using System.Globalization;
using IndustrialBackendDemo.Infrastructure;
using IndustrialBackendDemo.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IndustrialBackendDemo.IntegrationTests.Infrastructure;

public sealed class DatabaseConnectionFactoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("industrial_backend_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Dictionary<string, string?> settings = new()
        {
            [$"{DatabaseOptions.SectionName}:ConnectionString"] =
                _postgres.GetConnectionString()
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();

        services.AddInfrastructure(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private IDatabaseConnectionFactory GetConnectionFactory()
    {
        return _serviceProvider!
            .GetRequiredService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task CreateConnectionAsyncShouldReturnOpenConnection()
    {
        // Arrange
        IDatabaseConnectionFactory factory = GetConnectionFactory();

        // Act
        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync();

        // Assert
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task CreateConnectionAsyncShouldConnectToPostgreSql()
    {
        // Arrange
        IDatabaseConnectionFactory factory = GetConnectionFactory();

        // Act
        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync();

        await using NpgsqlCommand command = new("SELECT 1", connection);

        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.Equal(1, Convert.ToInt32(result, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CreateConnectionAsyncShouldReturnIndependentConnections()
    {
        // Arrange
        IDatabaseConnectionFactory factory = GetConnectionFactory();

        // Act
        await using NpgsqlConnection first =
            await factory.CreateConnectionAsync();

        await using NpgsqlConnection second =
            await factory.CreateConnectionAsync();

        // Assert
        Assert.NotSame(first, second);
        Assert.Equal(ConnectionState.Open, first.State);
        Assert.Equal(ConnectionState.Open, second.State);
    }

    [Fact]
    public async Task CreateConnectionAsyncShouldRespectCancellationToken()
    {
        // Arrange
        IDatabaseConnectionFactory factory = GetConnectionFactory();

        using CancellationTokenSource cancellationTokenSource = new();

        await cancellationTokenSource.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
                await factory.CreateConnectionAsync(
                    cancellationTokenSource.Token
                )
        );
    }

    [Fact]
    public async Task CreateConnectionAsyncShouldAllowConnectionReuseAfterDisposal()
    {
        // Arrange
        IDatabaseConnectionFactory factory = GetConnectionFactory();

        // Act
        await using (NpgsqlConnection first =
            await factory.CreateConnectionAsync())
        {
            Assert.Equal(ConnectionState.Open, first.State);
        }

        await using NpgsqlConnection second =
            await factory.CreateConnectionAsync();

        // Assert
        Assert.Equal(ConnectionState.Open, second.State);
    }
}
