using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record TransactionRequest(
    [param: Required(ErrorMessage = "must not be null")]
    Guid? accountId,
    Guid? categoryId,
    [param: Required(ErrorMessage = "must not be null")]
    TransactionType? type,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? amount,
    [param: Required(ErrorMessage = "must not be null")]
    DateOnly? transactionDate,
    string? merchant,
    string? note,
    string? paymentMethod
);

public sealed record TransactionResponse(
    Guid id,
    Guid accountId,
    string accountName,
    Guid? categoryId,
    string? categoryName,
    TransactionType type,
    decimal amount,
    DateOnly transactionDate,
    string? merchant,
    string? note,
    string? paymentMethod,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt
);

public sealed record SortResponse(
    bool sorted,
    bool unsorted,
    bool empty
);

public sealed record PageableResponse(
    SortResponse sort,
    long offset,
    int pageNumber,
    int pageSize,
    bool paged,
    bool unpaged
);

public sealed record PageResponse<T>(
    IReadOnlyList<T> content,
    PageableResponse pageable,
    long totalElements,
    int totalPages,
    bool last,
    int size,
    int number,
    SortResponse sort,
    int numberOfElements,
    bool first,
    bool empty
);
