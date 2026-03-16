package com.finance.dto;

import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;
import java.util.UUID;

public class BudgetDtos {

    public record BudgetRequest(
            @NotNull UUID categoryId,
            @NotNull @Min(1) @Max(12) Integer month,
            @NotNull @Min(2000) @Max(2100) Integer year,
            @NotNull @DecimalMin(value = "0.01") BigDecimal amount,
            @NotNull @Min(1) @Max(200) Integer alertThresholdPercent
    ) {
    }

    public record BudgetResponse(
            UUID id,
            UUID categoryId,
            String categoryName,
            Integer month,
            Integer year,
            BigDecimal amount,
            Integer alertThresholdPercent,
            BigDecimal spentAmount,
            BigDecimal utilizationPercent,
            String alertLevel
    ) {
    }
}
