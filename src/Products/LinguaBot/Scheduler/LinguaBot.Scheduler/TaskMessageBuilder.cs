using LinguaBot.Domain;

namespace LinguaBot.Scheduler;

/// <summary>Builds the Telegram notification text for each task type.</summary>
public sealed class TaskMessageBuilder
{
    public string Build(ScheduledTask task) => task.Type switch
    {
        ScheduledTaskType.WordReview =>
            "📚 Время повторить слова! Напиши мне что-нибудь, чтобы начать.",
        ScheduledTaskType.PhraseReview =>
            "💬 Есть фразы для повторения! Напиши мне, чтобы начать.",
        ScheduledTaskType.DailyLesson =>
            "🎯 Привет! Не забудь позаниматься сегодня — я жду тебя.",
        ScheduledTaskType.Custom =>
            task.Payload ?? "Напоминание от языкового помощника!",
        _ => "Напоминание!",
    };
}
