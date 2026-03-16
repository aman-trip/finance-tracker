package com.finance.security;

import com.finance.exception.UnauthorizedException;
import java.util.UUID;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;

@Component
public class CurrentUserService {

    public UUID getCurrentUserId() {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication == null || !(authentication.getPrincipal() instanceof FinanceUserPrincipal principal)) {
            throw new UnauthorizedException("Authentication required");
        }
        return principal.userId();
    }
}
