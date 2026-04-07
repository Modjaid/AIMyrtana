namespace AgentCore.Abstractions;

public sealed record AgentRunResult(bool Ok, string? AssistantReply, string? Error = null);
