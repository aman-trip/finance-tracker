package com.finance.dto;

import com.finance.entities.RecurringFrequency;
import com.finance.entities.TransactionType;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

public class RecurringDtos {

    public record RecurringTransactionRequest(
            @NotBlank String title,
            @NotNull TransactionType type,
            @NotNull @DecimalMin(value = "0.01") BigDecimal amount,
            UUID categoryId,
            @NotNull UUID accountId,
            @NotNull RecurringFrequency frequency,
            @NotNull LocalDate startDate,
            LocalDate endDate,
            @NotNull LocalDate nextRunDate,
            boolean autoCreateTransaction
    ) {
    }

    public record RecurringTransactionResponse(
            UUID id,
            String title,
            TransactionType type,
            BigDecimal amount,
            UUID categoryId,
            String categoryName,
            UUID accountId,
            String accountName,
            RecurringFrequency frequency,
            LocalDate startDate,
            LocalDate endDate,
            LocalDate nextRunDate,
            boolean autoCreateTransaction
    ) {
    }
}
