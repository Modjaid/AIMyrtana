using System.Text.Json;
using Messaging.Abstractions;

namespace Adapters.Telegram;

public sealed class TelegramWebhookHandler : IWebhookHandler
{
    public ChannelKind Channel => ChannelKind.Telegram;

    public async Task<WebhookHandleResult> HandleAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream();
        await context.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        ms.Position = 0;

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return new WebhookHandleResult(false, Error: ex.Message);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!TryMapMessage(root, out var inbound))
                return new WebhookHandleResult(true, Array.Empty<InboundMessage>());

            return new WebhookHandleResult(true, new[] { inbound });
        }
    }

    private static bool TryMapMessage(JsonElement root, out InboundMessage inbound)
    {
        inbound = new InboundMessage(ChannelKind.Unknown, "", null, null);
        if (!root.TryGetProperty("message", out var msg))
            return false;

        if (!msg.TryGetProperty("chat", out var chat) || !chat.TryGetProperty("id", out var chatId))
            return false;

        var id = chatId.GetRawText().Trim('"');
        string? text = null;
        if (msg.TryGetProperty("text", out var textEl))
            text = textEl.GetString();

        inbound = new InboundMessage(
            ChannelKind.Telegram,
            id,
            text,
            msg.TryGetProperty("message_id", out var mid) ? mid.GetRawText() : null);
        return true;
    }
}
