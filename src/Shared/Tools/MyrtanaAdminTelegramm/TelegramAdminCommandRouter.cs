using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyrtanaAdminTelegramm;

internal sealed class TelegramAdminCommandRouter
{
    private readonly BotOptions _options;
    private readonly ILogger<TelegramAdminCommandRouter> _logger;
    private readonly Dictionary<string, IAdminTelegramCommandHandler> _handlers;
    private readonly ServicePowerCommandHandler _powerCommandHandler;

    public TelegramAdminCommandRouter(
        BotOptions options,
        ILogger<TelegramAdminCommandRouter> logger,
        IEnumerable<IAdminTelegramCommandHandler> handlers,
        ServicePowerCommandHandler powerCommandHandler)
    {
        _options = options;
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.Command, h => h, StringComparer.OrdinalIgnoreCase);
        _powerCommandHandler = powerCommandHandler;
    }

    public async Task TryHandleAsync(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        if (message.Text is null)
            return;

        if (message.Chat.Type != ChatType.Private)
            return;

        if (!TelegramCommandParser.TryGetCommandName(message.Text, out var commandName))
            return;

        var trimmed = message.Text.Trim();
        var hasHandler = _handlers.TryGetValue(commandName, out var handler);
        var hasPower = ServicePowerCommandParser.TryParse(trimmed, out var activate, out var serviceKey);

        if (!hasHandler && !hasPower)
            return;

        if (message.From is null)
            return;

        if (!_options.AdminUserIds.Contains(message.From.Id))
        {
            await bot.SendMessage(
                    message.Chat.Id,
                    "Доступ запрещён.",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            if (hasHandler && handler is not null)
                await handler.ExecuteAsync(bot, message, cancellationToken).ConfigureAwait(false);
            else if (hasPower && serviceKey is not null)
                await _powerCommandHandler.ExecuteAsync(bot, message, activate, serviceKey, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {Command} failed.", commandName);
        }
    }
}
