using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Infrastructure;
using FinanceTracker.Api.Options;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var sharedJwtSecret = builder.Configuration["JWT_SECRET"];

var jwtOptions = new JwtOptions
{
    AccessTokenSecret = builder.Configuration["JWT_ACCESS_SECRET"] ?? sharedJwtSecret ?? builder.Configuration["App:Jwt:AccessTokenSecret"] ?? string.Empty,
    RefreshTokenSecret = builder.Configuration["JWT_REFRESH_SECRET"] ?? sharedJwtSecret ?? builder.Configuration["App:Jwt:RefreshTokenSecret"] ?? string.Empty,
    AccessTokenExpirationSeconds = long.TryParse(builder.Configuration["App:Jwt:AccessTokenExpirationSeconds"], out var accessExpires) ? accessExpires : 3600,
    RefreshTokenExpirationSeconds = long.TryParse(builder.Configuration["App:Jwt:RefreshTokenExpirationSeconds"], out var refreshExpires) ? refreshExpires : 1_209_600
};

var corsOptions = new CorsOptions
{
    AllowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? builder.Configuration["App:Cors:AllowedOrigins"] ?? string.Empty
};

var accessSecret = JwtKeyFactory.ResolveSecret(jwtOptions.AccessTokenSecret, false, builder.Environment);
var accessSigningKey = JwtKeyFactory.BuildKey(accessSecret);
var connectionString = ConnectionStringFactory.Resolve(builder.Configuration);

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(corsOptions);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.AddScoped<ValidationActionFilter>();
builder.Services.AddDbContext<FinanceTrackerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = corsOptions.AllowedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (origins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            return;
        }

        policy.WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKey = accessSigningKey,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var email = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrWhiteSpace(email))
                {
                    context.Fail("Unauthorized");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<FinanceTrackerDbContext>();
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var exists = await dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(user => user.Email.ToLower() == normalizedEmail, context.HttpContext.RequestAborted);
                if (!exists)
                {
                    context.Fail("Unauthorized");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SchemaBootstrapService>();
builder.Services.AddScoped<AccessControlLayer>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<GoalService>();
builder.Services.AddScoped<RecurringTransactionService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ForecastService>();
builder.Services.AddScoped<InsightsService>();
builder.Services.AddScoped<RulesEngineService>();
builder.Services.AddScoped<AccountMembershipService>();
builder.Services.AddHostedService<RecurringTransactionHostedService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var schemaBootstrapService = scope.ServiceProvider.GetRequiredService<SchemaBootstrapService>();
    await schemaBootstrapService.EnsureVersionTwoSchemaAsync(CancellationToken.None);
}

app.UseCors();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
