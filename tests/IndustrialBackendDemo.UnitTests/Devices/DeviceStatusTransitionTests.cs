using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices;

public sealed class DeviceStatusTransitionTests
{
    private static Device CreateDevice()
    {
        return new Device(DeviceId.New(), "Industrial Sensor", "SN-2026-0001");
    }

    [Theory]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Online)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Decommissioned)]
    public void ChangeStatusWithValidTransitionShouldUpdateStatus(
        DeviceStatus initialStatus,
        DeviceStatus newStatus
    )
    {
        // Arrange
        Device device = CreateDevice();

        if (initialStatus != DeviceStatus.Offline)
        {
            device.ChangeStatus(initialStatus);
        }

        // Act
        device.ChangeStatus(newStatus);

        // Assert
        Assert.Equal(newStatus, device.Status);
    }

    [Theory]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Online)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Decommissioned)]
    public void ChangeStatusWithValidTransitionShouldUpdateUpdatedAtUtc(
        DeviceStatus initialStatus,
        DeviceStatus newStatus
    )
    {
        // Arrange
        Device device = CreateDevice();

        if (initialStatus != DeviceStatus.Offline)
        {
            device.ChangeStatus(initialStatus);
        }

        DateTimeOffset previousUpdatedAt = device.UpdatedAtUtc;
        DateTimeOffset beforeChange = DateTimeOffset.UtcNow;

        // Act
        device.ChangeStatus(newStatus);

        DateTimeOffset afterChange = DateTimeOffset.UtcNow;

        // Assert
        Assert.InRange(device.UpdatedAtUtc, beforeChange, afterChange);

        Assert.True(device.UpdatedAtUtc >= previousUpdatedAt);

        Assert.Equal(TimeSpan.Zero, device.UpdatedAtUtc.Offset);
    }

    [Theory]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Online)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Decommissioned)]
    public void ChangeStatusWithValidTransitionShouldNotChangeCreatedAtUtc(
        DeviceStatus initialStatus,
        DeviceStatus newStatus
    )
    {
        // Arrange
        Device device = CreateDevice();

        DateTimeOffset createdAt = device.CreatedAtUtc;

        if (initialStatus != DeviceStatus.Offline)
        {
            device.ChangeStatus(initialStatus);
        }

        // Act
        device.ChangeStatus(newStatus);

        // Assert
        Assert.Equal(createdAt, device.CreatedAtUtc);
    }
}
