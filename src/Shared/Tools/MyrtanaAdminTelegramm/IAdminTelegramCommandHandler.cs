using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyrtanaAdminTelegramm;

internal interface IAdminTelegramCommandHandler
{
    string Command { get; }

    Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken cancellationToken);
}
