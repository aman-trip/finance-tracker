using Npgsql;

namespace FinanceTracker.Api.Infrastructure;

public static class ConnectionStringFactory
{
    public static string Resolve(IConfiguration configuration)
    {
        var dbUrl = configuration["DB_URL"];
        var username = configuration["DB_USER"] ?? configuration["DB_USERNAME"];
        var password = configuration["DB_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(dbUrl))
        {
            return dbUrl.StartsWith("jdbc:postgresql://", StringComparison.OrdinalIgnoreCase)
                ? FromJdbcUrl(dbUrl, username, password)
                : dbUrl;
        }

        var dbHost = configuration["DB_HOST"];
        if (!string.IsNullOrWhiteSpace(dbHost))
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = dbHost,
                Port = int.TryParse(configuration["DB_PORT"], out var port) ? port : 5432,
                Database = configuration["DB_NAME"] ?? "finance_tracker",
                Username = string.IsNullOrWhiteSpace(username) ? "postgres" : username,
                Password = string.IsNullOrWhiteSpace(password) ? "postgres" : password
            };

            return builder.ConnectionString;
        }

        return configuration.GetConnectionString("Default")
               ?? "Host=localhost;Port=5432;Database=finance_tracker;Username=postgres;Password=postgres";
    }

    private static string FromJdbcUrl(string jdbcUrl, string? username, string? password)
    {
        var uri = new Uri(jdbcUrl["jdbc:".Length..]);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = string.IsNullOrWhiteSpace(username) ? "postgres" : username,
            Password = string.IsNullOrWhiteSpace(password) ? "postgres" : password
        };

        return builder.ConnectionString;
    }
}
