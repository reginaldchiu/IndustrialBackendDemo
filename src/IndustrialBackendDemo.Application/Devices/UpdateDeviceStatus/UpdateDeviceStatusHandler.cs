using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;

public sealed class UpdateDeviceStatusHandler(IDeviceRepository deviceRepository)
{
    private static readonly ApplicationError DeviceNotFoundError = new("Device.NotFound", "The specified device was not found.");

    private static readonly ApplicationError InvalidStatusError = new("Device.InvalidStatus", "The specified device status is invalid.");

    private static readonly ApplicationError InvalidStatusTransitionError = new("Device.InvalidStatusTransition", "The requested device status transition is not allowed.");

    public async Task<Result<Device>> HandleAsync(UpdateDeviceStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Device? device = await deviceRepository.GetByIdAsync(command.Id, cancellationToken);

        if (device is null)
        {
            return Result<Device>.Failure(DeviceNotFoundError);
        }

        try
        {
            device.ChangeStatus(command.Status);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<Device>.Failure(InvalidStatusError);
        }
        catch (InvalidOperationException)
        {
            return Result<Device>.Failure(InvalidStatusTransitionError);
        }

        await deviceRepository.UpdateAsync(device, cancellationToken);

        return Result<Device>.Success(device);
    }
}
