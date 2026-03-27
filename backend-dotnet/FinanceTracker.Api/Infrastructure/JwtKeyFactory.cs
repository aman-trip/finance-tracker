using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Api.Infrastructure;

public static class JwtKeyFactory
{
    private const string DevAccessSecret = "finance-tracker-dev-access-secret-key-2026-strong";
    private const string DevRefreshSecret = "finance-tracker-dev-refresh-secret-key-2026-strong";

    public static string ResolveSecret(string configuredSecret, bool refreshSecret, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            if (configuredSecret.Trim().Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be at least 32 characters long");
            }

            return configuredSecret.Trim();
        }

        if (environment.IsDevelopment())
        {
            return refreshSecret ? DevRefreshSecret : DevAccessSecret;
        }

        throw new InvalidOperationException("JWT secret is missing. Please configure environment variables.");
    }

    public static SymmetricSecurityKey BuildKey(string secret)
    {
        var keyBytes = DeriveKeyBytes(secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters long");
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    private static byte[] DeriveKeyBytes(string secret)
    {
        try
        {
            var decoded = Convert.FromBase64String(secret);
            if (decoded.Length >= 32)
            {
                return decoded;
            }
        }
        catch (FormatException)
        {
        }

        return Encoding.UTF8.GetBytes(secret);
    }
}
