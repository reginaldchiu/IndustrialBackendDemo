using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices;

public sealed class DeviceIdTests
{
    [Fact]
    public void ConstructorWithValidGuidShouldCreateDeviceId()
    {
        var value = Guid.NewGuid();

        DeviceId deviceId = new(value);

        Assert.Equal(value, deviceId.Value);
    }

    [Fact]
    public void ConstructorWithEmptyGuidShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new DeviceId(Guid.Empty));
    }

    [Fact]
    public void NewShouldGenerateNoeEmptyGuid()
    {
        var deviceId = DeviceId.New();

        Assert.NotEqual(Guid.Empty, deviceId.Value);
    }

    [Fact]
    public void EqualsWithSameGuidShouldReturnTrue()
    {
        var value = Guid.NewGuid();

        DeviceId first = new(value);
        DeviceId second = new(value);

        Assert.Equal(first, second);
    }

    [Fact]
    public void EqualsWithDifferentGuidShouldReturnFalse()
    {
        var first = DeviceId.New();
        var second = DeviceId.New();

        Assert.NotEqual(first, second);
    }
}
