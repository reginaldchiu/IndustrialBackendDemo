namespace IndustrialBackendDemo.Api.Contracts.Devices;

public sealed record RegisterDeviceRequest(
    string? Name,
    string? SerialNumber
);
