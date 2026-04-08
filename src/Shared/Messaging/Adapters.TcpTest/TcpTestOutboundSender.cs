using System.Net.Sockets;
using System.Text;
using Messaging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adapters.TcpTest;

public sealed class TcpTestOutboundSender : IOutboundSender
{
    private readonly IOptions<TcpTestMessagingOptions> _options;
    private readonly TcpTestListenEndpoint _listenEndpoint;

    public TcpTestOutboundSender(
        IOptions<TcpTestMessagingOptions> options,
        TcpTestListenEndpoint listenEndpoint)
    {
        _options = options;
        _listenEndpoint = listenEndpoint;
    }

    public ChannelKind Channel => ChannelKind.TcpTest;

    public async Task<SendResult> SendAsync(
        OutboundMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message.Channel != ChannelKind.TcpTest)
            return new SendResult(false, Error: "Wrong channel");

        var opts = _options.Value;
        var port = opts.OutboundPort != 0
            ? opts.OutboundPort
            : _listenEndpoint.AssignedListenPort ?? TcpTestMessagingOptions.DefaultPort;

        var payload = message.Text;
        if (!payload.EndsWith('\n'))
            payload += Environment.NewLine;

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(opts.OutboundHost, port, cancellationToken).ConfigureAwait(false);
            await using var stream = client.GetStream();
            var bytes = Encoding.UTF8.GetBytes(payload);
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return new SendResult(true);
        }
        catch (Exception ex)
        {
            return new SendResult(false, Error: ex.Message);
        }
    }
}
