using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyrtanaAdminTelegramm;

internal sealed class ServicesAdminCommandHandler : IAdminTelegramCommandHandler
{
    private readonly BotOptions _options;
    private readonly ILogger<ServicesAdminCommandHandler> _logger;

    public ServicesAdminCommandHandler(BotOptions options, ILogger<ServicesAdminCommandHandler> logger)
    {
        _options = options;
        _logger = logger;
    }

    public string Command => "/services";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
    {
        var from = message.From;
        if (from is null)
            return;

        var report = await BuildServicesReportAsync(from, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in TelegramTextChunker.Chunk(report))
        {
            foreach (var adminId in _options.AdminUserIds)
            {
                try
                {
                    await bot.SendMessage(adminId, chunk, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send chunk to admin {AdminId}.", adminId);
                }
            }
        }
    }

    private async Task<string> BuildServicesReportAsync(User initiator, CancellationToken cancellationToken)
    {
        var username = string.IsNullOrEmpty(initiator.Username) ? "—" : "@" + initiator.Username;
        var lines = new List<string>
        {
            $"Инициатор: {initiator.Id} ({username})",
            "",
            "Статус сервисов:",
            "",
        };

        if (!File.Exists(_options.ServicesJsonPath))
        {
            lines.Add($"❓ Файл конфигурации не найден: {_options.ServicesJsonPath}");
            return string.Join("\n", lines);
        }

        ServicesConfigFile? config;
        try
        {
            config = await ServicesConfigLoader.LoadAsync(_options.ServicesJsonPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            lines.Add("❓ Не удалось прочитать JSON: " + ex.Message);
            return string.Join("\n", lines);
        }

        var entries = config?.Services ?? [];
        if (entries.Count == 0)
        {
            lines.Add("(в JSON нет записей в services)");
            return string.Join("\n", lines);
        }

        foreach (var s in entries)
        {
            var title = ServiceCatalog.DisplayTitle(s);
            var unit = s.Unit.Trim();
            if (unit.Length == 0)
                continue;

            if (!SystemdActiveProbe.IsSafeUnitName(unit))
            {
                lines.Add($"❓ {title} (недопустимое имя unit)");
                continue;
            }

            var active = await SystemdActiveProbe.IsActiveAsync(unit, cancellationToken).ConfigureAwait(false);
            var mark = active switch
            {
                true => "✅",
                false => "❌",
                null => "❓",
            };
            var hint = active is { } known ? ServiceCatalog.FormatSuggestedCommand(known, title) : null;
            var line = hint is { Length: > 0 }
                ? $"{mark} {title} — {hint}"
                : $"{mark} {title}";
            lines.Add(line);
        }

        return string.Join("\n", lines);
    }
}
