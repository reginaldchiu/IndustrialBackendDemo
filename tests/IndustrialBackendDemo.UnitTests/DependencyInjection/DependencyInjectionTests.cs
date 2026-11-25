using IndustrialBackendDemo.Application;
using IndustrialBackendDemo.Application.Devices.GetDevice;
using IndustrialBackendDemo.Application.Devices.RegisterDevice;
using IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialBackendDemo.UnitTests.DependencyInjection;

public sealed class DependencyInjectionTests
{

    [Theory]
    [InlineData(typeof(RegisterDeviceHandler))]
    [InlineData(typeof(GetDeviceHandler))]
    [InlineData(typeof(UpdateDeviceStatusHandler))]
    public void AddApplicationShouldRegisterHandlerAsScoped(Type handlerType)
    {
        ServiceCollection services = new();

        services.AddApplication();

        ServiceDescriptor? descriptor = services.SingleOrDefault(
            service => service.ServiceType == handlerType
        );

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(handlerType, descriptor.ImplementationType);
    }

    [Fact]
    public void AddApplicationShouldReturnSameServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddApplication();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddApplicationShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddApplication();

        Assert.Throws<ArgumentNullException>(act);
    }
}
