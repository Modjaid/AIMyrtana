namespace Messaging.Abstractions;

public sealed record InboundMessage(
    ChannelKind Channel,
    string ExternalChatId,
    string? Text,
    string? RawPayloadId,
    IReadOnlyDictionary<string, string>? Metadata = null);
