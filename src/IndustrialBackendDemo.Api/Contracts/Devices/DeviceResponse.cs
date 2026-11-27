using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Api.Contracts.Devices;

public sealed record DeviceResponse(
    Guid Id,
    string Name,
    string SerialNumber,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
)
{
    public static DeviceResponse FromDomain(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return new DeviceResponse(
            device.Id.Value,
            device.Name,
            device.SerialNumber,
            device.Status.ToString(),
            device.CreatedAtUtc,
            device.UpdatedAtUtc
        );
    }
}
