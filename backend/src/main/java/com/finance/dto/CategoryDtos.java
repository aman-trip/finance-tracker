package com.finance.dto;

import com.finance.entities.CategoryType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import java.util.UUID;

public class CategoryDtos {

    public record CategoryRequest(
            @NotBlank String name,
            @NotNull CategoryType type,
            @NotBlank String color,
            @NotBlank String icon
    ) {
    }

    public record CategoryResponse(
            UUID id,
            String name,
            CategoryType type,
            String color,
            String icon,
            boolean archived
    ) {
    }
}
