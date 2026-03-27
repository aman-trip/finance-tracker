using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record RegisterRequest(
    [param: EmailAddress(ErrorMessage = "must be a well-formed email address")]
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? email,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    [param: StringLength(100, MinimumLength = 8, ErrorMessage = "size must be between 8 and 100")]
    string? password,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    [param: StringLength(100, ErrorMessage = "size must be between 0 and 100")]
    string? displayName
);

public sealed record LoginRequest(
    [param: EmailAddress(ErrorMessage = "must be a well-formed email address")]
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? email,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? password
);

public sealed record RefreshTokenRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? refreshToken
);

public sealed record ForgotPasswordRequest(
    [param: EmailAddress(ErrorMessage = "must be a well-formed email address")]
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? email
);

public sealed record ResetPasswordRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? token,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    [param: StringLength(100, MinimumLength = 8, ErrorMessage = "size must be between 8 and 100")]
    [param: RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).+$", ErrorMessage = "Password must contain at least one letter and one number")]
    string? newPassword
);

public sealed record MessageResponse(string message);

public sealed record AuthResponse(
    string accessToken,
    string refreshToken,
    string tokenType,
    long expiresIn,
    UserResponse user
);

public sealed record UserResponse(
    Guid id,
    string email,
    string displayName
);
