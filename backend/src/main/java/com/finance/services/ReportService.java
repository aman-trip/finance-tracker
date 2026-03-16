package com.finance.services;

import com.finance.dto.ReportDtos.AccountBalanceTrendItem;
import com.finance.dto.ReportDtos.CategorySpendResponse;
import com.finance.dto.ReportDtos.IncomeExpenseTrendItem;
import com.finance.dto.ReportDtos.Point;
import com.finance.entities.TransactionType;
import com.finance.repositories.AccountRepository;
import com.finance.repositories.TransactionRepository;
import com.finance.security.CurrentUserService;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.YearMonth;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class ReportService {

    private final TransactionRepository transactionRepository;
    private final AccountRepository accountRepository;
    private final CurrentUserService currentUserService;

    @Transactional(readOnly = true)
    public CategorySpendResponse categorySpend(LocalDate startDate, LocalDate endDate) {
        UUID userId = currentUserService.getCurrentUserId();
        var range = normalizeRange(startDate, endDate);
        Map<String, BigDecimal> grouped = new LinkedHashMap<>();
        transactionRepository.findByUserIdAndTransactionDateBetween(userId, range[0], range[1]).stream()
                .filter(tx -> tx.getType() == TransactionType.EXPENSE)
                .forEach(tx -> grouped.merge(
                        tx.getCategory() == null ? "Uncategorized" : tx.getCategory().getName(),
                        tx.getAmount(),
                        BigDecimal::add
                ));
        List<Map<String, Object>> items = grouped.entrySet().stream()
                .sorted(Map.Entry.<String, BigDecimal>comparingByValue().reversed())
                .map(entry -> Map.<String, Object>of("category", entry.getKey(), "amount", entry.getValue()))
                .toList();
        BigDecimal total = grouped.values().stream().reduce(BigDecimal.ZERO, BigDecimal::add);
        return new CategorySpendResponse(items, total);
    }

    @Transactional(readOnly = true)
    public List<IncomeExpenseTrendItem> incomeExpenseTrend(LocalDate startDate, LocalDate endDate) {
        UUID userId = currentUserService.getCurrentUserId();
        var range = normalizeRange(startDate, endDate);
        Map<LocalDate, IncomeExpenseTrendAccumulator> grouped = new LinkedHashMap<>();
        transactionRepository.findByUserIdAndTransactionDateBetween(userId, range[0], range[1]).stream()
                .sorted(Comparator.comparing(com.finance.entities.Transaction::getTransactionDate))
                .forEach(tx -> {
                    IncomeExpenseTrendAccumulator accumulator = grouped.computeIfAbsent(
                            tx.getTransactionDate(),
                            date -> new IncomeExpenseTrendAccumulator()
                    );
                    if (tx.getType() == TransactionType.INCOME || tx.getType() == TransactionType.TRANSFER_IN) {
                        accumulator.income = accumulator.income.add(tx.getAmount());
                    } else {
                        accumulator.expense = accumulator.expense.add(tx.getAmount());
                    }
                });

        return grouped.entrySet().stream()
                .map(entry -> new IncomeExpenseTrendItem(entry.getKey(), entry.getValue().income, entry.getValue().expense))
                .toList();
    }

    @Transactional(readOnly = true)
    public List<AccountBalanceTrendItem> accountBalanceTrend(LocalDate startDate, LocalDate endDate) {
        UUID userId = currentUserService.getCurrentUserId();
        var range = normalizeRange(startDate, endDate);
        return accountRepository.findByUserIdOrderByCreatedAtDesc(userId).stream()
                .map(account -> {
                    BigDecimal running = account.getOpeningBalance();
                    List<Point> points = new ArrayList<>();
                    var transactions = transactionRepository.findByUserIdAndAccountIdOrderByTransactionDateAsc(userId, account.getId());
                    for (var tx : transactions) {
                        if (tx.getType() == TransactionType.INCOME || tx.getType() == TransactionType.TRANSFER_IN) {
                            running = running.add(tx.getAmount());
                        } else {
                            running = running.subtract(tx.getAmount());
                        }
                        if (!tx.getTransactionDate().isBefore(range[0]) && !tx.getTransactionDate().isAfter(range[1])) {
                            points.add(new Point(tx.getTransactionDate(), running));
                        }
                    }
                    if (points.isEmpty()) {
                        points.add(new Point(range[0], running));
                    }
                    return new AccountBalanceTrendItem(account.getName(), points);
                })
                .toList();
    }

    private LocalDate[] normalizeRange(LocalDate startDate, LocalDate endDate) {
        if (startDate != null && endDate != null) {
            return new LocalDate[]{startDate, endDate};
        }
        YearMonth current = YearMonth.now();
        return new LocalDate[]{current.atDay(1), current.atEndOfMonth()};
    }

    private static class IncomeExpenseTrendAccumulator {
        private BigDecimal income = BigDecimal.ZERO;
        private BigDecimal expense = BigDecimal.ZERO;
    }
}
