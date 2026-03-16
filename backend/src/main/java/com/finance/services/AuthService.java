package com.finance.services;

import com.finance.dto.AuthDtos.AuthResponse;
import com.finance.dto.AuthDtos.LoginRequest;
import com.finance.dto.AuthDtos.RefreshTokenRequest;
import com.finance.dto.AuthDtos.RegisterRequest;
import com.finance.dto.AuthDtos.UserResponse;
import com.finance.entities.User;
import com.finance.exception.BadRequestException;
import com.finance.exception.UnauthorizedException;
import com.finance.repositories.UserRepository;
import com.finance.security.FinanceUserPrincipal;
import com.finance.security.JwtService;
import lombok.RequiredArgsConstructor;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class AuthService {

    private final UserRepository userRepository;
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
