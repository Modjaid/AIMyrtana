using Adapters.Telegram;
using LinguaBot.MessageHandlers;
using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot;

namespace Microsoft.Extensions.DependencyInjection;

public static class LinguaBotServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full LinguaBot stack: database, Telegram adapter, messaging pipeline, and AI agent.
    /// </summary>
    public static IServiceCollection AddLinguaBotStack(
        this IServiceCollection services,
        string connectionString,
        string telegramToken,
        string openAiApiKey,
        string openAiModel = "gpt-4o-mini")
    {
        // Database
        services.AddLinguaBotData(connectionString);

        // Telegram client + adapter
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(telegramToken));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebhookHandler, TelegramWebhookHandler>());
        services.TryAddSingleton<IOutboundSender>(sp =>
            new TelegramOutboundSender(sp.GetRequiredService<ITelegramBotClient>()));

        // Messaging pipeline
        services.AddMessagingRuntimeCore();
        services.AddInboundMessagePipeline();
        services.AddLoggingInboundMiddleware();
        services.TryAddSingleton<IInboundMessageSink, LinguaBotInboundSink>();

        // AI agent (Microsoft Semantic Kernel)
        services.AddLinguaBotAgent(openAiApiKey, openAiModel);

        // Scheduler: background worker + ISchedulerService
        services.AddLinguaBotScheduler();

        return services;
    }
}
