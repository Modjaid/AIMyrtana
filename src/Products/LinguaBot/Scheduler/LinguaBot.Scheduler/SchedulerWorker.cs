using LinguaBot.Data;
using LinguaBot.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LinguaBot.Scheduler;

/// <summary>
/// Background service that polls for due scheduled tasks every minute and
/// delivers them as Telegram messages.
/// </summary>
public sealed class SchedulerWorker(
    IServiceScopeFactory scopeFactory,
    ITelegramBotClient telegram,
    ILogger<SchedulerWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SchedulerWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in SchedulerWorker tick.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("SchedulerWorker stopped.");
    }

    private async Task ProcessDueTasksAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var messageBuilder = scope.ServiceProvider.GetRequiredService<TaskMessageBuilder>();

        var dueTasks = await repo.GetDueTasksAsync(DateTimeOffset.UtcNow, ct);
        if (dueTasks.Count == 0) return;

        logger.LogInformation("Processing {Count} due scheduled task(s).", dueTasks.Count);

        foreach (var task in dueTasks)
            await ProcessTaskAsync(task, repo, messageBuilder, ct);
    }

    private async Task ProcessTaskAsync(
        ScheduledTask task,
        IScheduledTaskRepository repo,
        TaskMessageBuilder messageBuilder,
        CancellationToken ct)
    {
        try
        {
            var text = messageBuilder.Build(task);
            await telegram.SendMessage(
                task.TelegramUserId,
                text,
                parseMode: ParseMode.None,
                cancellationToken: ct);

            if (task.IsRecurring && task.CronExpression is not null)
            {
                // Re-arm: keep Status = Pending, advance ScheduledAt to next occurrence.
                var nextRun = CronHelper.GetNextOccurrence(task.CronExpression);
                task.ScheduledAt = nextRun;
                task.NextRunAt = nextRun;
                task.SentAt = DateTimeOffset.UtcNow;
            }
            else
            {
                task.Status = ScheduledTaskStatus.Sent;
                task.SentAt = DateTimeOffset.UtcNow;
            }

            logger.LogInformation(
                "Sent task {TaskId} ({Type}) to TelegramUser {TelegramUserId}. Recurring={IsRecurring}",
                task.Id, task.Type, task.TelegramUserId, task.IsRecurring);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to deliver task {TaskId} to TelegramUser {TelegramUserId}.",
                task.Id, task.TelegramUserId);

            task.Status = ScheduledTaskStatus.Failed;
        }

        await repo.UpdateAsync(task, ct);
    }
}
