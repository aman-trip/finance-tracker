package com.finance.services;

import com.finance.dto.AuthDtos.AuthResponse;
import com.finance.dto.AuthDtos.ForgotPasswordRequest;
import com.finance.dto.AuthDtos.LoginRequest;
import com.finance.dto.AuthDtos.MessageResponse;
import com.finance.dto.AuthDtos.RefreshTokenRequest;
import com.finance.dto.AuthDtos.RegisterRequest;
import com.finance.dto.AuthDtos.ResetPasswordRequest;
import com.finance.dto.AuthDtos.UserResponse;
import com.finance.entities.PasswordResetToken;
import com.finance.entities.User;
import com.finance.exception.BadRequestException;
import com.finance.exception.UnauthorizedException;
import com.finance.repositories.PasswordResetTokenRepository;
import com.finance.repositories.UserRepository;
import com.finance.security.FinanceUserPrincipal;
import com.finance.security.JwtService;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Optional;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
@Slf4j
public class AuthService {

    private static final String FORGOT_PASSWORD_SUCCESS_MESSAGE = "If this email exists, a reset link has been sent";
    private static final String RESET_PASSWORD_SUCCESS_MESSAGE = "Password reset successful";

    private final UserRepository userRepository;
    private final PasswordResetTokenRepository passwordResetTokenRepository;
    private final PasswordEncoder passwordEncoder;
    private final AuthenticationManager authenticationManager;
    private final JwtService jwtService;
    private final CategoryService categoryService;

    @Transactional
    public AuthResponse register(RegisterRequest request) {
        if (userRepository.existsByEmailIgnoreCase(request.email())) {
            throw new BadRequestException("Email is already registered");
        }

        User user = new User();
        user.setEmail(request.email().trim().toLowerCase());
        user.setDisplayName(request.displayName().trim());
        user.setPasswordHash(passwordEncoder.encode(request.password()));
        userRepository.save(user);
        categoryService.createDefaultCategories(user);

        return buildAuthResponse(new FinanceUserPrincipal(user.getId(), user.getEmail(), user.getPasswordHash()), user);
    }

    @Transactional(readOnly = true)
    public AuthResponse login(LoginRequest request) {
        authenticationManager.authenticate(new UsernamePasswordAuthenticationToken(request.email(), request.password()));
        User user = userRepository.findByEmailIgnoreCase(request.email())
                .orElseThrow(() -> new UnauthorizedException("Invalid credentials"));
        return buildAuthResponse(new FinanceUserPrincipal(user.getId(), user.getEmail(), user.getPasswordHash()), user);
    }

    @Transactional(readOnly = true)
    public AuthResponse refresh(RefreshTokenRequest request) {
        String refreshToken = request.refreshToken();
        String email;
        try {
            email = jwtService.extractUsername(refreshToken, true);
        } catch (Exception exception) {
            throw new UnauthorizedException("Invalid refresh token");
        }
        User user = userRepository.findByEmailIgnoreCase(email).orElseThrow(() -> new UnauthorizedException("Invalid refresh token"));
        FinanceUserPrincipal principal = new FinanceUserPrincipal(user.getId(), user.getEmail(), user.getPasswordHash());
        if (!jwtService.isTokenValid(refreshToken, principal, true)) {
            throw new UnauthorizedException("Refresh token expired or invalid");
        }
        return buildAuthResponse(principal, user);
    }

    @Transactional
    public MessageResponse forgotPassword(ForgotPasswordRequest request) {
        Optional<User> userOptional = userRepository.findByEmailIgnoreCase(request.email().trim());
        if (userOptional.isPresent()) {
            User user = userOptional.get();
            passwordResetTokenRepository.deleteByUser_Id(user.getId());

            String token = UUID.randomUUID().toString();
            PasswordResetToken resetToken = new PasswordResetToken();
            resetToken.setToken(token);
            resetToken.setUser(user);
            resetToken.setExpiryTime(OffsetDateTime.now(ZoneOffset.UTC).plusMinutes(15));
            passwordResetTokenRepository.save(resetToken);

            log.info("Password reset link for {}: http://localhost:4173/reset-password?token={}", user.getEmail(), token);
        }

        return new MessageResponse(FORGOT_PASSWORD_SUCCESS_MESSAGE);
    }

    @Transactional
    public MessageResponse resetPassword(ResetPasswordRequest request) {
        PasswordResetToken resetToken = passwordResetTokenRepository.findByToken(request.token())
                .orElseThrow(() -> new BadRequestException("Invalid or expired reset token"));

        if (resetToken.getExpiryTime().isBefore(OffsetDateTime.now(ZoneOffset.UTC))) {
            passwordResetTokenRepository.delete(resetToken);
            throw new BadRequestException("Invalid or expired reset token");
        }

        User user = resetToken.getUser();
        user.setPasswordHash(passwordEncoder.encode(request.newPassword()));
        userRepository.save(user);

        passwordResetTokenRepository.delete(resetToken);

        return new MessageResponse(RESET_PASSWORD_SUCCESS_MESSAGE);
    }

    private AuthResponse buildAuthResponse(FinanceUserPrincipal principal, User user) {
        return new AuthResponse(
                jwtService.generateAccessToken(principal),
                jwtService.generateRefreshToken(principal),
                "Bearer",
                jwtService.getAccessTokenExpirationSeconds(),
                new UserResponse(user.getId(), user.getEmail(), user.getDisplayName())
        );
    }
}
