using System.Reflection;
using IndustrialBackendDemo.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IndustrialBackendDemo.IntegrationTests.Persistence.Migrations;

public sealed class InitialMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("industrial_backend_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task UpMigrationShouldCreateTables()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        var devicesExists = await TableExistsAsync(
            database.Connection,
            "devices");

        var analysisJobsExists = await TableExistsAsync(
            database.Connection,
            "analysis_jobs");

        Assert.True(devicesExists);
        Assert.True(analysisJobsExists);
    }

    [Fact]
    public async Task UpMigrationShouldAllowInsertingValidDevice()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        await InsertDeviceAsync(database.Connection);
    }

    [Fact]
    public async Task DeviceShouldRejectDuplicateSerialNumber()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        const string serialNumber = "DEVICE-001";

        await InsertDeviceAsync(
            database.Connection,
            serialNumber: serialNumber);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.UniqueViolation,
            () => InsertDeviceAsync(
                database.Connection,
                serialNumber: serialNumber));
    }

    [Fact]
    public async Task DeviceShouldRejectEmptyName()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertDeviceAsync(
                database.Connection,
                name: string.Empty));
    }

    [Fact]
    public async Task DeviceShouldRejectEmptySerialNumber()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertDeviceAsync(
                database.Connection,
                serialNumber: string.Empty));
    }

    [Fact]
    public async Task DeviceShouldRejectInvalidStatus()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertDeviceAsync(
                database.Connection,
                status: 99));
    }

    [Fact]
    public async Task AnalysisJobShouldAllowValidPendingJob()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await InsertAnalysisJobAsync(
            database.Connection,
            deviceId);
    }

    [Fact]
    public async Task AnalysisJobShouldRejectNonExistingDevice()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.ForeignKeyViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                Guid.NewGuid()));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectInvalidStatus()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 99));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectPendingJobWithStartedAt()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                startedAtUtc: UtcNow()));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectRunningJobWithoutStartedAt()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 1));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectRunningJobWithCompletedAt()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        DateTime startedAt = UtcNow();
        DateTime completedAt = startedAt.AddMinutes(5);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 1,
                startedAtUtc: startedAt,
                completedAtUtc: completedAt));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectSucceededJobWithoutCompletedAt()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 2,
                startedAtUtc: UtcNow()));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectFailedJobWithoutFailureReason()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        DateTime startedAt = UtcNow();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 3,
                startedAtUtc: startedAt,
                completedAtUtc: startedAt.AddMinutes(5)));
    }

    [Fact]
    public async Task AnalysisJobShouldRejectFailedJobWithWhitespaceFailureReason()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        DateTime startedAt = UtcNow();

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.CheckViolation,
            () => InsertAnalysisJobAsync(
                database.Connection,
                deviceId,
                status: 3,
                startedAtUtc: startedAt,
                completedAtUtc: startedAt.AddMinutes(5),
                failureReason: "   "));
    }

    [Fact]
    public async Task DeviceShouldNotBeDeletedWhenReferencedByAnalysisJob()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        await InsertAnalysisJobAsync(
            database.Connection,
            deviceId);

        await AssertPostgresErrorAsync(
            PostgresErrorCodes.ForeignKeyViolation,
            () => DeleteDeviceAsync(
                database.Connection,
                deviceId));
    }

    [Fact]
    public async Task AnalysisJobShouldAllowValidFailedJob()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        DateTime startedAt = UtcNow();

        await InsertAnalysisJobAsync(
            database.Connection,
            deviceId,
            status: 3,
            startedAtUtc: startedAt,
            completedAtUtc: startedAt.AddMinutes(5),
            failureReason: "Analysis failed.");
    }

    [Fact]
    public async Task AnalysisJobShouldAllowValidSucceededJob()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        Guid deviceId = await InsertDeviceAsync(database.Connection);

        DateTime startedAt = UtcNow();

        await InsertAnalysisJobAsync(
            database.Connection,
            deviceId,
            status: 2,
            startedAtUtc: startedAt,
            completedAtUtc: startedAt.AddMinutes(5));
    }

    [Fact]
    public async Task DownMigrationShouldDropTables()
    {
        await using TestDatabase database = await CreateDatabaseAsync();

        var downMigration = await ReadMigrationAsync(
            "IndustrialBackendDemo.Infrastructure.Persistence.Migrations.001_initial_schema.down.sql");

        await ExecuteSqlAsync(
            database.Connection,
            downMigration);

        var devicesExists = await TableExistsAsync(
            database.Connection,
            "devices");

        var analysisJobsExists = await TableExistsAsync(
            database.Connection,
            "analysis_jobs");

        Assert.False(devicesExists);
        Assert.False(analysisJobsExists);
    }

    private async Task<TestDatabase> CreateDatabaseAsync()
    {
        var schemaName = $"test_{Guid.NewGuid():N}";

        await using (NpgsqlConnection adminConnection = new(
            _postgres.GetConnectionString()))
        {
            await adminConnection.OpenAsync();

            await ExecuteSqlAsync(
                adminConnection,
                $"CREATE SCHEMA \"{schemaName}\";");
        }

        NpgsqlConnectionStringBuilder connectionStringBuilder = new(
            _postgres.GetConnectionString())
        {
            SearchPath = schemaName
        };

        NpgsqlConnection connection = new(
            connectionStringBuilder.ConnectionString);

        try
        {
            await connection.OpenAsync();

            var upMigration = await ReadMigrationAsync(
                "IndustrialBackendDemo.Infrastructure.Persistence.Migrations.001_initial_schema.sql");

            await ExecuteSqlAsync(connection, upMigration);

            return new TestDatabase(
                connection,
                _postgres.GetConnectionString(),
                schemaName);
        }
        catch
        {
            await connection.DisposeAsync();

            await DropSchemaAsync(
                _postgres.GetConnectionString(),
                schemaName);

            throw;
        }
    }

    private static async Task<string> ReadMigrationAsync(
        string resourceName)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        await using Stream stream = assembly.GetManifestResourceStream(
            resourceName)
            ?? throw new InvalidOperationException(
                $"Migration resource '{resourceName}' was not found.");

        using StreamReader reader = new(stream);

        return await reader.ReadToEndAsync();
    }

    private static async Task ExecuteSqlAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string tableName)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name = @tableName
            );
            """;

        await using NpgsqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("tableName", tableName);

        var result = await command.ExecuteScalarAsync();

        return result is true;
    }

    private static async Task<Guid> InsertDeviceAsync(
        NpgsqlConnection connection,
        string name = "Test Device",
        string serialNumber = "DEVICE-001",
        int status = 0)
    {
        var id = Guid.NewGuid();

        const string sql = """
            INSERT INTO devices
            (
                id,
                name,
                serial_number,
                status,
                created_at_utc,
                updated_at_utc
            )
            VALUES
            (
                @id,
                @name,
                @serialNumber,
                @status,
                @createdAtUtc,
                @updatedAtUtc
            );
            """;

        await using NpgsqlCommand command = new(sql, connection);

        DateTime now = UtcNow();

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("serialNumber", serialNumber);
        command.Parameters.AddWithValue("status", (short)status);
        command.Parameters.AddWithValue("createdAtUtc", now);
        command.Parameters.AddWithValue("updatedAtUtc", now);

        await command.ExecuteNonQueryAsync();

        return id;
    }

    private static async Task<Guid> InsertAnalysisJobAsync(
        NpgsqlConnection connection,
        Guid deviceId,
        int status = 0,
        DateTime? startedAtUtc = null,
        DateTime? completedAtUtc = null,
        string? failureReason = null)
    {
        var id = Guid.NewGuid();

        const string sql = """
            INSERT INTO analysis_jobs
            (
                id,
                device_id,
                status,
                created_at_utc,
                started_at_utc,
                completed_at_utc,
                failure_reason
            )
            VALUES
            (
                @id,
                @deviceId,
                @status,
                @createdAtUtc,
                @startedAtUtc,
                @completedAtUtc,
                @failureReason
            );
            """;

        await using NpgsqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("deviceId", deviceId);
        command.Parameters.AddWithValue("status", (short)status);
        command.Parameters.AddWithValue("createdAtUtc", UtcNow());

        command.Parameters.AddWithValue(
            "startedAtUtc",
            (object?)startedAtUtc ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "completedAtUtc",
            (object?)completedAtUtc ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "failureReason",
            (object?)failureReason ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        return id;
    }

    private static async Task DeleteDeviceAsync(
        NpgsqlConnection connection,
        Guid deviceId)
    {
        const string sql = """
            DELETE FROM devices
            WHERE id = @id;
            """;

        await using NpgsqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("id", deviceId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertPostgresErrorAsync(
        string expectedSqlState,
        Func<Task> action)
    {
        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            action);

        Assert.Equal(expectedSqlState, exception.SqlState);
    }

    private static DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    private static async Task DropSchemaAsync(
        string connectionString,
        string schemaName)
    {
        await using NpgsqlConnection connection = new(connectionString);

        await connection.OpenAsync();

        await ExecuteSqlAsync(
            connection,
            $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE;");
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _schemaName;

        public NpgsqlConnection Connection { get; }

        public TestDatabase(
            NpgsqlConnection connection,
            string adminConnectionString,
            string schemaName)
        {
            Connection = connection;
            _adminConnectionString = adminConnectionString;
            _schemaName = schemaName;
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();

            await DropSchemaAsync(
                _adminConnectionString,
                _schemaName);
        }
    }
}
