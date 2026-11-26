namespace IndustrialBackendDemo.Infrastructure.Persistence.Repositories;

internal sealed class DeviceRow
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SerialNumber { get; init; } = string.Empty;

    public short Status { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
