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
        ValidateId(id);
        ValidateName(name);
        ValidateSerialNumber(serialNumber);

        Id = id;
        Name = name;
        SerialNumber = serialNumber;

        Status = DeviceStatus.Offline;

        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private Device(DeviceId id, string name, string serialNumber, DeviceStatus status, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        ValidateId(id);
        ValidateName(name);
        ValidateSerialNumber(serialNumber);
        ValidateStatus(status);

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("CreatedAtUtc must use UTC.", nameof(createdAtUtc));
        }

        if (updatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("UpdatedAtUtc must use UTC.", nameof(updatedAtUtc));
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new ArgumentException("UpdatedAtUtc cannot be earlier than CreatedAtUtc.", nameof(updatedAtUtc));
        }

        Id = id;
        Name = name;
        SerialNumber = serialNumber;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Device Reconstitute(DeviceId id, string name, string serialNumber, DeviceStatus status, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        return new Device(id, name, serialNumber, status, createdAtUtc, updatedAtUtc);
    }

    public void ChangeStatus(DeviceStatus newStatus)
    {
        ValidateStatus(newStatus);


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

    private static void ValidateId(DeviceId id)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(id));
        }
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 100)
        {
            throw new ArgumentException("Device name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateSerialNumber(string serialNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        if (serialNumber.Length > 100)
        {
            throw new ArgumentException("Serial number cannot exceed 100 characters.", nameof(serialNumber));
        }
    }

    private static void ValidateStatus(DeviceStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Invalid device status.");
        }
    }
}
