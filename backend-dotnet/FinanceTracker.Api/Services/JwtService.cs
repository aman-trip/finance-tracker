using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;
using FinanceTracker.Api.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Api.Services;

public sealed class JwtService
{
    private readonly SymmetricSecurityKey _accessKey;
    private readonly SymmetricSecurityKey _refreshKey;
    private readonly JwtOptions _options;

    public JwtService(JwtOptions options, IHostEnvironment environment, ILogger<JwtService> logger)
    {
        _options = options;
        var accessSecret = JwtKeyFactory.ResolveSecret(options.AccessTokenSecret, false, environment);
        var refreshSecret = JwtKeyFactory.ResolveSecret(options.RefreshTokenSecret, true, environment);

        _accessKey = JwtKeyFactory.BuildKey(accessSecret);
        _refreshKey = JwtKeyFactory.BuildKey(refreshSecret);
        logger.LogInformation("JWT secrets loaded successfully");
    }

    public SymmetricSecurityKey AccessSigningKey => _accessKey;

    public string GenerateAccessToken(User user)
    {
        return BuildToken(
            user,
            _accessKey,
            _options.AccessTokenExpirationSeconds,
            [new Claim("uid", user.Id.ToString())]
        );
    }

    public string GenerateRefreshToken(User user)
    {
        return BuildToken(
            user,
            _refreshKey,
            _options.RefreshTokenExpirationSeconds,
            [new Claim("uid", user.Id.ToString()), new Claim("type", "refresh")]
        );
    }

    public string ExtractUsername(string token, bool refreshToken)
    {
        var principal = ValidateToken(token, refreshToken ? _refreshKey : _accessKey, validateLifetime: false);
        return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? throw new SecurityTokenException("Missing subject claim");
    }

    public Guid ExtractUserId(string token, bool refreshToken)
    {
        var principal = ValidateToken(token, refreshToken ? _refreshKey : _accessKey, validateLifetime: false);
        return Guid.Parse(principal.FindFirstValue("uid") ?? throw new SecurityTokenException("Missing uid claim"));
    }

    public bool IsTokenValid(string token, User user, bool refreshToken)
    {
        var principal = ValidateToken(token, refreshToken ? _refreshKey : _accessKey, validateLifetime: false);
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var expiresAt = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

        return string.Equals(subject, user.Email, StringComparison.Ordinal)
               && long.TryParse(expiresAt, out var expUnix)
               && DateTimeOffset.FromUnixTimeSeconds(expUnix) > DateTimeOffset.UtcNow;
    }

    public long GetAccessTokenExpirationSeconds() => _options.AccessTokenExpirationSeconds;

    private static ClaimsPrincipal ValidateToken(string token, SecurityKey key, bool validateLifetime)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = validateLifetime,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub
        }, out _);
    }

    private static string BuildToken(User user, SecurityKey key, long expirationSeconds, IEnumerable<Claim> additionalClaims)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email)
        };
        claims.AddRange(additionalClaims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            Expires = now.AddSeconds(expirationSeconds),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}
