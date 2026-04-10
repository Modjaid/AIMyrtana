using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyrtanaAdminTelegramm;

internal sealed class TelegramAdminBotWorker : BackgroundService
{
    private readonly BotOptions _options;
    private readonly ILogger<TelegramAdminBotWorker> _logger;
    private readonly TelegramBotClient _bot;
    private readonly TelegramAdminCommandRouter _commandRouter;

    public TelegramAdminBotWorker(
        BotOptions options,
        ILogger<TelegramAdminBotWorker> logger,
        TelegramAdminCommandRouter commandRouter)
    {
        _options = options;
        _logger = logger;
        _bot = new TelegramBotClient(options.BotToken);
        _commandRouter = commandRouter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message],
        };

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);

        var me = await _bot.GetMe(stoppingToken).ConfigureAwait(false);
        _logger.LogInformation("Telegram bot @{Username} polling started.", me.Username);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is null)
            return;

        await _commandRouter.TryHandleAsync(bot, message, cancellationToken).ConfigureAwait(false);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
