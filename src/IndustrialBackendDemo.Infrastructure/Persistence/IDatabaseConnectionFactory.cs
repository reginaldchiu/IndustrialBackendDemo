using Npgsql;

namespace IndustrialBackendDemo.Infrastructure.Persistence;

public interface IDatabaseConnectionFactory
{
    ValueTask<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
