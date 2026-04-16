using System.ComponentModel;
using LinguaBot.Domain;
using Microsoft.SemanticKernel;

namespace LinguaBot.Agent;

/// <summary>
/// Semantic Kernel plugin that exposes language-learning and scheduling operations for the current user.
/// Instantiated per agent turn so all mutations target the correct user instance.
/// </summary>
public sealed class UserLanguagePlugin(User user, ISchedulerService scheduler)
{
    // ── Language learning ────────────────────────────────────────────────────

    [KernelFunction("set_native_language")]
    [Description("Установить родной язык пользователя.")]
    public string SetNativeLanguage(
        [Description("Название родного языка, например 'русский' или 'English'")] string language)
    {
        user.LanguageLearning.NativeLanguage = language;
        return $"Родной язык установлен: {language}.";
    }

    [KernelFunction("add_target_language")]
    [Description("Добавить язык для изучения.")]
    public string AddTargetLanguage(
        [Description("Название языка для изучения, например 'английский' или 'English'")] string language)
    {
        if (!user.LanguageLearning.TargetLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
            user.LanguageLearning.TargetLanguages.Add(language);
        return $"Язык добавлен: {language}.";
    }

    [KernelFunction("add_word")]
    [Description("Добавить слово в список изучения пользователя.")]
    public string AddWord(
        [Description("Код языка или название, например 'en' или 'английский'")] string language,
        [Description("Слово на изучаемом языке")] string word,
        [Description("Перевод слова на родной язык пользователя")] string translation)
    {
        user.LanguageLearning.Words.Add(new LearningWord
        {
            Id = Guid.NewGuid(),
            Language = language,
            OriginalText = word,
            Translation = translation,
            Status = LearningStatus.New,
            AddedAt = DateTimeOffset.UtcNow,
        });
        return $"Слово «{word}» ({translation}) добавлено.";
    }

    [KernelFunction("add_phrase")]
    [Description("Добавить фразу в список изучения пользователя.")]
    public string AddPhrase(
        [Description("Код языка или название")] string language,
        [Description("Фраза на изучаемом языке")] string phrase,
        [Description("Перевод фразы на родной язык пользователя")] string translation)
    {
        user.LanguageLearning.Phrases.Add(new LearningPhrase
        {
            Id = Guid.NewGuid(),
            Language = language,
            OriginalText = phrase,
            Translation = translation,
            Status = LearningStatus.New,
            AddedAt = DateTimeOffset.UtcNow,
        });
        return $"Фраза «{phrase}» добавлена.";
    }

    [KernelFunction("mark_word_learned")]
    [Description("Отметить слово как выученное по его ID.")]
    public string MarkWordLearned(
        [Description("ID слова в формате Guid")] string wordId)
    {
        if (!Guid.TryParse(wordId, out var id))
            return "Неверный формат ID слова.";

        var word = user.LanguageLearning.Words.FirstOrDefault(w => w.Id == id);
        if (word is null)
            return "Слово не найдено.";

        word.Status = LearningStatus.Learned;
        word.LearnedAt = DateTimeOffset.UtcNow;
        return $"Слово «{word.OriginalText}» отмечено как выученное!";
    }

    [KernelFunction("mark_phrase_learned")]
    [Description("Отметить фразу как выученную по её ID.")]
    public string MarkPhraseLearned(
        [Description("ID фразы в формате Guid")] string phraseId)
    {
        if (!Guid.TryParse(phraseId, out var id))
            return "Неверный формат ID фразы.";

        var phrase = user.LanguageLearning.Phrases.FirstOrDefault(p => p.Id == id);
        if (phrase is null)
            return "Фраза не найдена.";

        phrase.Status = LearningStatus.Learned;
        phrase.LearnedAt = DateTimeOffset.UtcNow;
        return $"Фраза «{phrase.OriginalText}» отмечена как выученная!";
    }

    [KernelFunction("get_words_for_review")]
    [Description("Получить список слов для повторения (ещё не выученных).")]
    public string GetWordsForReview()
    {
        var words = user.LanguageLearning.Words
            .Where(w => w.Status != LearningStatus.Learned)
            .ToList();

        if (words.Count == 0)
            return "Нет слов для повторения — все выучены или список пуст!";

        return string.Join("\n", words.Select(w =>
            $"[{w.Id}] {w.OriginalText} — {w.Translation} (повторений: {w.RepeatCount})"));
    }

    [KernelFunction("get_phrases_for_review")]
    [Description("Получить список фраз для повторения (ещё не выученных).")]
    public string GetPhrasesForReview()
    {
        var phrases = user.LanguageLearning.Phrases
            .Where(p => p.Status != LearningStatus.Learned)
            .ToList();

        if (phrases.Count == 0)
            return "Нет фраз для повторения — все выучены или список пуст!";

        return string.Join("\n", phrases.Select(p =>
            $"[{p.Id}] {p.OriginalText} — {p.Translation} (повторений: {p.RepeatCount})"));
    }

    [KernelFunction("record_word_attempt")]
    [Description("Записать результат попытки вспомнить слово.")]
    public string RecordWordAttempt(
        [Description("ID слова в формате Guid")] string wordId,
        [Description("true если ответ правильный, false если ошибка")] bool correct)
    {
        if (!Guid.TryParse(wordId, out var id))
            return "Неверный формат ID слова.";

        var word = user.LanguageLearning.Words.FirstOrDefault(w => w.Id == id);
        if (word is null)
            return "Слово не найдено.";

        word.RepeatCount++;
        if (!correct)
            word.MistakeCount++;
        if (word.Status == LearningStatus.New)
            word.Status = LearningStatus.InProgress;

        return correct ? "Правильно!" : $"Неверно. Слово: «{word.OriginalText}» = {word.Translation}.";
    }

    [KernelFunction("complete_introduction")]
    [Description("Отметить первичное знакомство с ботом как завершённое.")]
    public string CompleteIntroduction()
    {
        user.DialogState.BotIntroductionCompleted = true;
        return "Знакомство завершено.";
    }

    // ── Scheduling ───────────────────────────────────────────────────────────

    [KernelFunction("schedule_reminder")]
    [Description("Запланировать разовое напоминание пользователю.")]
    public async Task<string> ScheduleReminderAsync(
        [Description("Тип: WordReview, PhraseReview, DailyLesson или Custom")] string type,
        [Description("Дата и время в формате ISO 8601 UTC, например '2026-04-13T09:00:00Z'")] string scheduledAt,
        [Description("Текст напоминания (только для типа Custom, для остальных можно оставить пустым)")] string? payload = null)
    {
        if (!Enum.TryParse<ScheduledTaskType>(type, ignoreCase: true, out var taskType))
            return $"Неизвестный тип напоминания: {type}. Допустимые: WordReview, PhraseReview, DailyLesson, Custom.";

        if (!DateTimeOffset.TryParse(scheduledAt, out var at))
            return $"Неверный формат даты: {scheduledAt}. Используй ISO 8601, например '2026-04-13T09:00:00Z'.";

        await scheduler.ScheduleOnceAsync(user.Id, user.TelegramUserId, taskType, at, payload);
        return $"Напоминание типа {taskType} запланировано на {at:dd.MM.yyyy HH:mm} UTC.";
    }

    [KernelFunction("set_daily_reminder")]
    [Description("Настроить ежедневное повторяющееся напоминание по расписанию (cron UTC).")]
    public async Task<string> SetDailyReminderAsync(
        [Description("Тип: WordReview, PhraseReview или DailyLesson")] string type,
        [Description("Время в формате HH:mm UTC, например '09:00'")] string timeUtc)
    {
        if (!Enum.TryParse<ScheduledTaskType>(type, ignoreCase: true, out var taskType))
            return $"Неизвестный тип: {type}. Допустимые: WordReview, PhraseReview, DailyLesson.";

        var parts = timeUtc.Split(':');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var hour) ||
            !int.TryParse(parts[1], out var minute) ||
            hour is < 0 or > 23 ||
            minute is < 0 or > 59)
        {
            return $"Неверный формат времени: {timeUtc}. Используй HH:mm, например '09:00'.";
        }

