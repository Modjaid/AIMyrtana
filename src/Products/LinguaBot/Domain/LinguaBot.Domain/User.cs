namespace LinguaBot.Domain;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Telegram user ID (from.id in the Update payload).</summary>
    public long TelegramUserId { get; set; }

    public string? TelegramUsername { get; set; }

    public string? TelegramFirstName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public LanguageLearning LanguageLearning { get; set; } = new();

    public DialogState DialogState { get; set; } = new();
}

public class LanguageLearning
{
    /// <summary>The user's native language (e.g. "русский", "English").</summary>
    public string NativeLanguage { get; set; } = string.Empty;

    /// <summary>Languages the user is currently studying.</summary>
    public List<string> TargetLanguages { get; set; } = [];

    public List<LearningWord> Words { get; set; } = [];

    public List<LearningPhrase> Phrases { get; set; } = [];
}

public static class MemoryLevelIntervals
{
    /// <summary>Days until next repetition for each memory level.</summary>
    public const int Level0 = 1;   // следующий день
    public const int Level1 = 5;   // через 5 дней
    public const int Level2 = 21;  // через 3 недели
    public const int Level3 = 60;  // через 2 месяца

    /// <summary>Maximum memory level. Beyond this level the item is considered fully learned.</summary>
    public const int MaxLevel = 3;

    /// <summary>
    /// Number of successfully completed review tasks required at the current level
    /// before <see cref="LearningWord.MemoryLevel"/> / <see cref="LearningPhrase.MemoryLevel"/> is incremented.
    /// </summary>
    public const int TasksRequiredToLevelUp = 3;

    public static int GetRepeatAfterDays(int memoryLevel) => memoryLevel switch
    {
        0 => Level0,
        1 => Level1,
        2 => Level2,
        3 => Level3,
        _ => Level0,
    };
}

public class LearningWord
{
    public Guid Id { get; set; }

    /// <summary>ISO language code or language name, e.g. "en", "fr".</summary>
    public string Language { get; set; } = string.Empty;

    public string OriginalText { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;

    public LearningStatus Status { get; set; } = LearningStatus.New;

    /// <summary>
    /// Spaced-repetition memory level (0–3).
    /// Use <see cref="MemoryLevelIntervals"/> to get the repeat interval in days.
    /// </summary>
    public int MemoryLevel { get; set; }

    /// <summary>
    /// How many review tasks at the current <see cref="MemoryLevel"/> have been completed successfully.
    /// Resets to 0 when the level is incremented.
    /// </summary>
    public int CompletedTasksAtCurrentLevel { get; set; }

    public int RepeatCount { get; set; }

    public int MistakeCount { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public DateTimeOffset? LearnedAt { get; set; }
}

public class LearningPhrase
{
    public Guid Id { get; set; }

    public string Language { get; set; } = string.Empty;

    public string OriginalText { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;

    public LearningStatus Status { get; set; } = LearningStatus.New;

    /// <summary>
    /// Spaced-repetition memory level (0–3).
    /// Use <see cref="MemoryLevelIntervals"/> to get the repeat interval in days.
    /// </summary>
    public int MemoryLevel { get; set; }

    /// <summary>
    /// How many review tasks at the current <see cref="MemoryLevel"/> have been completed successfully.
    /// Resets to 0 when the level is incremented.
    /// </summary>
    public int CompletedTasksAtCurrentLevel { get; set; }

    public int RepeatCount { get; set; }

    public int MistakeCount { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public DateTimeOffset? LearnedAt { get; set; }
}

public enum LearningStatus
{
    New = 0,
    InProgress = 1,
    Learned = 2,
}

/// <summary>Persistent per-user dialog state.</summary>
public class DialogState
{
    /// <summary>Whether the initial bot introduction has been completed.</summary>
    public bool BotIntroductionCompleted { get; set; }

    /// <summary>Name of the current activity the agent is running, e.g. "quiz".</summary>
    public string? CurrentActivity { get; set; }
}
