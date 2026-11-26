
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Domain.Devices;

public sealed class DeviceReconstitutionTests
{
    private static readonly DateTimeOffset _createdAtUtc =
        new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset _updatedAtUtc =
        new(2026, 1, 2, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DeviceStatus.Offline)]
    [InlineData(DeviceStatus.Online)]
    [InlineData(DeviceStatus.Maintenance)]
    [InlineData(DeviceStatus.Decommissioned)]
    public void ReconstituteShouldRestoreDeviceStatus(DeviceStatus status)
    {
        Device device = CreateDevice(status);

        Assert.Equal(status, device.Status);
    }

    [Fact]
    public void ReconstituteShouldRestoreAllProperties()
    {
        var id = DeviceId.New();

        var device = Device.Reconstitute(
            id,
            "Industrial Sensor",
            "SENSOR-001",
            DeviceStatus.Online,
            _createdAtUtc,
            _updatedAtUtc);

        Assert.Equal(id, device.Id);
        Assert.Equal("Industrial Sensor", device.Name);
        Assert.Equal("SENSOR-001", device.SerialNumber);
        Assert.Equal(DeviceStatus.Online, device.Status);
        Assert.Equal(_createdAtUtc, device.CreatedAtUtc);
        Assert.Equal(_updatedAtUtc, device.UpdatedAtUtc);
    }

    [Fact]
    public void ReconstituteShouldRejectEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                default,
                "Sensor",
                "SENSOR-001",
                DeviceStatus.Offline,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ReconstituteShouldRejectInvalidName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                name!,
                "SENSOR-001",
                DeviceStatus.Offline,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ReconstituteShouldRejectInvalidSerialNumber(
        string? serialNumber)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                serialNumber!,
                DeviceStatus.Offline,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Fact]
    public void ReconstituteShouldRejectNameLongerThan100Characters()
    {
        string name = new('A', 101);

        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                name,
                "SENSOR-001",
                DeviceStatus.Offline,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Fact]
    public void ReconstituteShouldRejectSerialNumberLongerThan100Characters()
    {
        string serialNumber = new('A', 101);

        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                serialNumber,
                DeviceStatus.Offline,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Fact]
    public void ReconstituteShouldRejectUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                "SENSOR-001",
                (DeviceStatus)999,
                _createdAtUtc,
                _updatedAtUtc));
    }

    [Fact]
    public void ReconstituteShouldRejectNonUtcCreatedAt()
    {
        DateTimeOffset localTime =
            new(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                "SENSOR-001",
                DeviceStatus.Offline,
                localTime,
                _updatedAtUtc));
    }

    [Fact]
    public void ReconstituteShouldRejectNonUtcUpdatedAt()
    {
        DateTimeOffset localTime =
            new(2026, 1, 2, 10, 0, 0, TimeSpan.FromHours(8));

        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                "SENSOR-001",
                DeviceStatus.Offline,
                _createdAtUtc,
                localTime));
    }

    [Fact]
    public void ReconstituteShouldRejectUpdatedAtEarlierThanCreatedAt()
    {
        Assert.Throws<ArgumentException>(() =>
            Device.Reconstitute(
                DeviceId.New(),
                "Sensor",
                "SENSOR-001",
                DeviceStatus.Offline,
                _updatedAtUtc,
                _createdAtUtc));
    }

    private static Device CreateDevice(DeviceStatus status)
    {
        return Device.Reconstitute(
            DeviceId.New(),
            "Sensor",
            "SENSOR-001",
            status,
            _createdAtUtc,
            _updatedAtUtc);
    }
}
