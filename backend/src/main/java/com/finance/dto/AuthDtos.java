package com.finance.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public class AuthDtos {

    public record RegisterRequest(
            @Email @NotBlank String email,
            @NotBlank @Size(min = 8, max = 100) String password,
            @NotBlank @Size(max = 100) String displayName
    ) {
    }

    public record LoginRequest(
            @Email @NotBlank String email,
            @NotBlank String password
    ) {
    }

    public record RefreshTokenRequest(
            @NotBlank String refreshToken
    ) {
    }

    public record ForgotPasswordRequest(
            @Email @NotBlank String email
    ) {
    }

    public record ResetPasswordRequest(
            @NotBlank String token,
            @NotBlank
            @Size(min = 8, max = 100)
            @Pattern(
                    regexp = "^(?=.*[A-Za-z])(?=.*\\d).+$",
                    message = "Password must contain at least one letter and one number"
            )
            String newPassword
    ) {
    }

    public record MessageResponse(
            String message
    ) {
    }

    public record AuthResponse(
            String accessToken,
            String refreshToken,
            String tokenType,
            long expiresIn,
            UserResponse user
    ) {
    }

    public record UserResponse(
            java.util.UUID id,
            String email,
            String displayName
    ) {
    }
}
