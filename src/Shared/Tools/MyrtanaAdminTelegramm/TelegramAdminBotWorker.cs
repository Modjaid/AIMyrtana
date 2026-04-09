using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyrtanaAdminTelegramm;

internal sealed class TelegramAdminBotWorker : BackgroundService
{
    private const int TelegramMaxMessageLength = 4096;
    private readonly BotOptions _options;
    private readonly ILogger<TelegramAdminBotWorker> _logger;
    private readonly TelegramBotClient _bot;

    public TelegramAdminBotWorker(BotOptions options, ILogger<TelegramAdminBotWorker> logger)
    {
        _options = options;
        _logger = logger;
        _bot = new TelegramBotClient(options.BotToken);
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

        if (message.Chat.Type != ChatType.Private)
            return;

        var text = message.Text.Trim();
        if (text.Length == 0 || text[0] != '/')
            return;

        var command = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var at = command.IndexOf('@', StringComparison.Ordinal);
        if (at > 0)
            command = command[..at];

        if (!command.Equals("/services", StringComparison.OrdinalIgnoreCase))
            return;

        var from = message.From;
        if (from is null)
            return;

        var userId = from.Id;
        if (!_options.AdminUserIds.Contains(userId))
        {
            await bot.SendMessage(
                    message.Chat.Id,
                    "Доступ запрещён.",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var report = await BuildServicesReportAsync(from, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in ChunkForTelegram(report, TelegramMaxMessageLength))
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
            var title = string.IsNullOrWhiteSpace(s.Title) ? s.Unit.Trim() : s.Title.Trim();
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
            lines.Add($"{mark} {title}");
        }

        return string.Join("\n", lines);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }

    internal static IEnumerable<string> ChunkForTelegram(string text, int maxLen)
    {
        if (text.Length <= maxLen)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < text.Length)
        {
            var remaining = text.Length - start;
            var take = Math.Min(maxLen, remaining);
            if (take < remaining)
            {
                var slice = text.AsSpan(start, take);
                var lastNl = slice.LastIndexOf('\n');
                if (lastNl > maxLen / 2)
                    take = lastNl + 1;
            }

            yield return text.Substring(start, take);
            start += take;
        }
    }
}