        var cron = $"{minute} {hour} * * *";
        await scheduler.ScheduleRecurringAsync(user.Id, user.TelegramUserId, taskType, cron);
        return $"Ежедневное напоминание типа {taskType} настроено на {timeUtc} UTC (cron: {cron}).";
    }

    [KernelFunction("cancel_reminder")]
    [Description("Отменить напоминание по его ID.")]
    public async Task<string> CancelReminderAsync(
        [Description("ID напоминания в формате Guid")] string taskId)
    {
        if (!Guid.TryParse(taskId, out var id))
            return "Неверный формат ID напоминания.";

        await scheduler.CancelAsync(id);
        return $"Напоминание {id} отменено.";
    }

    [KernelFunction("get_my_reminders")]
    [Description("Получить список активных напоминаний пользователя.")]
    public async Task<string> GetMyRemindersAsync()
    {
        var tasks = await scheduler.GetForUserAsync(user.Id);
        if (tasks.Count == 0)
            return "Активных напоминаний нет.";

        return string.Join("\n", tasks.Select(t =>
            $"[{t.Id}] {t.Type} — {(t.IsRecurring ? $"ежедневно (cron: {t.CronExpression})" : $"разово {t.ScheduledAt:dd.MM.yyyy HH:mm} UTC")} [{t.Status}]"));
    }
}
