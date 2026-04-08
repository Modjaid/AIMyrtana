using System.Net;
using System.Net.Sockets;
using System.Text;
using Adapters.TcpTest;
using AgentForSite.AgentImplementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentForSite.Worker;

/// <summary>
/// Simple TCP line chat server for local testing (compatible with TcpTestClient.Console).
/// Reads UTF-8 lines and writes back one UTF-8 line reply.
/// </summary>
public sealed class TcpTestAgentChatHostedService(
    IOptions<TcpTestMessagingOptions> options,
    IOpenAiAgentClient openAi,
    ILogger<TcpTestAgentChatHostedService> logger) : BackgroundService
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (!opts.StartInboundListener)
            return;

        var address = await ResolveAddressAsync(opts.ListenHost, stoppingToken).ConfigureAwait(false);
        var listener = new TcpListener(address, opts.ListenPort);
        listener.Start();
        logger.LogInformation("AgentForSite TcpTest chat listening on {Endpoint}", listener.LocalEndpoint);

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
        return chosen ?? throw new InvalidOperationException($"Cannot resolve host '{host}'.");
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken ct)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        try
        {
            using (client)
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Utf8NoBom, leaveOpen: true);
                await using var writer = new StreamWriter(stream, Utf8NoBom, leaveOpen: true) { AutoFlush = true };

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line is null)
                        break;
                    if (line.Length == 0)
                        continue;

                    logger.LogInformation("TcpTest inbound from {Remote}: {Text}", remote, line);
                    var reply = await openAi.GetReplyAsync(line, ct).ConfigureAwait(false);
                    await writer.WriteLineAsync(reply.AsMemory(), ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // ignore
        }
        catch (IOException)
        {
            // Client disconnected while we were still reading/writing; expected in local tests.
        }
        catch (SocketException)
        {
            // Client disconnected/aborted; expected in local tests.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TcpTest client {Remote} ended with error", remote);
        }
    }
}

