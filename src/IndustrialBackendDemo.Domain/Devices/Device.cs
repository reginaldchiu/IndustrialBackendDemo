namespace IndustrialBackendDemo.Domain.Devices;

public sealed class Device
{
    public DeviceId Id { get; }

    public string Name { get; private set; }

    public string SerialNumber { get; private set; }

    public DeviceStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Device(DeviceId id, string name, string serialNumber)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(id));
        }

        ValidateRequiredText(name, nameof(name));
        ValidateRequiredText(serialNumber, nameof(serialNumber));

        if (name.Length > 100)
        {
            throw new ArgumentException("Device name cannot exceed 100 characters.", nameof(name));
        }

        if (serialNumber.Length > 100)
        {
            throw new ArgumentException(
                "Serial number cannot exceed 100 characters.",
                nameof(serialNumber)
            );
        }

        Id = id;
        Name = name;
        SerialNumber = serialNumber;

        Status = DeviceStatus.Offline;

        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void ChangeStatus(DeviceStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(newStatus),
                newStatus,
                "Invalid device status."
            );
        }

        if (Status == newStatus)
        {
            throw new InvalidOperationException("Device is already in the requested status.");
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot change device status from {Status} to {newStatus}"
            );
        }

        Status = newStatus;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private bool CanTransitionTo(DeviceStatus newStatus)
    {
        return Status switch
        {
            DeviceStatus.Offline => newStatus
                is DeviceStatus.Online
                    or DeviceStatus.Maintenance
                    or DeviceStatus.Decommissioned,

            DeviceStatus.Online => newStatus
                is DeviceStatus.Offline
                    or DeviceStatus.Maintenance
                    or DeviceStatus.Decommissioned,

            DeviceStatus.Maintenance => newStatus
                is DeviceStatus.Offline
                    or DeviceStatus.Decommissioned,

            DeviceStatus.Decommissioned => false,

            _ => false,
        };
    }

    private static void ValidateRequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    }
}
