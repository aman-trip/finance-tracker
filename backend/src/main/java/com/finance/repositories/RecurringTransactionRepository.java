package com.finance.repositories;

import com.finance.entities.RecurringTransaction;
import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface RecurringTransactionRepository extends JpaRepository<RecurringTransaction, UUID> {
    List<RecurringTransaction> findByUserIdOrderByNextRunDateAsc(UUID userId);
    Optional<RecurringTransaction> findByIdAndUserId(UUID id, UUID userId);
    List<RecurringTransaction> findByAutoCreateTransactionTrueAndNextRunDateLessThanEqual(LocalDate date);
}
