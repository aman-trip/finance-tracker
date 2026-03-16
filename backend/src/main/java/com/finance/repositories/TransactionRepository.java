package com.finance.repositories;

import com.finance.entities.Transaction;
import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

public interface TransactionRepository extends JpaRepository<Transaction, UUID>, JpaSpecificationExecutor<Transaction> {
    Optional<Transaction> findByIdAndUserId(UUID id, UUID userId);
    List<Transaction> findByUserIdAndTransactionDateBetween(UUID userId, LocalDate startDate, LocalDate endDate);
    List<Transaction> findByUserIdAndAccountIdOrderByTransactionDateAsc(UUID userId, UUID accountId);
}
