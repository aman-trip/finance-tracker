package com.finance.services;

import com.finance.dto.BudgetDtos.BudgetRequest;
import com.finance.dto.BudgetDtos.BudgetResponse;
import com.finance.entities.Budget;
import com.finance.entities.TransactionType;
import com.finance.exception.BadRequestException;
import com.finance.exception.NotFoundException;
import com.finance.repositories.BudgetRepository;
import com.finance.repositories.TransactionRepository;
import com.finance.repositories.UserRepository;
import com.finance.security.CurrentUserService;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.LocalDate;
import java.time.YearMonth;
import java.util.List;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class BudgetService {

    private final BudgetRepository budgetRepository;
    private final CategoryService categoryService;
    private final TransactionRepository transactionRepository;
    private final UserRepository userRepository;
    private final CurrentUserService currentUserService;

    @Transactional(readOnly = true)
    public List<BudgetResponse> getAll() {
        UUID userId = currentUserService.getCurrentUserId();
        return budgetRepository.findByUserIdOrderByYearDescMonthDesc(userId).stream()
                .map(budget -> toResponse(budget))
                .toList();
    }

    @Transactional
    public BudgetResponse create(BudgetRequest request) {
        UUID userId = currentUserService.getCurrentUserId();
        validateUniqueBudget(userId, request, null);
        Budget budget = new Budget();
        budget.setUser(userRepository.findById(userId).orElseThrow(() -> new NotFoundException("User not found")));
        map(budget, request, userId);
        return toResponse(budgetRepository.save(budget));
    }

    @Transactional
    public BudgetResponse update(UUID id, BudgetRequest request) {
        UUID userId = currentUserService.getCurrentUserId();
        Budget budget = getBudget(id, userId);
        validateUniqueBudget(userId, request, id);
        map(budget, request, userId);
        return toResponse(budgetRepository.save(budget));
    }

    @Transactional
    public void delete(UUID id) {
        budgetRepository.delete(getBudget(id, currentUserService.getCurrentUserId()));
    }

    @Transactional(readOnly = true)
    public Budget getBudget(UUID id, UUID userId) {
        return budgetRepository.findByIdAndUserId(id, userId)
                .orElseThrow(() -> new NotFoundException("Budget not found"));
    }

    private void map(Budget budget, BudgetRequest request, UUID userId) {
        budget.setCategory(categoryService.getCategory(request.categoryId(), userId));
        budget.setMonth(request.month());
        budget.setYear(request.year());
        budget.setAmount(request.amount());
        budget.setAlertThresholdPercent(request.alertThresholdPercent());
    }

    private void validateUniqueBudget(UUID userId, BudgetRequest request, UUID existingBudgetId) {
        boolean exists = existingBudgetId == null
                ? budgetRepository.existsByUserIdAndCategoryIdAndMonthAndYear(userId, request.categoryId(), request.month(), request.year())
                : budgetRepository.existsByUserIdAndCategoryIdAndMonthAndYearAndIdNot(
                userId,
                request.categoryId(),
                request.month(),
                request.year(),
                existingBudgetId
        );
        if (exists) {
            throw new BadRequestException("Budget already exists for this category and month");
        }
    }

    private BudgetResponse toResponse(Budget budget) {
        YearMonth yearMonth = YearMonth.of(budget.getYear(), budget.getMonth());
        LocalDate start = yearMonth.atDay(1);
        LocalDate end = yearMonth.atEndOfMonth();
        BigDecimal spent = transactionRepository.sumAmountByUserIdAndCategoryIdAndTypeAndTransactionDateBetween(
                budget.getUser().getId(),
                budget.getCategory().getId(),
                TransactionType.EXPENSE,
                start,
                end
        );

        BigDecimal utilization = budget.getAmount().compareTo(BigDecimal.ZERO) == 0
                ? BigDecimal.ZERO
                : spent.multiply(BigDecimal.valueOf(100)).divide(budget.getAmount(), 2, RoundingMode.HALF_UP);

        String alertLevel = utilization.compareTo(BigDecimal.valueOf(120)) >= 0 ? "120%"
                : utilization.compareTo(BigDecimal.valueOf(100)) >= 0 ? "100%"
                : utilization.compareTo(BigDecimal.valueOf(80)) >= 0 ? "80%"
                : "OK";

        return new BudgetResponse(
                budget.getId(),
                budget.getCategory().getId(),
                budget.getCategory().getName(),
                budget.getMonth(),
                budget.getYear(),
                budget.getAmount(),
                budget.getAlertThresholdPercent(),
                spent,
                utilization,
                alertLevel
        );
    }
}
