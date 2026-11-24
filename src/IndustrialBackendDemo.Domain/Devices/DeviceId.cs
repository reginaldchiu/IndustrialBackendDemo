namespace IndustrialBackendDemo.Domain.Devices;

public readonly record struct DeviceId
{
    public Guid Value { get; }

    public DeviceId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Device ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static DeviceId New()
    {
        return new DeviceId(Guid.NewGuid());
    }
}
