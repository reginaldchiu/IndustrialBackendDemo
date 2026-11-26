using Npgsql;

namespace IndustrialBackendDemo.Infrastructure.Persistence;

public sealed class DatabaseConnectionFactory(NpgsqlDataSource dataSource) : IDatabaseConnectionFactory
{
    public ValueTask<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return dataSource.OpenConnectionAsync(cancellationToken);
    }
}
