namespace Messaging.Abstractions;

public sealed record SendResult(bool Ok, string? ExternalMessageId = null, string? Error = null);
