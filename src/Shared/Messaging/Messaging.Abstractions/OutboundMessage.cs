namespace Messaging.Abstractions;

public sealed record OutboundMessage(
    ChannelKind Channel,
    string ExternalChatId,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);
