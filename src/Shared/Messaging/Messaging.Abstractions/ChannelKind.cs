namespace Messaging.Abstractions;

public enum ChannelKind
{
    Unknown = 0,
    Telegram = 1,
    WhatsApp = 2,
    Sms = 3,
    /// <summary>Local TCP line protocol for integration tests (see Adapters.TcpTest).</summary>
    TcpTest = 4,
}
