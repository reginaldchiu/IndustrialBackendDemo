using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Application.Devices.GetDevice;

public sealed class GetDeviceHandler(IDeviceRepository deviceRepository)
{
    private static readonly ApplicationError DeviceNotFoundError = new("Device.NotFound", "The specified device was not found.");

    public async Task<Result<Device>> HandleAsync(GetDeviceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Device? device = await deviceRepository.GetByIdAsync(query.Id, cancellationToken);

        if (device is null)
        {
            return Result<Device>.Failure(DeviceNotFoundError);
        }

        return Result<Device>.Success(device);
    }
}
