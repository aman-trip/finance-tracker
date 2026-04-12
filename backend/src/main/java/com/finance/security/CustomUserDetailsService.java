package com.finance.security;

import com.finance.exception.UnauthorizedException;
import com.finance.repositories.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class CustomUserDetailsService implements UserDetailsService {

    private final UserRepository userRepository;

    @Override
    public UserDetails loadUserByUsername(String username) {
        var user = userRepository.findByEmailIgnoreCase(username)
                .orElseThrow(() -> new UnauthorizedException("Invalid credentials"));
        return new FinanceUserPrincipal(user.getId(), user.getEmail(), user.getPasswordHash());
    }
}
