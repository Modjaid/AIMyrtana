using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyrtanaAdminTelegramm;

internal sealed class ServicePowerCommandHandler
{
    private readonly BotOptions _options;
    private readonly ILogger<ServicePowerCommandHandler> _logger;

    public ServicePowerCommandHandler(BotOptions options, ILogger<ServicePowerCommandHandler> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        ITelegramBotClient bot,
        Message message,
        bool activate,
        string serviceKey,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.ServicesJsonPath))
        {
            await ReplyAsync(bot, message, $"Файл конфигурации не найден: {_options.ServicesJsonPath}", cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        ServicesConfigFile? config;
        try
        {
            config = await ServicesConfigLoader.LoadAsync(_options.ServicesJsonPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ReplyAsync(bot, message, "Не удалось прочитать JSON: " + ex.Message, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var entries = config?.Services ?? [];
        var entry = ServiceCatalog.FindByKey(entries, serviceKey);
        if (entry is null)
        {
            await ReplyAsync(bot, message, $"Сервис «{serviceKey}» не найден в конфигурации.", cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var unit = entry.Unit.Trim();
        if (!SystemdActiveProbe.IsSafeUnitName(unit))
        {
            await ReplyAsync(bot, message, $"Недопустимое имя unit: {unit}", cancellationToken).ConfigureAwait(false);
            return;
        }

        var title = ServiceCatalog.DisplayTitle(entry);
        bool? ok = activate
            ? await SystemdActiveProbe.StartUnitAsync(unit, cancellationToken).ConfigureAwait(false)
            : await SystemdActiveProbe.StopUnitAsync(unit, cancellationToken).ConfigureAwait(false);

        var action = activate ? "запуск" : "остановка";
        var text = ok switch
        {
            true => $"✅ {action}: {title} ({unit})",
            false => $"❌ {action} не удалась: {title} ({unit})",
            null => $"❓ Не удалось выполнить {action} для {title} ({unit}). Проверьте права и systemctl.",
        };

        await ReplyAsync(bot, message, text, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReplyAsync(
        ITelegramBotClient bot,
        Message message,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            await bot.SendMessage(message.Chat.Id, text, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send power-command reply to chat {ChatId}.", message.Chat.Id);
        }
    }
}
