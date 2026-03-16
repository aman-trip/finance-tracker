package com.finance.dto;

import com.finance.entities.TransactionType;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.UUID;

public class TransactionDtos {

    public record TransactionRequest(
            @NotNull UUID accountId,
            UUID categoryId,
            @NotNull TransactionType type,
            @NotNull @DecimalMin(value = "0.01") BigDecimal amount,
            @NotNull LocalDate transactionDate,
            String merchant,
            String note,
            String paymentMethod
    ) {
    }

    public record TransactionResponse(
            UUID id,
            UUID accountId,
            String accountName,
            UUID categoryId,
            String categoryName,
            TransactionType type,
            BigDecimal amount,
            LocalDate transactionDate,
            String merchant,
            String note,
            String paymentMethod,
            OffsetDateTime createdAt,
            OffsetDateTime updatedAt
    ) {
    }
}
