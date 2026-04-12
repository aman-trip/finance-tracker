package com.finance.repositories;

import com.finance.entities.Transaction;
import com.finance.entities.TransactionType;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface TransactionRepository extends JpaRepository<Transaction, UUID>, JpaSpecificationExecutor<Transaction> {
    Optional<Transaction> findByIdAndUserId(UUID id, UUID userId);
    List<Transaction> findByUserIdAndTransactionDateBetween(UUID userId, LocalDate startDate, LocalDate endDate);
    List<Transaction> findByUserIdAndAccountIdOrderByTransactionDateAsc(UUID userId, UUID accountId);

    @Query("""
            select coalesce(sum(t.amount), 0)
            from Transaction t
            where t.user.id = :userId
              and t.category.id = :categoryId
              and t.type = :type
              and t.transactionDate between :startDate and :endDate
            """)
    BigDecimal sumAmountByUserIdAndCategoryIdAndTypeAndTransactionDateBetween(
            @Param("userId") UUID userId,
            @Param("categoryId") UUID categoryId,
            @Param("type") TransactionType type,
            @Param("startDate") LocalDate startDate,
            @Param("endDate") LocalDate endDate
    );
}
