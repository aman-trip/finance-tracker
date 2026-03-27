namespace FinanceTracker.Api.Options;

public sealed class JwtOptions
{
    public string AccessTokenSecret { get; init; } = string.Empty;

    public string RefreshTokenSecret { get; init; } = string.Empty;

    public long AccessTokenExpirationSeconds { get; init; } = 3600;

    public long RefreshTokenExpirationSeconds { get; init; } = 1_209_600;
}
