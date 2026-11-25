using IndustrialBackendDemo.Application.Devices.GetDevice;
using IndustrialBackendDemo.Application.Devices.RegisterDevice;
using IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialBackendDemo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<RegisterDeviceHandler>();
        services.AddScoped<GetDeviceHandler>();
        services.AddScoped<UpdateDeviceStatusHandler>();

        return services;
    }
}
