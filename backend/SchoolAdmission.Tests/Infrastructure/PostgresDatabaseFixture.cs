using Npgsql;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgresDatabase";
}

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private string? _externalDatabaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var externalUrl = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(externalUrl))
        {
            var baseBuilder = BuildConnectionString(externalUrl);
            _adminConnectionString = baseBuilder.ConnectionString;
            _externalDatabaseName = $"school_admission_test_{Guid.NewGuid():N}"[..32];

            await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
            await adminConnection.OpenAsync();
            await using var createCommand = new NpgsqlCommand(
                $"CREATE DATABASE \"{_externalDatabaseName}\"",
                adminConnection);
            await createCommand.ExecuteNonQueryAsync();

            baseBuilder.Database = _externalDatabaseName;
            ConnectionString = baseBuilder.ConnectionString;
            return;
        }

        _container = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        if (_adminConnectionString is not null && _externalDatabaseName is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var adminConnection = new NpgsqlConnection(_adminConnectionString);
            await adminConnection.OpenAsync();
            await using var dropCommand = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{_externalDatabaseName}\" WITH (FORCE)",
                adminConnection);
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    private static NpgsqlConnectionStringBuilder BuildConnectionString(string value)
    {
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            var userInfo = uri.UserInfo.Split(':', 2);
            return new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = string.IsNullOrWhiteSpace(uri.AbsolutePath.TrimStart('/'))
                    ? "postgres"
                    : uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
            };
        }

        return new NpgsqlConnectionStringBuilder(value);
    }
}
