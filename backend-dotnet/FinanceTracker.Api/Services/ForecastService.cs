using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class ForecastService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer)
{
    private const int LookbackDays = 180;
    private const int DailyForecastHorizon = 30;

    public async Task<ForecastMonthResponse> GetMonthForecastAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var horizonDays = Math.Max(1, monthEnd.DayNumber - today.DayNumber + 1);
        var model = await BuildForecastModelAsync(today, monthEnd, horizonDays, cancellationToken);

        return new ForecastMonthResponse(
            model.CurrentBalance,
            model.ForecastBalance,
            model.SafeToSpend,
            model.AverageDailySpend,
            model.RecurringExpenses,
            model.Risk,
            horizonDays
        );
    }

    public async Task<ForecastDailyResponse> GetDailyForecastAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizonEnd = today.AddDays(DailyForecastHorizon - 1);
        var model = await BuildForecastModelAsync(today, horizonEnd, DailyForecastHorizon, cancellationToken);
        var recurringByDate = await LoadRecurringAdjustmentsAsync(today, horizonEnd, cancellationToken);

        var runningBalance = model.CurrentBalance;
        var points = new List<ForecastDailyPointResponse>(DailyForecastHorizon);

        for (var offset = 0; offset < DailyForecastHorizon; offset++)
        {
            var date = today.AddDays(offset);
            var recurringImpact = recurringByDate.TryGetValue(date, out var value) ? value : 0m;
            runningBalance += recurringImpact - model.AverageDailySpend;

            var daysRemaining = Math.Max(1, DailyForecastHorizon - offset);
            var safeToSpend = CalculateSafeToSpend(
                runningBalance,
                daysRemaining,
                model.AverageDailySpend,
                recurringByDate
                    .Where(item => item.Key > date)
                    .Sum(item => item.Value));

            points.Add(new ForecastDailyPointResponse(
                date,
                Round(runningBalance),
                safeToSpend,
                ClassifyRisk(runningBalance, safeToSpend, model.AverageDailySpend)));
        }

        return new ForecastDailyResponse(
            points,
            model.ForecastBalance,
            model.SafeToSpend,
            model.Risk
        );
    }

    private async Task<ForecastModel> BuildForecastModelAsync(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int horizonDays,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var accountIds = await accessControlLayer.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        if (accountIds.Count == 0)
        {
            return new ForecastModel(0m, 0m, 0m, 0m, 0m, "LOW");
        }

        var currentBalance = Round(await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .Select(account => (decimal?)account.CurrentBalance)
            .SumAsync(cancellationToken) ?? 0m);

        var averageDailySpend = await CalculateAverageDailySpendAsync(accountIds, rangeStart, cancellationToken);
        var recurringAdjustments = await LoadRecurringAdjustmentsAsync(rangeStart, rangeEnd, cancellationToken);
        var recurringExpenses = Round(Math.Abs(recurringAdjustments.Values.Where(value => value < 0m).Sum()));
        var projectedVariableExpenses = Round(averageDailySpend * horizonDays);
        var forecastBalance = Round(currentBalance + recurringAdjustments.Values.Sum() - projectedVariableExpenses);
        var safeToSpend = CalculateSafeToSpend(currentBalance, horizonDays, averageDailySpend, recurringAdjustments.Values.Sum());
        var risk = ClassifyRisk(forecastBalance, safeToSpend, averageDailySpend);

        return new ForecastModel(
            currentBalance,
            forecastBalance,
            safeToSpend,
            averageDailySpend,
            recurringExpenses,
            risk);
    }

    private async Task<decimal> CalculateAverageDailySpendAsync(
        IReadOnlyList<Guid> accountIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var lookbackStart = today.AddDays(-(LookbackDays - 1));
        var expenseTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.Type == TransactionType.EXPENSE
                                  && transaction.TransactionDate >= lookbackStart
                                  && transaction.TransactionDate <= today)
            .Select(transaction => new { transaction.Amount, transaction.TransactionDate })
            .ToListAsync(cancellationToken);

        if (expenseTransactions.Count == 0)
        {
            return 0m;
        }

        var earliest = expenseTransactions.Min(item => item.TransactionDate);
        var divisor = Math.Max(30, today.DayNumber - earliest.DayNumber + 1);
        var totalExpense = expenseTransactions.Sum(item => item.Amount);
        return Round(totalExpense / divisor);
    }

    private async Task<Dictionary<DateOnly, decimal>> LoadRecurringAdjustmentsAsync(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var accountIds = await accessControlLayer.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        if (accountIds.Count == 0)
        {
            return new Dictionary<DateOnly, decimal>();
        }

        var recurringTransactions = await dbContext.RecurringTransactions
            .AsNoTracking()
            .Where(recurring => accountIds.Contains(recurring.AccountId) && recurring.NextRunDate <= rangeEnd)
            .OrderBy(recurring => recurring.NextRunDate)
            .ToListAsync(cancellationToken);

        var adjustments = new Dictionary<DateOnly, decimal>();
        foreach (var recurring in recurringTransactions)
        {
            var runDate = recurring.NextRunDate;
            var effectiveEnd = recurring.EndDate is null || recurring.EndDate > rangeEnd
                ? rangeEnd
                : recurring.EndDate.Value;

            while (runDate <= effectiveEnd)
            {
                if (runDate >= rangeStart)
                {
                    var signedAmount = recurring.Type == TransactionType.INCOME ? recurring.Amount : -recurring.Amount;
                    adjustments[runDate] = adjustments.TryGetValue(runDate, out var current)
                        ? current + signedAmount
                        : signedAmount;
                }

                runDate = recurring.Frequency switch
                {
                    RecurringFrequency.DAILY => runDate.AddDays(1),
                    RecurringFrequency.WEEKLY => runDate.AddDays(7),
                    RecurringFrequency.MONTHLY => runDate.AddMonths(1),
                    RecurringFrequency.YEARLY => runDate.AddYears(1),
                    _ => runDate
                };
            }
        }

        return adjustments;
    }

    private static decimal CalculateSafeToSpend(
        decimal currentBalance,
        int daysRemaining,
        decimal averageDailySpend,
        decimal recurringNet)
    {
        var reserve = Math.Max(averageDailySpend * 7m, 100m);
        var available = currentBalance + recurringNet - reserve;
        return Round(Math.Max(0m, available / Math.Max(1, daysRemaining)));
    }

    private static string ClassifyRisk(decimal forecastBalance, decimal safeToSpend, decimal averageDailySpend)
    {
        if (forecastBalance < 0m || safeToSpend <= 0m)
        {
            return "HIGH";
        }

        if (forecastBalance < averageDailySpend * 14m || safeToSpend < averageDailySpend * 0.75m)
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ForecastModel(
        decimal CurrentBalance,
        decimal ForecastBalance,
        decimal SafeToSpend,
        decimal AverageDailySpend,
        decimal RecurringExpenses,
        string Risk
    );
}
