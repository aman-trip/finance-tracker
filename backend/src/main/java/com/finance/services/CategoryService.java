package com.finance.services;

import com.finance.dto.CategoryDtos.CategoryRequest;
import com.finance.dto.CategoryDtos.CategoryResponse;
import com.finance.entities.Category;
import com.finance.entities.CategoryType;
import com.finance.entities.User;
import com.finance.exception.BadRequestException;
import com.finance.exception.NotFoundException;
import com.finance.repositories.CategoryRepository;
import com.finance.repositories.UserRepository;
import com.finance.security.CurrentUserService;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class CategoryService {

    private final CategoryRepository categoryRepository;
    private final UserRepository userRepository;
    private final CurrentUserService currentUserService;

    @Transactional(readOnly = true)
    public List<CategoryResponse> getAll() {
        return categoryRepository.findByUserIdAndArchivedFalseOrderByNameAsc(currentUserService.getCurrentUserId())
                .stream()
                .map(this::toResponse)
                .toList();
    }

    @Transactional
    public CategoryResponse create(CategoryRequest request) {
        UUID userId = currentUserService.getCurrentUserId();
        if (categoryRepository.existsByUserIdAndNameIgnoreCaseAndType(userId, request.name(), request.type())) {
            throw new BadRequestException("Category already exists");
        }

        Category category = new Category();
        category.setUser(getUser(userId));
        map(category, request);
        category.setArchived(false);
        return toResponse(categoryRepository.save(category));
    }

    @Transactional
    public CategoryResponse update(UUID id, CategoryRequest request) {
        Category category = getCategory(id, currentUserService.getCurrentUserId());
        map(category, request);
        return toResponse(categoryRepository.save(category));
    }

    @Transactional
    public void delete(UUID id) {
        Category category = getCategory(id, currentUserService.getCurrentUserId());
        category.setArchived(true);
        categoryRepository.save(category);
    }

    @Transactional
    public void createDefaultCategories(User user) {
        if (!categoryRepository.findByUserIdAndArchivedFalseOrderByNameAsc(user.getId()).isEmpty()) {
            return;
        }

        List<Category> categories = new ArrayList<>();
        defaultExpenseCategories().forEach((name, meta) -> categories.add(buildDefaultCategory(user, name, CategoryType.EXPENSE, meta[0], meta[1])));
        defaultIncomeCategories().forEach((name, meta) -> categories.add(buildDefaultCategory(user, name, CategoryType.INCOME, meta[0], meta[1])));
        categoryRepository.saveAll(categories);
    }

    @Transactional(readOnly = true)
    public Category getCategory(UUID id, UUID userId) {
        return categoryRepository.findByIdAndUserId(id, userId)
                .orElseThrow(() -> new NotFoundException("Category not found"));
    }

    private User getUser(UUID userId) {
        return userRepository.findById(userId).orElseThrow(() -> new NotFoundException("User not found"));
    }

    private void map(Category category, CategoryRequest request) {
        category.setName(request.name());
        category.setType(request.type());
        category.setColor(request.color());
        category.setIcon(request.icon());
    }

    private Category buildDefaultCategory(User user, String name, CategoryType type, String color, String icon) {
        Category category = new Category();
        category.setUser(user);
        category.setName(name);
        category.setType(type);
        category.setColor(color);
        category.setIcon(icon);
        category.setArchived(false);
        return category;
    }

    private Map<String, String[]> defaultExpenseCategories() {
        return Map.ofEntries(
                Map.entry("Food", new String[]{"#EF4444", "utensils"}),
                Map.entry("Rent", new String[]{"#F97316", "house"}),
                Map.entry("Utilities", new String[]{"#EAB308", "bolt"}),
                Map.entry("Transport", new String[]{"#14B8A6", "car"}),
                Map.entry("Entertainment", new String[]{"#8B5CF6", "film"}),
                Map.entry("Shopping", new String[]{"#EC4899", "shopping-bag"}),
                Map.entry("Health", new String[]{"#10B981", "heart-pulse"}),
                Map.entry("Education", new String[]{"#3B82F6", "graduation-cap"}),
                Map.entry("Travel", new String[]{"#06B6D4", "plane"}),
                Map.entry("Subscriptions", new String[]{"#6366F1", "repeat"}),
                Map.entry("Misc", new String[]{"#6B7280", "circle"})
        );
    }

    private Map<String, String[]> defaultIncomeCategories() {
        return Map.ofEntries(
                Map.entry("Salary", new String[]{"#16A34A", "briefcase"}),
                Map.entry("Freelance", new String[]{"#0EA5E9", "laptop"}),
                Map.entry("Bonus", new String[]{"#84CC16", "sparkles"}),
                Map.entry("Investment", new String[]{"#22C55E", "chart-line"}),
                Map.entry("Gift", new String[]{"#F59E0B", "gift"}),
                Map.entry("Refund", new String[]{"#06B6D4", "rotate-ccw"}),
                Map.entry("Other", new String[]{"#64748B", "plus-circle"})
        );
    }

    private CategoryResponse toResponse(Category category) {
        return new CategoryResponse(
                category.getId(),
                category.getName(),
                category.getType(),
                category.getColor(),
                category.getIcon(),
                category.isArchived()
        );
    }
}
