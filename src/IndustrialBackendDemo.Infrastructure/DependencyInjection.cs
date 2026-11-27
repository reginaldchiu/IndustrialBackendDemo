using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Infrastructure.Persistence;
using IndustrialBackendDemo.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IndustrialBackendDemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database connection string cannot be empty."
            )
            .ValidateOnStart();

        services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
        {
            DatabaseOptions options = serviceProvider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Value;

            return NpgsqlDataSource.Create(options.ConnectionString);
        });

        services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();

        return services;
    }
}
