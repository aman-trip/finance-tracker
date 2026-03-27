using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class GoalService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccountService accountService,
    LedgerService ledgerService)
{
    public async Task<IReadOnlyList<GoalResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var goals = await dbContext.Goals
            .AsNoTracking()
            .Where(goal => goal.UserId == currentUserService.GetCurrentUserId())
            .OrderBy(goal => goal.TargetDate)
            .ToListAsync(cancellationToken);

        return goals.Select(ToResponse).ToList();
    }

    public async Task<GoalResponse> CreateAsync(GoalRequest request, CancellationToken cancellationToken)
    {
        var goal = new Goal
        {
            UserId = currentUserService.GetCurrentUserId(),
            Name = request.name!,
            TargetAmount = request.targetAmount!.Value,
            CurrentAmount = 0m,
            TargetDate = request.targetDate,
            Status = request.status ?? GoalStatus.ACTIVE
        };

        dbContext.Goals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(goal);
    }

    public async Task<GoalResponse> UpdateAsync(Guid id, GoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await GetGoalAsync(id, currentUserService.GetCurrentUserId(), cancellationToken);
        goal.Name = request.name!;
        goal.TargetAmount = request.targetAmount!.Value;
        goal.TargetDate = request.targetDate;
        goal.Status = request.status ?? goal.Status;
        RecalculateStatus(goal);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(goal);
    }

    public async Task<GoalResponse> ContributeAsync(Guid id, GoalContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var goal = await GetGoalAsync(id, userId, cancellationToken);
        var account = await accountService.GetAccountAsync(request.accountId!.Value, userId, cancellationToken);

        if (account.CurrentBalance < request.amount!.Value)
        {
            throw new BadRequestException("Insufficient account balance");
        }

        var user = await dbContext.Users.FirstAsync(candidate => candidate.Id == userId, cancellationToken);
        goal.CurrentAmount += request.amount.Value;
        RecalculateStatus(goal);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        ledgerService.CreateTransaction(
            user,
            account,
            null,
            TransactionType.TRANSFER_OUT,
            request.amount.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            goal.Name,
            $"Contribution to goal: {goal.Name}",
            "GOAL",
            Guid.NewGuid()
        );

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(goal);
    }

    public async Task<GoalResponse> WithdrawAsync(Guid id, GoalContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var goal = await GetGoalAsync(id, userId, cancellationToken);
        if (goal.CurrentAmount < request.amount!.Value)
        {
            throw new BadRequestException("Insufficient goal balance");
        }

        var account = await accountService.GetAccountAsync(request.accountId!.Value, userId, cancellationToken);
        var user = await dbContext.Users.FirstAsync(candidate => candidate.Id == userId, cancellationToken);

        goal.CurrentAmount -= request.amount.Value;
        RecalculateStatus(goal);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        ledgerService.CreateTransaction(
            user,
            account,
            null,
            TransactionType.TRANSFER_IN,
            request.amount.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            goal.Name,
            $"Withdrawal from goal: {goal.Name}",
            "GOAL",
            Guid.NewGuid()
        );

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(goal);
    }

    public async Task<Goal> GetGoalAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Goals.FirstOrDefaultAsync(goal => goal.Id == id && goal.UserId == userId, cancellationToken)
               ?? throw new NotFoundException("Goal not found");
    }

    private static void RecalculateStatus(Goal goal)
    {
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.Status = GoalStatus.COMPLETED;
        }
        else if (goal.Status == GoalStatus.COMPLETED)
        {
            goal.Status = GoalStatus.ACTIVE;
        }
    }

    private static GoalResponse ToResponse(Goal goal)
    {
        var progressPercent = goal.TargetAmount == 0
            ? 0m
            : Math.Round(goal.CurrentAmount * 100m / goal.TargetAmount, 2, MidpointRounding.AwayFromZero);

        return new GoalResponse(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            goal.TargetDate,
            goal.Status,
            progressPercent
        );
    }
}
