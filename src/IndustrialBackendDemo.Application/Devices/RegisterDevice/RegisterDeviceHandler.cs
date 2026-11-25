using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Application.Devices.RegisterDevice;

public sealed class RegisterDeviceHandler(IDeviceRepository deviceRepository)
{
    private static readonly ApplicationError DuplicateSerialNumberError = new(
        "Device.DuplicateSerialNumber",
        "A device with the specified serial number already exists."
    );

    public async Task<Result<Device>> HandleAsync(
        RegisterDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        Device? existingDevice = await deviceRepository.GetBySerialNumberAsync(
            command.SerialNumber,
            cancellationToken
        );

        if (existingDevice is not null)
        {
            return Result<Device>.Failure(DuplicateSerialNumberError);
        }

        Device device = new(DeviceId.New(), command.Name, command.SerialNumber);

        await deviceRepository.AddAsync(device, cancellationToken);

        return Result<Device>.Success(device);
    }
}
