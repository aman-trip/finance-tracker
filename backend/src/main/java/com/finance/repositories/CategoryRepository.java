package com.finance.repositories;

import com.finance.entities.Category;
import com.finance.entities.CategoryType;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface CategoryRepository extends JpaRepository<Category, UUID> {
    List<Category> findByUserIdAndArchivedFalseOrderByNameAsc(UUID userId);
    Optional<Category> findByIdAndUserId(UUID id, UUID userId);
    boolean existsByUserIdAndNameIgnoreCaseAndType(UUID userId, String name, CategoryType type);
}
