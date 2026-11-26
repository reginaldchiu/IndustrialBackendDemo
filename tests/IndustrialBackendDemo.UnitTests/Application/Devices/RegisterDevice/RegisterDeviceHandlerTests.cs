
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Application.Devices.RegisterDevice;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Application.Devices.RegisterDevice;

public sealed class RegisterDeviceHandlerTests
{
    [Fact]
    public async Task HandleAsyncShouldReturnDuplicateErrorWhenInsertConflicts()
    {
        IDeviceRepository repository = new ConflictingDeviceRepository();
        RegisterDeviceHandler handler = new(repository);

        RegisterDeviceCommand command = new(
            "Sensor",
            "SENSOR-001");

        Result<Device> result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "Device.DuplicateSerialNumber",
            result.Error.Code);
    }

    private sealed class ConflictingDeviceRepository : IDeviceRepository
    {
        public Task AddAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            throw new DeviceSerialNumberAlreadyExistsException(
                device.SerialNumber);
        }

        public Task<Device?> GetByIdAsync(
            DeviceId id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Device?>(null);
        }

        public Task<Device?> GetBySerialNumberAsync(
            string serialNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Device?>(null);
        }

        public Task UpdateAsync(
            Device device,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
