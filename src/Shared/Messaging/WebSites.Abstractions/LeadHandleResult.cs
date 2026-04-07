namespace WebSites.Abstractions;

public sealed record LeadHandleResult(bool Accepted, string? Error = null);
