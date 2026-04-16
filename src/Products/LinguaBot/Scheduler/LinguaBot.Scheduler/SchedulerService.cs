using LinguaBot.Data;
using LinguaBot.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaBot.Scheduler;

/// <summary>
/// Singleton-safe implementation of <see cref="ISchedulerService"/>.
/// Resolves <see cref="IScheduledTaskRepository"/> through a fresh scope per call
/// to avoid capturing a scoped dependency in a singleton.
/// </summary>
public sealed class SchedulerService(IServiceScopeFactory scopeFactory) : ISchedulerService
{
    public async Task ScheduleOnceAsync(
        Guid userId,
        long telegramUserId,
        ScheduledTaskType type,
        DateTimeOffset scheduledAt,
        string? payload = null,
        CancellationToken ct = default)
    {
        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TelegramUserId = telegramUserId,
            Type = type,
            Status = ScheduledTaskStatus.Pending,
            ScheduledAt = scheduledAt,
            IsRecurring = false,
            Payload = payload,
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        await repo.AddAsync(task, ct);
    }

    public async Task ScheduleRecurringAsync(
        Guid userId,
        long telegramUserId,
        ScheduledTaskType type,
        string cronExpression,
        string? payload = null,
        CancellationToken ct = default)
    {
        var nextRun = CronHelper.GetNextOccurrence(cronExpression);

        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TelegramUserId = telegramUserId,
            Type = type,
            Status = ScheduledTaskStatus.Pending,
            ScheduledAt = nextRun,
            IsRecurring = true,
            CronExpression = cronExpression,
            NextRunAt = nextRun,
            Payload = payload,
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        await repo.AddAsync(task, ct);
    }

    public async Task ScheduleReviewAsync(
        Guid userId,
        long telegramUserId,
        Guid itemId,
        ReviewItemKind itemKind,
        DateTimeOffset scheduledAt,
        CancellationToken ct = default)
    {
        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TelegramUserId = telegramUserId,
            Type = itemKind == ReviewItemKind.Word ? ScheduledTaskType.WordReview : ScheduledTaskType.PhraseReview,
            Status = ScheduledTaskStatus.Pending,
            ScheduledAt = scheduledAt,
            IsRecurring = false,
            ReviewItemId = itemId,
            ReviewItemKind = itemKind,
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        await repo.AddAsync(task, ct);
    }

    public async Task CancelAsync(Guid taskId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();

        var task = await repo.GetByIdAsync(taskId, ct);
        if (task is null) return;

        task.Status = ScheduledTaskStatus.Cancelled;
        await repo.UpdateAsync(task, ct);
    }

    public async Task<List<ScheduledTask>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        return await repo.GetByUserIdAsync(userId, ct);
    }
}
