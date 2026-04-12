package com.finance.controllers;

import com.finance.dto.ReportDtos.AccountBalanceTrendItem;
import com.finance.dto.ReportDtos.CategorySpendResponse;
import com.finance.dto.ReportDtos.FutureBalancePredictionResponse;
import com.finance.dto.ReportDtos.IncomeExpenseTrendItem;
import com.finance.dto.ReportDtos.InsightItem;
import com.finance.services.ReportService;
import java.time.LocalDate;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/reports")
@RequiredArgsConstructor
public class ReportController {

    private final ReportService reportService;

    @GetMapping("/category-spend")
    public CategorySpendResponse categorySpend(
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate startDate,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate endDate
    ) {
        return reportService.categorySpend(startDate, endDate);
    }

    @GetMapping("/income-vs-expense")
    public List<IncomeExpenseTrendItem> incomeVsExpense(
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate startDate,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate endDate
    ) {
        return reportService.incomeExpenseTrend(startDate, endDate);
    }

    @GetMapping("/account-balance-trend")
    public List<AccountBalanceTrendItem> accountBalanceTrend(
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate startDate,
            @RequestParam(required = false) @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate endDate
    ) {
        return reportService.accountBalanceTrend(startDate, endDate);
    }

    @GetMapping("/insights")
    public List<InsightItem> insights() {
        return reportService.insights();
    }

    @GetMapping("/future-balance-prediction")
    public FutureBalancePredictionResponse futureBalancePrediction() {
        return reportService.futureBalancePrediction();
    }
}
