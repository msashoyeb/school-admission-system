using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SchoolAdmission.Infrastructure.Persistence
{
    public static class DatabaseConnectionResolver
    {
        public static string Resolve(IConfiguration configuration)
        {
            var databaseUrl = configuration["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                return databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                       databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                    ? ConvertPostgresUri(databaseUrl)
                    : databaseUrl;
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            throw new InvalidOperationException(
                "Database connection is not configured. Set DATABASE_URL or ConnectionStrings:DefaultConnection.");
        }

        private static string ConvertPostgresUri(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':', 2);
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
            };

            return builder.ConnectionString;
        }
    }
}
