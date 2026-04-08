using System.Net;
using System.Net.Sockets;
using System.Text;
using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adapters.TcpTest;

/// <summary>Accepts TCP connections, reads UTF-8 lines (same as TcpTestClient.Console), and feeds <see cref="InboundMessagePipeline"/>.</summary>
public sealed class TcpTestInboundListenerHostedService : BackgroundService
{
    private readonly IOptions<TcpTestMessagingOptions> _options;
    private readonly InboundMessagePipeline _pipeline;
    private readonly TcpTestListenEndpoint _endpoint;
    private readonly ILogger<TcpTestInboundListenerHostedService> _logger;

    public TcpTestInboundListenerHostedService(
        IOptions<TcpTestMessagingOptions> options,
        InboundMessagePipeline pipeline,
        TcpTestListenEndpoint endpoint,
        ILogger<TcpTestInboundListenerHostedService> logger)
    {
        _options = options;
        _pipeline = pipeline;
        _endpoint = endpoint;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        if (!opts.StartInboundListener)
            return;

        var address = await ResolveAddressAsync(opts.ListenHost, stoppingToken).ConfigureAwait(false);
        using var listener = new TcpListener(address, opts.ListenPort);
        listener.Start();
        _endpoint.AssignedListenPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        _logger.LogInformation(
            "TcpTest messaging inbound listener on {Endpoint} (channel {Channel})",
            listener.LocalEndpoint,
            ChannelKind.TcpTest);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                _ = ProcessClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<IPAddress> ResolveAddressAsync(string host, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var parsed))
            return parsed;

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        var chosen = addresses.FirstOrDefault(a =>
            a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
        return chosen ?? throw new InvalidOperationException($"Cannot resolve host '{host}' for TcpTest listener.");
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        try
        {
            using (client)
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line is null)
                        break;
                    if (line.Length == 0)
                        continue;

                    var msg = new InboundMessage(ChannelKind.TcpTest, remote, line, null);
                    await _pipeline.RunAsync(msg, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TcpTest client {Remote} ended with error", remote);
        }
    }
}
