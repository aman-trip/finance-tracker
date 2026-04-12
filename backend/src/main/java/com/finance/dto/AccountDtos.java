package com.finance.dto;

import com.finance.entities.AccountType;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.UUID;

public class AccountDtos {

    public record AccountRequest(
            @NotBlank String name,
            @NotNull AccountType type,
            @NotNull @DecimalMin(value = "0.0", inclusive = true) BigDecimal openingBalance,
            String institutionName
    ) {
    }

    public record AccountResponse(
            UUID id,
            String name,
            AccountType type,
            BigDecimal openingBalance,
            BigDecimal currentBalance,
            String institutionName,
            OffsetDateTime createdAt
    ) {
    }

    public record TransferRequest(
            @NotNull UUID sourceAccountId,
            @NotNull UUID targetAccountId,
            @NotNull @DecimalMin(value = "0.01") BigDecimal amount,
            @NotNull java.time.LocalDate transactionDate,
            String note
    ) {
    }
}
