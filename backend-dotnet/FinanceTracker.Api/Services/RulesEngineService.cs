using System.Text.Json;
using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class RulesEngineService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    ILogger<RulesEngineService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IReadOnlyList<RuleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var rules = await dbContext.Rules
            .AsNoTracking()
            .Where(rule => rule.UserId == userId)
            .OrderByDescending(rule => rule.UpdatedAt)
            .ThenByDescending(rule => rule.CreatedAt)
            .ToListAsync(cancellationToken);

        return rules.Select(ToResponse).ToList();
    }

    public async Task<RuleResponse> CreateAsync(RuleRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var normalized = await NormalizeAsync(request);

        var rule = new Rule
        {
            UserId = userId,
            Name = normalized.Name,
            ConditionJson = normalized.ConditionJson,
            ActionJson = normalized.ActionJson,
            IsActive = request.isActive
        };

        dbContext.Rules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(rule);
    }

    public async Task<RuleResponse> UpdateAsync(Guid id, RuleRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var normalized = await NormalizeAsync(request);

        var rule = await GetRuleAsync(id, userId, cancellationToken);
        rule.Name = normalized.Name;
        rule.ConditionJson = normalized.ConditionJson;
        rule.ActionJson = normalized.ActionJson;
        rule.IsActive = request.isActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(rule);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await GetRuleAsync(id, currentUserService.GetCurrentUserId(), cancellationToken);
        dbContext.Rules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransactionRuleDraft> ApplyAsync(TransactionRuleDraft draft, CancellationToken cancellationToken)
    {
        var rules = await dbContext.Rules
            .AsNoTracking()
            .Where(rule => rule.UserId == draft.OwnerUserId && rule.IsActive)
            .OrderBy(rule => rule.CreatedAt)
            .ToListAsync(cancellationToken);

        var current = draft;
        foreach (var rule in rules)
        {
            try
            {
                var condition = DeserializeCondition(rule.ConditionJson);
                var action = DeserializeAction(rule.ActionJson);

                if (!Matches(condition, current))
                {
                    continue;
                }

                current = ApplyAction(current, action);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Skipping invalid rule payload for rule {RuleId}", rule.Id);
            }
        }

        return current;
    }

    private Task<NormalizedRulePayload> NormalizeAsync(RuleRequest request)
    {
        var name = request.name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Rule name must not be blank");
        }

        var conditionJson = request.conditionJson?.Trim();
        var actionJson = request.actionJson?.Trim();

        try
        {
            logger.LogInformation("Received rule condition JSON: {Json}", conditionJson);
            logger.LogInformation("Received rule action JSON: {Json}", actionJson);

            using var conditionDocument = JsonDocument.Parse(conditionJson ?? "{}");
            using var actionDocument = JsonDocument.Parse(actionJson ?? "{}");
        }
        catch (JsonException)
        {
            throw new BadRequestException("Rule JSON must be valid");
        }

        return Task.FromResult(new NormalizedRulePayload(
            name,
            conditionJson ?? "{}",
            actionJson ?? "{}"));
    }

    private async Task<Rule> GetRuleAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Rules
            .FirstOrDefaultAsync(rule => rule.Id == id && rule.UserId == userId, cancellationToken)
               ?? throw new NotFoundException("Rule not found");
    }

    private static RuleCondition DeserializeCondition(string? rawJson)
    {
        return JsonSerializer.Deserialize<RuleCondition>(rawJson ?? "{}", JsonOptions) ?? new RuleCondition();
    }

    private static RuleAction DeserializeAction(string? rawJson)
    {
        return JsonSerializer.Deserialize<RuleAction>(rawJson ?? "{}", JsonOptions) ?? new RuleAction();
    }

    private static bool Matches(RuleCondition condition, TransactionRuleDraft draft)
    {
        if (condition.AccountId.HasValue && condition.AccountId.Value != draft.AccountId)
        {
            return false;
        }

        if (condition.TypeEquals.HasValue && condition.TypeEquals.Value != draft.Type)
        {
            return false;
        }

        if (condition.MinAmount.HasValue && draft.Amount < condition.MinAmount.Value)
        {
            return false;
        }

        if (condition.MaxAmount.HasValue && draft.Amount > condition.MaxAmount.Value)
        {
            return false;
        }

        if (!ContainsIgnoreCase(draft.Merchant, condition.MerchantContains))
        {
            return false;
        }

        if (!ContainsIgnoreCase(draft.Note, condition.NoteContains))
        {
            return false;
        }

        if (!ContainsIgnoreCase(draft.PaymentMethod, condition.PaymentMethodEquals, exact: true))
        {
            return false;
        }

        return true;
    }

    private static TransactionRuleDraft ApplyAction(TransactionRuleDraft draft, RuleAction action)
    {
        var note = draft.Note;
        if (!string.IsNullOrWhiteSpace(action.PrependNote))
        {
            note = string.IsNullOrWhiteSpace(note)
                ? action.PrependNote.Trim()
                : $"{action.PrependNote.Trim()} {note}";
        }

        if (!string.IsNullOrWhiteSpace(action.AppendNote))
        {
            note = string.IsNullOrWhiteSpace(note)
                ? action.AppendNote.Trim()
                : $"{note} {action.AppendNote.Trim()}";
        }

        return draft with
        {
            CategoryId = action.SetCategoryId ?? draft.CategoryId,
            PaymentMethod = string.IsNullOrWhiteSpace(action.SetPaymentMethod)
                ? draft.PaymentMethod
                : action.SetPaymentMethod.Trim(),
            Note = note
        };
    }

    private static bool ContainsIgnoreCase(string? candidate, string? expected, bool exact = false)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return exact
            ? string.Equals(candidate.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase)
            : candidate.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static RuleResponse ToResponse(Rule rule)
    {
        return new RuleResponse(
            rule.Id,
            rule.Name,
            rule.ConditionJson,
            rule.ActionJson,
            rule.IsActive,
            rule.CreatedAt,
            rule.UpdatedAt
        );
    }

    private sealed record NormalizedRulePayload(
        string Name,
        string ConditionJson,
        string ActionJson
    );

    private sealed record RuleCondition(
        Guid? AccountId = null,
        TransactionType? TypeEquals = null,
        string? MerchantContains = null,
        string? NoteContains = null,
        decimal? MinAmount = null,
        decimal? MaxAmount = null,
        string? PaymentMethodEquals = null
    );

    private sealed record RuleAction(
        Guid? SetCategoryId = null,
        string? SetPaymentMethod = null,
        string? PrependNote = null,
        string? AppendNote = null
    );
}

public sealed record TransactionRuleDraft(
    Guid OwnerUserId,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    DateOnly TransactionDate,
    string? Merchant,
    string? Note,
    string? PaymentMethod,
    Guid? CategoryId
);
