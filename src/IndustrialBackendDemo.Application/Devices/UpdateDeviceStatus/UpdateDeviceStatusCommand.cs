using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;

public sealed record UpdateDeviceStatusCommand(DeviceId Id, DeviceStatus Status);
