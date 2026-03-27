using BCrypt.Net;
using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceTracker.Api.Services;

public sealed class AuthService(
    FinanceTrackerDbContext dbContext,
    CategoryService categoryService,
    JwtService jwtService,
    IConfiguration configuration,
    ILogger<AuthService> logger)
{
    private const string ForgotPasswordSuccessMessage = "If this email exists, a reset link has been sent";
    private const string ResetPasswordSuccessMessage = "Password reset successful";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.email);
        var displayName = NormalizeDisplayName(request.displayName);
        var password = request.password ?? string.Empty;

        logger.LogInformation("Register request received for {Email}", normalizedEmail);

        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);

        logger.LogInformation("Existing user lookup for {Email}: {Exists}", normalizedEmail, exists);
        if (exists)
        {
            throw new BadRequestException("Email is already registered");
        }

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            await categoryService.CreateDefaultCategoriesAsync(user.Id, cancellationToken);

            var response = BuildAuthResponse(user);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("User registration succeeded for {Email} with id {UserId}", normalizedEmail, user.Id);
            return response;
        }
        catch (DbUpdateException exception) when (IsDuplicateEmailViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(exception, "Duplicate email constraint hit while registering {Email}", normalizedEmail);
            throw new BadRequestException("Email is already registered");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            logger.LogError(exception, "User registration failed for {Email}", normalizedEmail);
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.email);
        logger.LogInformation("Login attempt received for {Email}", normalizedEmail);

        var user = await FindUserByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.password ?? string.Empty, user.PasswordHash))
        {
            logger.LogWarning("Login failed for {Email}", normalizedEmail);
            throw new UnauthorizedException("Invalid credentials");
        }

        logger.LogInformation("Login succeeded for {Email} with id {UserId}", normalizedEmail, user.Id);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        string email;
        try
        {
            email = NormalizeEmail(request.refreshToken is null
                ? null
                : jwtService.ExtractUsername(request.refreshToken, refreshToken: true));
        }
        catch
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        logger.LogInformation("Refresh token attempt received for {Email}", email);

        var user = await FindUserByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Refresh token rejected because user was not found for {Email}", email);
            throw new UnauthorizedException("Invalid refresh token");
        }

        if (!jwtService.IsTokenValid(request.refreshToken!, user, refreshToken: true))
        {
            logger.LogWarning("Refresh token validation failed for {Email}", email);
            throw new UnauthorizedException("Refresh token expired or invalid");
        }

        logger.LogInformation("Refresh token succeeded for {Email} with id {UserId}", email, user.Id);
        return BuildAuthResponse(user);
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.email);
        logger.LogInformation("Forgot password request received for {Email}", normalizedEmail);

        var user = await FindUserByEmailAsync(normalizedEmail, cancellationToken);
        logger.LogInformation("Forgot password user lookup for {Email}: {Exists}", normalizedEmail, user is not null);

        if (user is not null)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingTokens = await dbContext.PasswordResetTokens
                    .Where(token => token.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                dbContext.PasswordResetTokens.RemoveRange(existingTokens);

                var token = Guid.NewGuid().ToString();
                dbContext.PasswordResetTokens.Add(new PasswordResetToken
                {
                    Token = token,
                    UserId = user.Id,
                    ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(15)
                });

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Password reset link for {Email}: {ResetLink}",
                    user.Email,
                    BuildResetPasswordUrl(token, configuration));
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                logger.LogError(exception, "Forgot password flow failed while generating reset token for {Email}", normalizedEmail);
                throw;
            }
        }

        return new MessageResponse(ForgotPasswordSuccessMessage);
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var resetToken = await dbContext.PasswordResetTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == request.token, cancellationToken)
               ?? throw new BadRequestException("Invalid or expired reset token");

        if (resetToken.ExpiryTime < DateTimeOffset.UtcNow)
        {
            dbContext.PasswordResetTokens.Remove(resetToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new BadRequestException("Invalid or expired reset token");
        }

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.newPassword!);
        dbContext.PasswordResetTokens.Remove(resetToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MessageResponse(ResetPasswordSuccessMessage);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return new AuthResponse(
            jwtService.GenerateAccessToken(user),
            jwtService.GenerateRefreshToken(user),
            "Bearer",
            jwtService.GetAccessTokenExpirationSeconds(),
            new UserResponse(user.Id, user.Email, user.DisplayName)
        );
    }

    private async Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Email must not be blank");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new BadRequestException("Display name must not be blank");
        }

        return displayName.Trim();
    }

    private static bool IsDuplicateEmailViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        if (!string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
            return false;
        }

        var constraintName = postgresException.ConstraintName ?? string.Empty;
        var detail = postgresException.Detail ?? string.Empty;

        return constraintName.Contains("email", StringComparison.OrdinalIgnoreCase)
               || detail.Contains("(email)", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildResetPasswordUrl(string token, IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["FRONTEND_BASE_URL"] ?? configuration["App:FrontendBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? "http://localhost:4173"
            : configuredBaseUrl.Trim().TrimEnd('/');
        return $"{baseUrl}/reset-password?token={token}";
    }
}
