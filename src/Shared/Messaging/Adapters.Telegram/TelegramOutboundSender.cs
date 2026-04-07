using Messaging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Adapters.Telegram;

public sealed class TelegramOutboundSender : IOutboundSender
{
    private readonly ITelegramBotClient _client;

    public TelegramOutboundSender(ITelegramBotClient client) => _client = client;

    public ChannelKind Channel => ChannelKind.Telegram;

    public async Task<SendResult> SendAsync(
        OutboundMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message.Channel != ChannelKind.Telegram)
            return new SendResult(false, Error: "Wrong channel");

        try
        {
            var chat = long.TryParse(message.ExternalChatId, out var id)
                ? new ChatId(id)
                : new ChatId(message.ExternalChatId);

            var sent = await _client.SendMessage(
                    chat,
                    message.Text,
                    parseMode: ParseMode.None,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new SendResult(true, sent.MessageId.ToString());
        }
        catch (Exception ex)
        {
            return new SendResult(false, Error: ex.Message);
        }
    }
}
