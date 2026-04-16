using LinguaBot.Domain;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LinguaBot.Agent;

public sealed class LanguageTutorAgent(Kernel kernel, ISchedulerService scheduler) : ILanguageTutorAgent
{
    public async Task<string> ProcessMessageAsync(User user, string userMessage, CancellationToken ct = default)
    {
        // Clone the shared kernel and attach per-turn user plugin so state mutations are isolated.
        var localKernel = kernel.Clone();
        localKernel.Plugins.AddFromObject(new UserLanguagePlugin(user, scheduler), "UserLanguage");

        var chat = localKernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory(BuildSystemPrompt(user));
        history.AddUserMessage(userMessage);

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = 1024,
        };

        var result = await chat.GetChatMessageContentAsync(history, settings, localKernel, ct);
        return result.Content ?? "Не могу ответить прямо сейчас. Попробуй ещё раз.";
    }

    private static string BuildSystemPrompt(User user)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Ты — языковой наставник в Telegram-боте. Помогаешь пользователю изучать иностранные языки.");
        sb.AppendLine("Возможности: добавлять слова и фразы для повторения, проводить тесты, отмечать выученное, планировать напоминания.");
        sb.AppendLine("Отвечай кратко и по делу. Используй язык общения пользователя.");
        sb.AppendLine($"Текущее время UTC: {DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ssZ}.");

        if (!string.IsNullOrWhiteSpace(user.LanguageLearning.NativeLanguage))
            sb.AppendLine($"Родной язык пользователя: {user.LanguageLearning.NativeLanguage}.");

        if (user.LanguageLearning.TargetLanguages.Count > 0)
            sb.AppendLine($"Изучает: {string.Join(", ", user.LanguageLearning.TargetLanguages)}.");

        var activeWords = user.LanguageLearning.Words.Count(w => w.Status != LearningStatus.Learned);
        var learnedWords = user.LanguageLearning.Words.Count(w => w.Status == LearningStatus.Learned);
        sb.AppendLine($"Слов в работе: {activeWords}, выучено: {learnedWords}.");

        var activePhrases = user.LanguageLearning.Phrases.Count(p => p.Status != LearningStatus.Learned);
        var learnedPhrases = user.LanguageLearning.Phrases.Count(p => p.Status == LearningStatus.Learned);
        sb.AppendLine($"Фраз в работе: {activePhrases}, выучено: {learnedPhrases}.");

        if (!user.DialogState.BotIntroductionCompleted)
            sb.AppendLine("ВАЖНО: это первый разговор с пользователем. " +
                          "Коротко представься, объясни возможности бота и спроси родной язык и язык для изучения. " +
                          "После получения ответа вызови complete_introduction.");

        sb.AppendLine("Если пользователь хочет напоминания или регулярную практику — " +
                      "используй schedule_reminder (разовое) или set_daily_reminder (ежедневное). " +
                      "Для просмотра напоминаний — get_my_reminders. Для отмены — cancel_reminder.");

        return sb.ToString();
    }
}
