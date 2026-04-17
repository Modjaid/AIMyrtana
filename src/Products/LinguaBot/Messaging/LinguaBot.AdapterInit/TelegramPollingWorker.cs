using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinguaBot.AdapterInit;

internal sealed class TelegramPollingWorker(
    ITelegramBotClient bot,
    InboundMessagePipeline pipeline,
    ILogger<TelegramPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };

        var me = await bot.GetMe(stoppingToken);
        logger.LogInformation("LinguaBot @{Username} polling started.", me.Username);

        await bot.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, options, stoppingToken);

        logger.LogInformation("LinguaBot polling stopped.");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: not null } msg)
            return;

        var chatId = msg.Chat.Id.ToString();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["telegram:from:id"] = (msg.From?.Id ?? msg.Chat.Id).ToString(),
        };
        if (msg.From?.Username is { } username)
            metadata["telegram:from:username"] = username;
        if (msg.From?.FirstName is { } firstName)
            metadata["telegram:from:first_name"] = firstName;

        var inbound = new InboundMessage(ChannelKind.Telegram, chatId, msg.Text, msg.MessageId.ToString(), metadata);

        try
        {
            await pipeline.RunAsync(inbound, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline failed for chat {ChatId}", chatId);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
