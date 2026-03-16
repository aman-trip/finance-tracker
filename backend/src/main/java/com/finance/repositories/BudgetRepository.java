package com.finance.repositories;

import com.finance.entities.Budget;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface BudgetRepository extends JpaRepository<Budget, UUID> {
    List<Budget> findByUserIdOrderByYearDescMonthDesc(UUID userId);
    Optional<Budget> findByIdAndUserId(UUID id, UUID userId);
    boolean existsByUserIdAndCategoryIdAndMonthAndYear(UUID userId, UUID categoryId, Integer month, Integer year);
}
