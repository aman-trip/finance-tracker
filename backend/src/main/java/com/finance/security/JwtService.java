package com.finance.security;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.io.Decoders;
import io.jsonwebtoken.security.Keys;
import java.security.Key;
import java.time.Instant;
import java.util.Date;
import java.util.Map;
import java.util.UUID;
import javax.crypto.SecretKey;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

@Service
public class JwtService {

    private final SecretKey accessTokenKey;
    private final SecretKey refreshTokenKey;
    private final long accessTokenExpirationSeconds;
    private final long refreshTokenExpirationSeconds;

    public JwtService(
            @Value("${app.jwt.access-token-secret}") String accessTokenSecret,
            @Value("${app.jwt.refresh-token-secret}") String refreshTokenSecret,
            @Value("${app.jwt.access-token-expiration-seconds}") long accessTokenExpirationSeconds,
            @Value("${app.jwt.refresh-token-expiration-seconds}") long refreshTokenExpirationSeconds
    ) {
        this.accessTokenKey = Keys.hmacShaKeyFor(Decoders.BASE64.decode(accessTokenSecret));
        this.refreshTokenKey = Keys.hmacShaKeyFor(Decoders.BASE64.decode(refreshTokenSecret));
        this.accessTokenExpirationSeconds = accessTokenExpirationSeconds;
        this.refreshTokenExpirationSeconds = refreshTokenExpirationSeconds;
    }

    public String generateAccessToken(FinanceUserPrincipal principal) {
        return buildToken(principal, accessTokenKey, accessTokenExpirationSeconds, Map.of("uid", principal.userId().toString()));
    }

    public String generateRefreshToken(FinanceUserPrincipal principal) {
        return buildToken(principal, refreshTokenKey, refreshTokenExpirationSeconds, Map.of("uid", principal.userId().toString(), "type", "refresh"));
    }

    public String extractUsername(String token, boolean refreshToken) {
        return parseClaims(token, refreshToken ? refreshTokenKey : accessTokenKey).getSubject();
    }

    public UUID extractUserId(String token, boolean refreshToken) {
        return UUID.fromString(parseClaims(token, refreshToken ? refreshTokenKey : accessTokenKey).get("uid", String.class));
    }

    public boolean isTokenValid(String token, FinanceUserPrincipal principal, boolean refreshToken) {
        Claims claims = parseClaims(token, refreshToken ? refreshTokenKey : accessTokenKey);
        return principal.getUsername().equals(claims.getSubject()) && claims.getExpiration().after(new Date());
    }

    public long getAccessTokenExpirationSeconds() {
        return accessTokenExpirationSeconds;
    }

    private String buildToken(FinanceUserPrincipal principal, Key key, long expiresInSeconds, Map<String, Object> claims) {
        Instant now = Instant.now();
        return Jwts.builder()
                .claims(claims)
                .subject(principal.getUsername())
                .issuedAt(Date.from(now))
                .expiration(Date.from(now.plusSeconds(expiresInSeconds)))
                .signWith(key)
                .compact();
    }

    private Claims parseClaims(String token, Key key) {
        return Jwts.parser()
                .verifyWith((SecretKey) key)
                .build()
                .parseSignedClaims(token)
                .getPayload();
    }
}
