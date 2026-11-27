
using Dapper;
using IndustrialBackendDemo.Domain.AnalysisJobs;
using IndustrialBackendDemo.Domain.Devices;
using IndustrialBackendDemo.Infrastructure.Persistence;
using IndustrialBackendDemo.Infrastructure.Persistence.Repositories;
using IndustrialBackendDemo.IntegrationTests.Fixtures;
using Npgsql;

namespace IndustrialBackendDemo.IntegrationTests.Persistence.Repositories;

public sealed class AnalysisJobRepositoryTests(
    PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static DateTimeOffset NormalizeToPostgreSqlPrecision(DateTimeOffset value)
    {
        var ticks = value.Ticks - value.Ticks % 10;

        return new DateTimeOffset(ticks, value.Offset);
    }

    [Fact]
    public async Task AddAsyncShouldPersistPendingJob()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await repository.AddAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(job.DeviceId, result.DeviceId);
        Assert.Equal(AnalysisJobStatus.Pending, result.Status);
        Assert.Equal(NormalizeToPostgreSqlPrecision(job.CreatedAtUtc), result.CreatedAtUtc);
        Assert.Null(result.StartedAtUtc);
        Assert.Null(result.CompletedAtUtc);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task AddAsyncShouldRejectDuplicateJobId()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        var jobId = AnalysisJobId.New();

        AnalysisJob first = new(jobId, device.Id);
        AnalysisJob second = new(jobId, device.Id);

        await repository.AddAsync(first);

        await Assert.ThrowsAsync<PostgresException>(
            () => repository.AddAsync(second));
    }

    [Fact]
    public async Task AddAsyncShouldRejectUnknownDeviceId()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(DeviceId.New());

        PostgresException exception =
            await Assert.ThrowsAsync<PostgresException>(
                () => repository.AddAsync(job));

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            exception.SqlState);
    }

    [Fact]
    public async Task GetByIdAsyncShouldReturnNullWhenJobDoesNotExist()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob? result = await repository.GetByIdAsync(
            AnalysisJobId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsyncShouldRestoreAllProperties()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        DateTimeOffset createdAt = new(
            2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset startedAt = new(
            2026, 1, 1, 10, 5, 0, TimeSpan.Zero);

        DateTimeOffset completedAt = new(
            2026, 1, 1, 10, 10, 0, TimeSpan.Zero);

        var job = AnalysisJob.Reconstitute(
            AnalysisJobId.New(),
            device.Id,
            AnalysisJobStatus.Failed,
            createdAt,
            startedAt,
            completedAt,
            "Sensor data is invalid.");

        await repository.AddAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(job.DeviceId, result.DeviceId);
        Assert.Equal(AnalysisJobStatus.Failed, result.Status);
        Assert.Equal(NormalizeToPostgreSqlPrecision(createdAt), result.CreatedAtUtc);
        Assert.Equal(NormalizeToPostgreSqlPrecision(startedAt), result.StartedAtUtc);
        Assert.Equal(NormalizeToPostgreSqlPrecision(completedAt), result.CompletedAtUtc);
        Assert.Equal("Sensor data is invalid.", result.FailureReason);
    }

    [Theory]
    [InlineData(AnalysisJobStatus.Pending)]
    [InlineData(AnalysisJobStatus.Running)]
    [InlineData(AnalysisJobStatus.Succeeded)]
    [InlineData(AnalysisJobStatus.Failed)]
    public async Task GetByIdAsyncShouldRestoreAllStatuses(
        AnalysisJobStatus status)
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJobWithStatus(device.Id, status);

        await repository.AddAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public async Task UpdateAsyncShouldPersistRunningStatus()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await repository.AddAsync(job);

        job.Start();

        await repository.UpdateAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(AnalysisJobStatus.Running, result.Status);
        Assert.NotNull(result.StartedAtUtc);
        Assert.Null(result.CompletedAtUtc);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task UpdateAsyncShouldPersistSucceededStatus()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await repository.AddAsync(job);

        job.Start();
        job.Complete();

        await repository.UpdateAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(AnalysisJobStatus.Succeeded, result.Status);
        Assert.NotNull(result.StartedAtUtc);
        Assert.NotNull(result.CompletedAtUtc);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task UpdateAsyncShouldPersistFailedStatus()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await repository.AddAsync(job);

        job.Start();
        job.Fail("Analysis execution failed.");

        await repository.UpdateAsync(job);

        AnalysisJob? result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal(AnalysisJobStatus.Failed, result.Status);
        Assert.NotNull(result.StartedAtUtc);
        Assert.NotNull(result.CompletedAtUtc);
        Assert.Equal("Analysis execution failed.", result.FailureReason);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowWhenJobDoesNotExist()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(job));
    }

    [Fact]
    public async Task AddAsyncShouldRespectCancellationToken()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(
                CreateJob(device.Id),
                cancellation.Token));
    }

    [Fact]
    public async Task GetByIdAsyncShouldRespectCancellationToken()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        AnalysisJobRepository repository = CreateRepository(database);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                AnalysisJobId.New(),
                cancellation.Token));
    }

    [Fact]
    public async Task UpdateAsyncShouldRespectCancellationToken()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Device device = await CreateAndPersistDeviceAsync(database);
        AnalysisJobRepository repository = CreateRepository(database);

        AnalysisJob job = CreateJob(device.Id);

        await repository.AddAsync(job);

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UpdateAsync(
                job,
                cancellation.Token));
    }

    private static AnalysisJobRepository CreateRepository(
        TestDatabase database)
    {
        return new AnalysisJobRepository(
            new TestDatabaseConnectionFactory(database.ConnectionString));
    }

    private static AnalysisJob CreateJob(DeviceId deviceId)
    {
        return new AnalysisJob(
            AnalysisJobId.New(),
            deviceId);
    }

    private static AnalysisJob CreateJobWithStatus(
        DeviceId deviceId,
        AnalysisJobStatus status)
    {
        DateTimeOffset createdAt = new(
            2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset startedAt = new(
            2026, 1, 1, 10, 5, 0, TimeSpan.Zero);

        DateTimeOffset completedAt = new(
            2026, 1, 1, 10, 10, 0, TimeSpan.Zero);

        return status switch
        {
            AnalysisJobStatus.Pending => AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                deviceId,
                status,
                createdAt,
                null,
                null,
                null),

            AnalysisJobStatus.Running => AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                deviceId,
                status,
                createdAt,
                startedAt,
                null,
                null),

            AnalysisJobStatus.Succeeded => AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                deviceId,
                status,
                createdAt,
                startedAt,
                completedAt,
                null),

            AnalysisJobStatus.Failed => AnalysisJob.Reconstitute(
                AnalysisJobId.New(),
                deviceId,
                status,
                createdAt,
                startedAt,
                completedAt,
                "Analysis failed."),

            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }

    private static async Task<Device> CreateAndPersistDeviceAsync(
        TestDatabase database)
    {
        Device device = new(
            DeviceId.New(),
            "Industrial Sensor",
            $"SENSOR-{Guid.NewGuid():N}");

        DeviceRepository repository = new(
            new TestDatabaseConnectionFactory(database.ConnectionString));

        await repository.AddAsync(device);

        return device;
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
