using LinguaBot.Data;
using LinguaBot.Domain;

namespace LinguaBot.Scheduler;

/// <summary>
/// Handles the completion of a word/phrase review task.
/// Increments <see cref="LearningWord.CompletedTasksAtCurrentLevel"/> /
/// <see cref="LearningPhrase.CompletedTasksAtCurrentLevel"/> and,
/// once the threshold (<see cref="MemoryLevelIntervals.TasksRequiredToLevelUp"/>) is reached,
/// advances the memory level (0→1→2→3). After completing the required tasks
/// at <see cref="MemoryLevelIntervals.MaxLevel"/>, the item is marked
/// <see cref="LearningStatus.Learned"/> and no further reviews are scheduled.
/// </summary>
public interface ISpacedRepetitionService
{
    /// <summary>
    /// Marks one review task as completed for the given item.
    /// Persists the updated user and schedules the next review automatically
    /// (unless the item just reached <see cref="LearningStatus.Learned"/>).
    /// </summary>
    Task CompleteReviewAsync(
        Guid userId,
        long telegramUserId,
        Guid itemId,
        ReviewItemKind itemKind,
        CancellationToken ct = default);
}

public sealed class SpacedRepetitionService(
    IUserRepository userRepo,
    ISchedulerService scheduler) : ISpacedRepetitionService
{
    public async Task CompleteReviewAsync(
        Guid userId,
        long telegramUserId,
        Guid itemId,
        ReviewItemKind itemKind,
        CancellationToken ct = default)
    {
        var user = await userRepo.FindByTelegramUserIdAsync(telegramUserId, ct);
        if (user is null) return;

        var result = itemKind == ReviewItemKind.Word
            ? AdvanceWord(user, itemId)
            : AdvancePhrase(user, itemId);

        if (!result.HasValue) return; // item not found

        await userRepo.SaveAsync(user, ct);

        // Stop scheduling once the item is fully learned.
        if (result.Value.IsLearned) return;

        var daysUntilNext = MemoryLevelIntervals.GetRepeatAfterDays(result.Value.MemoryLevel);
        var nextAt = DateTimeOffset.UtcNow.AddDays(daysUntilNext);
        await scheduler.ScheduleReviewAsync(userId, telegramUserId, itemId, itemKind, nextAt, ct);
    }

    private static AdvanceResult? AdvanceWord(User user, Guid itemId)
    {
        var word = user.LanguageLearning.Words.FirstOrDefault(w => w.Id == itemId);
        if (word is null) return null;

        word.RepeatCount++;
        word.CompletedTasksAtCurrentLevel++;

        if (word.CompletedTasksAtCurrentLevel < MemoryLevelIntervals.TasksRequiredToLevelUp)
            return new(word.MemoryLevel, IsLearned: false);

        // Threshold reached.
        word.CompletedTasksAtCurrentLevel = 0;

        if (word.MemoryLevel < MemoryLevelIntervals.MaxLevel)
        {
            word.MemoryLevel++;
            return new(word.MemoryLevel, IsLearned: false);
        }

        // Already at MaxLevel — mark as Learned.
        word.Status = LearningStatus.Learned;
        word.LearnedAt = DateTimeOffset.UtcNow;
        return new(word.MemoryLevel, IsLearned: true);
    }

    private static AdvanceResult? AdvancePhrase(User user, Guid itemId)
    {
        var phrase = user.LanguageLearning.Phrases.FirstOrDefault(p => p.Id == itemId);
        if (phrase is null) return null;

        phrase.RepeatCount++;
        phrase.CompletedTasksAtCurrentLevel++;

        if (phrase.CompletedTasksAtCurrentLevel < MemoryLevelIntervals.TasksRequiredToLevelUp)
            return new(phrase.MemoryLevel, IsLearned: false);

        // Threshold reached.
        phrase.CompletedTasksAtCurrentLevel = 0;

        if (phrase.MemoryLevel < MemoryLevelIntervals.MaxLevel)
        {
            phrase.MemoryLevel++;
            return new(phrase.MemoryLevel, IsLearned: false);
        }

        // Already at MaxLevel — mark as Learned.
        phrase.Status = LearningStatus.Learned;
        phrase.LearnedAt = DateTimeOffset.UtcNow;
        return new(phrase.MemoryLevel, IsLearned: true);
    }

    private readonly record struct AdvanceResult(int MemoryLevel, bool IsLearned);
}
