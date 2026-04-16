namespace LinguaBot.Domain;

public class ScheduledTask
{
    public Guid Id { get; set; }

    /// <summary>FK to <see cref="User.Id"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Denormalised for fast outbound delivery without an extra join.</summary>
    public long TelegramUserId { get; set; }

    public ScheduledTaskType Type { get; set; }

    public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.Pending;

    /// <summary>When the notification should be (or was last) sent.</summary>
    public DateTimeOffset ScheduledAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    /// <summary>True for cron-based recurring tasks; false for one-off reminders.</summary>
    public bool IsRecurring { get; set; }

    /// <summary>Standard 5-field cron expression (UTC), e.g. "0 9 * * *". Null for one-off tasks.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Pre-computed next fire time, kept in sync after each delivery.</summary>
    public DateTimeOffset? NextRunAt { get; set; }

    /// <summary>Free-form JSON payload; used for the Custom task type.</summary>
    public string? Payload { get; set; }

    /// <summary>
    /// The <see cref="LearningWord.Id"/> or <see cref="LearningPhrase.Id"/> this review task is for.
    /// Null for non-review task types (e.g. DailyLesson, Custom).
    /// </summary>
    public Guid? ReviewItemId { get; set; }

    /// <summary>Whether the review item is a word or a phrase. Null for non-review tasks.</summary>
    public ReviewItemKind? ReviewItemKind { get; set; }
}

public enum ReviewItemKind
{
    Word = 0,
    Phrase = 1,
}

public enum ScheduledTaskType
{
    WordReview = 0,
    PhraseReview = 1,
    DailyLesson = 2,
    Custom = 3,
}

public enum ScheduledTaskStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Cancelled = 3,
}

/// <summary>
/// Service for scheduling task-based notifications to users.
/// Implementations are expected to be singleton-safe.
/// </summary>
public interface ISchedulerService
{
    /// <summary>Schedules a one-time notification.</summary>
    Task ScheduleOnceAsync(
        Guid userId,
        long telegramUserId,
        ScheduledTaskType type,
        DateTimeOffset scheduledAt,
        string? payload = null,
        CancellationToken ct = default);

    /// <summary>Schedules a recurring notification using a standard UTC cron expression.</summary>
    Task ScheduleRecurringAsync(
        Guid userId,
        long telegramUserId,
        ScheduledTaskType type,
        string cronExpression,
        string? payload = null,
        CancellationToken ct = default);

    /// <summary>
    /// Schedules a one-time word/phrase review notification.
    /// Sets <see cref="ScheduledTask.ReviewItemId"/> and <see cref="ScheduledTask.ReviewItemKind"/> automatically.
    /// </summary>
    Task ScheduleReviewAsync(
        Guid userId,
        long telegramUserId,
        Guid itemId,
        ReviewItemKind itemKind,
        DateTimeOffset scheduledAt,
        CancellationToken ct = default);

    /// <summary>Cancels a previously created scheduled task.</summary>
    Task CancelAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Returns all non-cancelled tasks for the given user, ordered by next fire time.</summary>
    Task<List<ScheduledTask>> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
