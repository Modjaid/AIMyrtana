using System.Text.Json;
using Messaging.Abstractions;

namespace Adapters.WhatsApp;

/// <summary>
/// Minimal Meta Cloud-style webhook parsing (entry → changes → value → messages).
/// </summary>
public sealed class WhatsAppWebhookHandler : IWebhookHandler
{
    public ChannelKind Channel => ChannelKind.WhatsApp;

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
            var list = new List<InboundMessage>();
            if (doc.RootElement.TryGetProperty("entry", out var entry) && entry.ValueKind == JsonValueKind.Array)
            {
                foreach (var ent in entry.EnumerateArray())
                {
                    if (!ent.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var ch in changes.EnumerateArray())
                    {
                        if (!ch.TryGetProperty("value", out var value))
                            continue;
                        if (!value.TryGetProperty("messages", out var messages) ||
                            messages.ValueKind != JsonValueKind.Array)
                            continue;
                        foreach (var msg in messages.EnumerateArray())
                            TryAddMessage(msg, list);
                    }
                }
            }

            return new WebhookHandleResult(true, list);
        }
    }

    private static void TryAddMessage(JsonElement msg, List<InboundMessage> list)
    {
        if (!msg.TryGetProperty("from", out var fromEl))
            return;
        var from = fromEl.GetString();
        if (string.IsNullOrEmpty(from))
            return;

        string? text = null;
        if (msg.TryGetProperty("text", out var textObj) &&
            textObj.TryGetProperty("body", out var body))
            text = body.GetString();

        var id = msg.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        list.Add(new InboundMessage(ChannelKind.WhatsApp, from, text, id));
    }
}
