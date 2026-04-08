namespace Adapters.TcpTest;

/// <summary>Published after the inbound listener starts; use for ephemeral <see cref="TcpTestMessagingOptions.ListenPort"/> = 0.</summary>
public sealed class TcpTestListenEndpoint
{
    public int? AssignedListenPort { get; internal set; }
}
