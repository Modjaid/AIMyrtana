namespace Messaging.Abstractions;

public sealed record WebhookHandleResult(
    bool Handled,
    IReadOnlyList<InboundMessage>? Messages = null,
    string? Error = null);
