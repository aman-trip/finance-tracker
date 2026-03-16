package com.finance.controllers;

import com.finance.dto.GoalDtos.GoalContributionRequest;
import com.finance.dto.GoalDtos.GoalRequest;
import com.finance.dto.GoalDtos.GoalResponse;
import com.finance.services.GoalService;
import jakarta.validation.Valid;
import java.util.List;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/goals")
@RequiredArgsConstructor
public class GoalController {

    private final GoalService goalService;

    @GetMapping
    public List<GoalResponse> getAll() {
        return goalService.getAll();
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public GoalResponse create(@Valid @RequestBody GoalRequest request) {
        return goalService.create(request);
    }

    @PutMapping("/{id}")
    public GoalResponse update(@PathVariable UUID id, @Valid @RequestBody GoalRequest request) {
        return goalService.update(id, request);
    }

    @PostMapping("/{id}/contribute")
    public GoalResponse contribute(@PathVariable UUID id, @Valid @RequestBody GoalContributionRequest request) {
        return goalService.contribute(id, request);
    }

    @PostMapping("/{id}/withdraw")
    public GoalResponse withdraw(@PathVariable UUID id, @Valid @RequestBody GoalContributionRequest request) {
        return goalService.withdraw(id, request);
    }
}
