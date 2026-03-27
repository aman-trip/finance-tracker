namespace FinanceTracker.Api.Services;

public sealed class RecurringTransactionHostedService(
    IServiceProvider serviceProvider,
    ILogger<RecurringTransactionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var delay = DelayUntilNextHourUtc();
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            do
            {
                await RunSchedulerAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var recurringTransactionService = scope.ServiceProvider.GetRequiredService<RecurringTransactionService>();
            logger.LogInformation("Running recurring transaction scheduler");
            await recurringTransactionService.ProcessDueTransactionsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Recurring transaction scheduler run failed");
        }
    }

    private static TimeSpan DelayUntilNextHourUtc()
    {
        var now = DateTimeOffset.UtcNow;
        var nextHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).AddHours(1);
        return nextHour - now;
    }
}
