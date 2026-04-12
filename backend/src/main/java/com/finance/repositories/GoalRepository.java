package com.finance.repositories;

import com.finance.entities.Goal;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface GoalRepository extends JpaRepository<Goal, UUID> {
    List<Goal> findByUserIdOrderByTargetDateAsc(UUID userId);
    Optional<Goal> findByIdAndUserId(UUID id, UUID userId);
}
