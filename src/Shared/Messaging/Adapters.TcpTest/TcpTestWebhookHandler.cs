using System.Text;
using Messaging.Abstractions;

namespace Adapters.TcpTest;

/// <summary>Maps a synthetic HTTP webhook body to inbound messages (optional; primary path is the TCP listener).</summary>
public sealed class TcpTestWebhookHandler : IWebhookHandler
{
    public ChannelKind Channel => ChannelKind.TcpTest;

    public async Task<WebhookHandleResult> HandleAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream();
        await context.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var text = Encoding.UTF8.GetString(ms.ToArray()).Trim();
        if (text.Length == 0)
            return new WebhookHandleResult(true, Array.Empty<InboundMessage>());

        var chatId = context.Query.TryGetValue("chatId", out var id) && id.Length > 0
            ? id
            : "http-test";

        return new WebhookHandleResult(true, new[]
        {
            new InboundMessage(ChannelKind.TcpTest, chatId, text, null),
        });
    }
}
