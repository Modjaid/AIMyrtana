namespace Adapters.TcpTest;

/// <summary>Configuration for TCP line-based test messaging (compatible with TcpTestClient.Console).</summary>
public sealed class TcpTestMessagingOptions
{
    public const int DefaultPort = 19407;

    /// <summary>Address the inbound listener binds to (IP or resolvable host).</summary>
    public string ListenHost { get; set; } = "127.0.0.1";

    /// <summary>TCP port for inbound listener. Use 0 for an ephemeral port; actual port is exposed via <see cref="TcpTestListenEndpoint"/>.</summary>
    public int ListenPort { get; set; } = DefaultPort;

    /// <summary>When true, starts the TCP listener as a hosted service (default for local test hosts).</summary>
    public bool StartInboundListener { get; set; } = true;

    /// <summary>Host used by <see cref="TcpTestOutboundSender"/>.</summary>
    public string OutboundHost { get; set; } = "127.0.0.1";

    /// <summary>Port for outbound sends. When 0, uses <see cref="TcpTestListenEndpoint.AssignedListenPort"/> when set.</summary>
    public int OutboundPort { get; set; } = DefaultPort;
}
