
using Dapper;
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Domain.Devices;
using IndustrialBackendDemo.Infrastructure.Persistence;
using IndustrialBackendDemo.Infrastructure.Persistence.Repositories;
using IndustrialBackendDemo.IntegrationTests.Fixtures;
using Npgsql;

namespace IndustrialBackendDemo.IntegrationTests.Persistence.Repositories;

public sealed class DeviceRepositoryTests(
    PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task AddAsyncShouldPersistDevice()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device device = CreateDevice();

        await repository.AddAsync(device);

        Device? result = await repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
        Assert.Equal(device.Name, result.Name);
        Assert.Equal(device.SerialNumber, result.SerialNumber);
        Assert.Equal(device.Status, result.Status);
    }

    [Fact]
    public async Task AddAsyncShouldRejectDuplicateSerialNumber()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        const string serialNumber = "SENSOR-DUPLICATE-001";

        Device first = CreateDevice(serialNumber);
        Device second = CreateDevice(serialNumber);

        await repository.AddAsync(first);

        DeviceSerialNumberAlreadyExistsException exception =
            await Assert.ThrowsAsync<DeviceSerialNumberAlreadyExistsException>(
                () => repository.AddAsync(second));

        Assert.Contains(serialNumber, exception.Message);
    }

    [Fact]
    public async Task GetByIdAsyncShouldReturnNullWhenDeviceDoesNotExist()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device? result = await repository.GetByIdAsync(DeviceId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsyncShouldRestoreAllProperties()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        DateTimeOffset createdAt = new(
            2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset updatedAt = new(
            2026, 1, 2, 10, 0, 0, TimeSpan.Zero);

        var device = Device.Reconstitute(
            DeviceId.New(),
            "Industrial Sensor",
            "SENSOR-001",
            DeviceStatus.Online,
            createdAt,
            updatedAt);

        await repository.AddAsync(device);

        Device? result = await repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
        Assert.Equal(device.Name, result.Name);
        Assert.Equal(device.SerialNumber, result.SerialNumber);
        Assert.Equal(DeviceStatus.Online, result.Status);
        Assert.Equal(createdAt, result.CreatedAtUtc);
        Assert.Equal(updatedAt, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetBySerialNumberAsyncShouldReturnDevice()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device device = CreateDevice();

        await repository.AddAsync(device);

        Device? result = await repository.GetBySerialNumberAsync(
            device.SerialNumber);

        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
    }

    [Fact]
    public async Task GetBySerialNumberAsyncShouldReturnNullWhenNotFound()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device? result = await repository.GetBySerialNumberAsync(
            "UNKNOWN-001");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online)]
    [InlineData(DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Decommissioned)]
    public async Task GetByIdAsyncShouldRestoreAllStatuses(
        DeviceStatus status)
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        var device = Device.Reconstitute(
            DeviceId.New(),
            "Sensor",
            $"SENSOR-{(int)status:000}",
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(device);

        Device? result = await repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public async Task UpdateAsyncShouldPersistStatusChange()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device device = CreateDevice();

        await repository.AddAsync(device);

        device.ChangeStatus(DeviceStatus.Online);

        await repository.UpdateAsync(device);

        Device? result = await repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.Equal(DeviceStatus.Online, result.Status);
    }

    [Fact]
    public async Task UpdateAsyncShouldPersistUpdatedAtUtc()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device device = CreateDevice();

        await repository.AddAsync(device);

        DateTimeOffset originalUpdatedAt = device.UpdatedAtUtc;

        device.ChangeStatus(DeviceStatus.Online);

        await repository.UpdateAsync(device);

        Device? result = await repository.GetByIdAsync(device.Id);

        Assert.NotNull(result);
        Assert.True(result.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowWhenDeviceDoesNotExist()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        Device device = CreateDevice();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(device));
    }

    [Fact]
    public async Task AddAsyncShouldRespectCancellationToken()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(
                CreateDevice(),
                cancellation.Token));
    }

    [Fact]
    public async Task GetByIdAsyncShouldRespectCancellationToken()
    {
        await using TestDatabase database = await CreateDatabaseAsync();
        DeviceRepository repository = CreateRepository(database);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                DeviceId.New(),
                cancellation.Token));
    }

    private static DeviceRepository CreateRepository(TestDatabase database)
    {
        return new DeviceRepository(
            new TestDatabaseConnectionFactory(database.ConnectionString));
    }

    private static Device CreateDevice(string? serialNumber = null)
    {
        return new Device(
            DeviceId.New(),
            "Industrial Sensor",
            serialNumber ?? $"SENSOR-{Guid.NewGuid():N}");
    }

    private async Task<TestDatabase> CreateDatabaseAsync()
    {
        var schemaName = $"test_{Guid.NewGuid():N}";

        await using NpgsqlConnection adminConnection =
            new(fixture.ConnectionString);

        await adminConnection.OpenAsync();

        await adminConnection.ExecuteAsync(
            $"CREATE SCHEMA \"{schemaName}\";");

        NpgsqlConnectionStringBuilder builder =
            new(fixture.ConnectionString)
            {
                SearchPath = schemaName
            };

        TestDatabase database = new(
            builder.ConnectionString,
            schemaName,
            fixture.ConnectionString);

        await using NpgsqlConnection connection =
            new(database.ConnectionString);

        await connection.OpenAsync();

        var migration = await File.ReadAllTextAsync(
            GetMigrationPath());

        await connection.ExecuteAsync(migration);

        return database;
    }

    private static string GetMigrationPath()
    {
        var currentDirectory = AppContext.BaseDirectory;

        DirectoryInfo? directory = new(currentDirectory);

        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "src",
                "IndustrialBackendDemo.Infrastructure",
                "Persistence",
                "Migrations",
                "001_initial_schema.sql");

            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Initial migration SQL file was not found.");
    }

    private sealed class TestDatabase(
        string connectionString,
        string schemaName,
        string adminConnectionString) : IAsyncDisposable
    {
        public string ConnectionString { get; } = connectionString;

        public async ValueTask DisposeAsync()
        {
            await using NpgsqlConnection connection =
                new(adminConnectionString);

            await connection.OpenAsync();

            await connection.ExecuteAsync(
                $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE;");
        }
    }

    private sealed class TestDatabaseConnectionFactory(
        string connectionString) : IDatabaseConnectionFactory
    {
        public async ValueTask<NpgsqlConnection> CreateConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}
