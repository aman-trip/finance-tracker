package com.finance.controllers;

import com.finance.dto.RecurringDtos.RecurringTransactionRequest;
import com.finance.dto.RecurringDtos.RecurringTransactionResponse;
import com.finance.services.RecurringTransactionService;
import jakarta.validation.Valid;
import java.util.List;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/recurring")
@RequiredArgsConstructor
public class RecurringTransactionController {

    private final RecurringTransactionService recurringTransactionService;

    @GetMapping
    public List<RecurringTransactionResponse> getAll() {
        return recurringTransactionService.getAll();
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public RecurringTransactionResponse create(@Valid @RequestBody RecurringTransactionRequest request) {
        return recurringTransactionService.create(request);
    }

    @PutMapping("/{id}")
    public RecurringTransactionResponse update(@PathVariable UUID id, @Valid @RequestBody RecurringTransactionRequest request) {
        return recurringTransactionService.update(id, request);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable UUID id) {
        recurringTransactionService.delete(id);
    }
}
