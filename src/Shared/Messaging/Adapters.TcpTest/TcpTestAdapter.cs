using Messaging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adapters.TcpTest;

/// <summary>Bundles TCP test webhook handler, outbound sender, and optional inbound listener registration.</summary>
public sealed class TcpTestAdapter : IMessageAdapter
{
    public TcpTestAdapter(IOptions<TcpTestMessagingOptions> options, TcpTestListenEndpoint listenEndpoint)
    {
        WebhookHandler = new TcpTestWebhookHandler();
        OutboundSender = new TcpTestOutboundSender(options, listenEndpoint);
    }

    public ChannelKind Channel => ChannelKind.TcpTest;
    public IWebhookHandler WebhookHandler { get; }
    public IOutboundSender OutboundSender { get; }
}
