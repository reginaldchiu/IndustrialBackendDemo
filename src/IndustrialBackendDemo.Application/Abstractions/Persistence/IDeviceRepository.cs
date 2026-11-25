using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Application.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    Task<Device?> GetByIdAsync(DeviceId id, CancellationToken cancellationToken = default);

    Task<Device?> GetBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default
    );

    Task UpdateAsync(Device device, CancellationToken cancellationToken = default);
}
