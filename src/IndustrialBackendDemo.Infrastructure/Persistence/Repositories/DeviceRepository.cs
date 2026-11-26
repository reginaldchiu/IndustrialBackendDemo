using Dapper;
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Domain.Devices;
using Npgsql;

namespace IndustrialBackendDemo.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(IDatabaseConnectionFactory factory) : IDeviceRepository
{
    private const string _uniqueSerialNumberConstraint = "ux_devices_serial_number";

    private const string _insertSql = """
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
            @Id,
            @Name,
            @SerialNumber,
            @Status,
            @CreatedAtUtc,
            @UpdatedAtUtc
        );
        """;

    private const string _selectColumns = """
        id AS Id,
        name AS Name,
        serial_number AS SerialNumber,
        status AS Status,
        created_at_utc AS CreatedAtUtc,
        updated_at_utc AS UpdatedAtUtc
        """;

    private const string _updateSql = """
        UPDATE devices
        SET
            name = @Name,
            serial_number = @SerialNumber,
            status = @Status,
            updated_at_utc = @UpdatedAtUtc
        WHERE id = @Id;
        """;

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(_insertSql,
                new
                {
                    Id = device.Id.Value,
                    device.Name,
                    device.SerialNumber,
                    Status = (short)device.Status,
                    device.CreatedAtUtc,
                    device.UpdatedAtUtc
                }, cancellationToken: cancellationToken)
            );
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == _uniqueSerialNumberConstraint)
        {
            throw new DeviceSerialNumberAlreadyExistsException(device.SerialNumber, ex);
        }
    }

    public async Task<Device?> GetByIdAsync(DeviceId id, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        var sql = $"""
            SELECT {_selectColumns}
            FROM devices
            WHERE id = @Id;
            """;

        DeviceRow? row = await connection.QuerySingleOrDefaultAsync<DeviceRow>(
            new CommandDefinition(
                sql,
                new { Id = id.Value },
                cancellationToken: cancellationToken
            )
        );

        return row is null ? null : MapToDomain(row);
    }

    public async Task<Device?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        var sql = $"""
            SELECT {_selectColumns}
            FROM devices
            WHERE serial_number = @SerialNumber;
            """;

        DeviceRow? row = await connection.QuerySingleOrDefaultAsync<DeviceRow>(
            new CommandDefinition(
                sql,
                new { SerialNumber = serialNumber },
                cancellationToken: cancellationToken
            )
        );

        return row is null ? null : MapToDomain(row);
    }

    public async Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        await using NpgsqlConnection connection =
            await factory.CreateConnectionAsync(cancellationToken);

        try
        {
            var affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    _updateSql,
                    new
                    {
                        Id = device.Id.Value,
                        device.Name,
                        device.SerialNumber,
                        Status = (short)device.Status,
                        device.UpdatedAtUtc
                    },
                    cancellationToken: cancellationToken
                )
            );

            if (affectedRows == 0)
            {
                throw new InvalidOperationException($"Device '{device.Id.Value}' does not exists.");
            }
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == _uniqueSerialNumberConstraint)
        {
            throw new DeviceSerialNumberAlreadyExistsException(device.SerialNumber, ex);
        }
    }

    private static Device MapToDomain(DeviceRow row)
    {
        return Device.Reconstitute(
            new DeviceId(row.Id),
            row.Name,
            row.SerialNumber,
            (DeviceStatus)row.Status,
            row.CreatedAtUtc,
            row.UpdatedAtUtc
        );
    }
}
