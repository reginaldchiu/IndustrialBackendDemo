using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices;

public sealed class DeviceTests
{
    private static Device CreateDevice()
    {
        return new Device(DeviceId.New(), "Industrial Sensor", "SN-2026-0001");
    }

    #region Constructor Tests

    [Fact]
    public void ConstructorWithValidParametersShouldCreateDevice()
    {
        var id = DeviceId.New();

        Device device = new(id, "Industrial Sensor", "SN-2026-0001");

        Assert.Equal(id, device.Id);
        Assert.Equal("Industrial Sensor", device.Name);
        Assert.Equal("SN-2026-0001", device.SerialNumber);
    }

    [Fact]
    public void ConstructorWithDefaultDeviceIdShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Device(default, "Industrial Sensor", "SN-2026-0001")
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ConstructorWithInvalidNameShouldTHrowArgumentException(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Device(DeviceId.New(), name!, "SN-2026-0001")
        );
    }

    [Fact]
    public void ConstructorWithNameExceeding100CharactersShouldThrowArgumentException()
    {
        string name = new('A', 101);

        Assert.ThrowsAny<ArgumentException>(() => new Device(DeviceId.New(), name, "SN-2026-0001"));
    }

    [Fact]
    public void ConstructorWithNameContaining100CharactersShouldSucceed()
    {
        string name = new('A', 100);

        Device device = new(DeviceId.New(), name, "SN-2026-0001");

        Assert.Equal(name, device.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void ConstructorWithInvalidSerialNumberShouldThrowArgumentException(string? serialNumber)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Device(DeviceId.New(), "Industrial Sensor", serialNumber!)
        );
    }

    [Fact]
    public void ConstructorWithSerialNumberExceeding100CharactersShouldThrowArgumentException()
    {
        string serialNumber = new('A', 101);

        Assert.ThrowsAny<ArgumentException>(() =>
            new Device(DeviceId.New(), "Industrial Sensor", serialNumber)
        );
    }

    [Fact]
    public void ConstructorWithSerialNumberContaining100CharactersShouldSucceed()
    {
        string serialNumber = new('A', 100);

        Device device = new(DeviceId.New(), "Industrial Sensor", serialNumber);

        Assert.Equal(serialNumber, device.SerialNumber);
    }

    [Fact]
    public void ConstructorShouldSetInitialStatusToOffline()
    {
        Device device = CreateDevice();

        Assert.Equal(DeviceStatus.Offline, device.Status);
    }

    [Fact]
    public void ConstructorShouldInitializeUtcTimestamps()
    {
        DateTimeOffset beforeCreation = DateTimeOffset.UtcNow;

        Device device = CreateDevice();

        DateTimeOffset afterCreation = DateTimeOffset.UtcNow;

        Assert.Equal(TimeSpan.Zero, device.CreatedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, device.UpdatedAtUtc.Offset);

        Assert.True(device.CreatedAtUtc >= beforeCreation);
        Assert.True(device.CreatedAtUtc <= afterCreation);

        Assert.Equal(device.CreatedAtUtc, device.UpdatedAtUtc);
    }

    #endregion

    #region Valid Status Transition Tests

    [Theory]
    [InlineData(DeviceStatus.Offline, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online, DeviceStatus.Online)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Decommissioned, DeviceStatus.Decommissioned)]
    [InlineData(DeviceStatus.Maintenance, DeviceStatus.Online)]
    public void ChangeStatusWithInvalidTransitionShouldThrowInvalidOperationException(
        DeviceStatus initialStatus,
        DeviceStatus newStatus
    )
    {
        Device device = CreateDevice();

        if (initialStatus != DeviceStatus.Offline)
        {
            device.ChangeStatus(initialStatus);
        }

        Assert.Throws<InvalidOperationException>(() => device.ChangeStatus(newStatus));
    }

    [Theory]
    [InlineData(DeviceStatus.Decommissioned, DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Decommissioned, DeviceStatus.Online)]
    [InlineData(DeviceStatus.Decommissioned, DeviceStatus.Maintenance)]
    public void ChangeStatusFromDecommissionedShouldThrowInvalidOperationException(
        DeviceStatus initialStatus,
        DeviceStatus newStatus
    )
    {
        Device device = CreateDevice();
        device.ChangeStatus(initialStatus);

        Assert.Throws<InvalidOperationException>(() => device.ChangeStatus(newStatus));
    }

    [Fact]
    public void ChangeStatusWithUndefinedStatusShouldThrowArgumentOutOfRangeException()
    {
        Device device = CreateDevice();
        var invalidStatus = (DeviceStatus)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => device.ChangeStatus(invalidStatus));
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void ChangeStatusShouldUpdateUpdatedAtUtc()
    {
        Device device = CreateDevice();

        DateTimeOffset previousUpdatedAt = device.UpdatedAtUtc;

        device.ChangeStatus(DeviceStatus.Online);

        Assert.True(device.UpdatedAtUtc >= previousUpdatedAt);
        Assert.Equal(TimeSpan.Zero, device.UpdatedAtUtc.Offset);
    }

    [Fact]
    public void ChanageStatusShouldNotChangeCreatedAtUtc()
    {
        Device device = CreateDevice();
        DateTimeOffset createdAt = device.CreatedAtUtc;

        device.ChangeStatus(DeviceStatus.Online);

        Assert.Equal(createdAt, device.CreatedAtUtc);
    }

    [Fact]
    public void ChangeStatusWithInvalidTransitionShouldNotChangeStatus()
    {
        Device device = CreateDevice();
        device.ChangeStatus(DeviceStatus.Maintenance);

        Assert.Throws<InvalidOperationException>(() => device.ChangeStatus(DeviceStatus.Online));

        Assert.Equal(DeviceStatus.Maintenance, device.Status);
    }

    [Fact]
    public void ChangeStatusWithInvalidTransitionShouldNotUpdateTimestamp()
    {
        Device device = CreateDevice();
        device.ChangeStatus(DeviceStatus.Maintenance);

        DateTimeOffset previousUpdatedAt = device.UpdatedAtUtc;

        Assert.Throws<InvalidOperationException>(() => device.ChangeStatus(DeviceStatus.Online));

        Assert.Equal(previousUpdatedAt, device.UpdatedAtUtc);
    }

    #endregion
}
