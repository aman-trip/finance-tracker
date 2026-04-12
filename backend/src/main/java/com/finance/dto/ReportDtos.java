package com.finance.dto;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.Map;

public class ReportDtos {

    public record CategorySpendResponse(
            List<Map<String, Object>> items,
            BigDecimal total
    ) {
    }

    public record IncomeExpenseTrendItem(
            LocalDate date,
            BigDecimal income,
            BigDecimal expense
    ) {
    }

    public record AccountBalanceTrendItem(
            String accountName,
            List<Point> points
    ) {
    }

    public record Point(
            LocalDate date,
            BigDecimal balance
    ) {
    }

    public record InsightItem(
            String type,
            String title,
            String description,
            String severity
    ) {
    }

    public record FutureBalancePredictionResponse(
            BigDecimal currentBalance,
            BigDecimal projectedRecurringNet,
            BigDecimal averageDailySpending,
            BigDecimal predictedBalance,
            Integer horizonDays
    ) {
    }
}
