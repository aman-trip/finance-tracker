using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Controllers;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Options;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Api.Tests;

public sealed class ContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Controllers_KeepExpectedRouteTemplates()
    {
        AssertRoute<AuthController>("api/auth");
        AssertRoute<AccountController>("api/accounts");
        AssertRoute<CategoryController>("api/categories");
        AssertRoute<BudgetController>("api/budgets");
        AssertRoute<GoalController>("api/goals");
        AssertRoute<TransactionController>("api/transactions");
        AssertRoute<RecurringTransactionController>("api/recurring");
        AssertRoute<ReportController>("api/reports");
        AssertRoute<HealthController>("actuator");
    }

    [Fact]
    public void AuthResponse_Serializes_WithSpringCompatibleFieldNames()
    {
        var payload = new AuthResponse(
            "access",
            "refresh",
            "Bearer",
            3600,
            new UserResponse(Guid.Parse("11111111-1111-1111-1111-111111111111"), "user@example.com", "Aman"));

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        Assert.Contains("\"accessToken\"", json);
        Assert.Contains("\"refreshToken\"", json);
        Assert.Contains("\"tokenType\"", json);
        Assert.Contains("\"expiresIn\"", json);
        Assert.Contains("\"displayName\"", json);
    }

    [Fact]
    public void PageResponse_Serializes_SpringStylePagingFields()
    {
        var response = new PageResponse<TransactionResponse>(
            [],
            new PageableResponse(new SortResponse(true, false, false), 0, 0, 10, true, false),
            0,
            0,
            true,
            10,
            0,
            new SortResponse(true, false, false),
            0,
            true,
            true);

        var json = JsonSerializer.Serialize(response, JsonOptions);

        Assert.Contains("\"content\"", json);
        Assert.Contains("\"pageable\"", json);
        Assert.Contains("\"totalElements\"", json);
        Assert.Contains("\"totalPages\"", json);
        Assert.Contains("\"numberOfElements\"", json);
        Assert.Contains("\"first\"", json);
        Assert.Contains("\"last\"", json);
    }

    [Fact]
    public void JwtService_GeneratesExpectedClaims()
    {
        var jwtService = new JwtService(
            new JwtOptions
            {
                AccessTokenSecret = string.Empty,
                RefreshTokenSecret = string.Empty,
                AccessTokenExpirationSeconds = 3600,
                RefreshTokenExpirationSeconds = 1_209_600
            },
            new TestHostEnvironment(),
            NullLogger<JwtService>.Instance);

        var user = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "user@example.com",
            DisplayName = "Aman",
            PasswordHash = "hash"
        };

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken(user);

        var handler = new JwtSecurityTokenHandler();
        var accessJwt = handler.ReadJwtToken(accessToken);
        var refreshJwt = handler.ReadJwtToken(refreshToken);

        Assert.Equal("user@example.com", accessJwt.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Id.ToString(), accessJwt.Claims.First(claim => claim.Type == "uid").Value);
        Assert.DoesNotContain(accessJwt.Claims, claim => claim.Type == "type");
        Assert.Equal("refresh", refreshJwt.Claims.First(claim => claim.Type == "type").Value);
        Assert.True(jwtService.IsTokenValid(refreshToken, user, refreshToken: true));
    }

    private static void AssertRoute<TController>(string expectedTemplate)
    {
        var route = typeof(TController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal(expectedTemplate, route!.Template);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "FinanceTracker.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
