namespace IndustrialBackendDemo.Application.Abstractions.Persistence;

public sealed class DeviceSerialNumberAlreadyExistsException(string serialNumber, Exception? innerException = null)
: Exception($"A device with serial number '{serialNumber}' already exists.", innerException);
