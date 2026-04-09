using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AgentForSite.AgentImplementations;

public readonly record struct OpenAiChatMessage(string Role, string Content);

/// <summary>
/// One consultant turn from the forced <c>submit_consultant_turn</c> tool (Responses API).
/// </summary>
public readonly record struct ConsultantStructuredTurn(
    string AssistantMessage,
    IReadOnlyList<string> DevStack,
    int CurrentPriceUsd);

public static class ConsultantChatFormatting
{
    /// <summary>
    /// Canonical assistant text stored in chat session history so multi-turn context matches the prior prompt format.
    /// </summary>
    public static string BuildHistoryContent(string assistantMessage, IReadOnlyList<string> devStack)
    {
        var msg = (assistantMessage ?? "").Trim();
        if (devStack is null || devStack.Count == 0)
            return string.IsNullOrEmpty(msg) ? "" : $"Draft stack:\n(none)\n\nQuestion: {msg}";

        var sb = new StringBuilder();
        sb.Append("Draft stack:\n");
        var n = 0;
        for (var i = 0; i < devStack.Count; i++)
        {
            var line = (devStack[i] ?? "").Trim();
            if (line.Length == 0)
                continue;
            n++;
            sb.Append(n).Append(") ").Append(line).Append('\n');
        }

        if (n == 0)
            sb.Append("(none)\n");
        sb.Append("\nQuestion: ").Append(msg);
        return sb.ToString();
    }
}

public interface IOpenAiAgentClient
{
    Task<string> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full conversation for multi-turn context (Responses API message list). Plain text output (e.g. pricing JSON).
    /// </summary>
    Task<string> GetReplyFromConversationAsync(
        IReadOnlyList<OpenAiChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consultant chat turn via forced function call (strict schema).
    /// </summary>
    Task<ConsultantStructuredTurn> GetStructuredConsultantTurnAsync(
        IReadOnlyList<OpenAiChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stateless consultant turn (internal system prompt + one user message).
    /// </summary>
    Task<ConsultantStructuredTurn> GetStructuredConsultantTurnAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}

public sealed class OpenAiAgentClient(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiAgentClient> logger) : IOpenAiAgentClient
{
    private const string EnvKeyName = "OpenAI_Key_AgentForSite";
    private const string ConsultantToolName = "submit_consultant_turn";
    private const string ModelId = "gpt-4.1-mini";

    /// <summary>
    /// Compact mirror of the landing <c>system_prompt</c> for stateless calls (no chat session / no client-supplied system).
    /// </summary>
    private const string StatelessConsultantSystemPrompt =
        "You are a consultant for automation and AI agent projects. The user does not need technical internals.\n"
        + "You MUST call submit_consultant_turn every turn. Put your follow-up for the user in assistantMessage.\n"
        + "Put the full current devStack array (client-friendly building blocks only, in sensible order). "
        + "Do not name PostgreSQL, Redis, RabbitMQ, Kafka, Kubernetes, Docker, object storage, message queues, cron, "
        + "worker pools, embeddings, vector DBs, retry policies, etc., unless the user explicitly asks. "
        + "Use blocks like Data storage, Scheduler, OCR engine, Telegram API, Server / message handler.\n"
        + "Stack grows across turns — output the complete list each time. Add blocks only when clearly needed.\n"
        + "currentPriceUsd: rough US list-price ballpark for the work implied by devStack, or 0 if unknown.\n"
        + "Reply in the user's language. No invented facts about the customer.";

    public async Task<string> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var turn = await GetStructuredConsultantTurnAsync(userMessage, cancellationToken).ConfigureAwait(false);
        return turn.AssistantMessage;
    }

    public async Task<string> GetReplyFromConversationAsync(
        IReadOnlyList<OpenAiChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages is null || messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var apiKey = Environment.GetEnvironmentVariable(EnvKeyName);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Missing environment variable '{EnvKeyName}'.");

        var http = httpClientFactory.CreateClient(nameof(OpenAiAgentClient));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var input = new object[messages.Count];
        for (var i = 0; i < messages.Count; i++)
        {
            var role = NormalizeRole(messages[i].Role);
            input[i] = new { type = "message", role, content = messages[i].Content };
        }

        var payload = new
        {
            model = ModelId,
            input,
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var resp = await http
            .PostAsync("https://api.openai.com/v1/responses", content, cancellationToken)
            .ConfigureAwait(false);

        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI call failed: {StatusCode}", (int)resp.StatusCode);
            throw new InvalidOperationException($"OpenAI error: HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        return TryExtractOutputText(json) ?? string.Empty;
    }

    public Task<ConsultantStructuredTurn> GetStructuredConsultantTurnAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var u = (userMessage ?? "").Trim();
        if (u.Length == 0)
            throw new ArgumentException("User message is required.", nameof(userMessage));

        return GetStructuredConsultantTurnAsync(
            new[]
            {
                new OpenAiChatMessage("system", StatelessConsultantSystemPrompt),
                new OpenAiChatMessage("user", u),
            },
            cancellationToken);
    }

    public async Task<ConsultantStructuredTurn> GetStructuredConsultantTurnAsync(
        IReadOnlyList<OpenAiChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages is null || messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var apiKey = Environment.GetEnvironmentVariable(EnvKeyName);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Missing environment variable '{EnvKeyName}'.");

        var http = httpClientFactory.CreateClient(nameof(OpenAiAgentClient));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var input = new object[messages.Count];
        for (var i = 0; i < messages.Count; i++)
        {
            var role = NormalizeRole(messages[i].Role);
            input[i] = new { type = "message", role, content = messages[i].Content };
        }

        var tools = new object[]
        {
            new
            {
                type = "function",
                name = ConsultantToolName,
                description =
                    "Submit this turn: visible follow-up message, full implementation stack as strings, and rough US list price hint.",
                strict = true,
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        assistantMessage = new
                        {
                            type = "string",
                            description = "Short follow-up question or guidance for the user in plain language.",
                        },
                        devStack = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description =
                                "Ordered client-facing building blocks; full current list for this turn (input → output).",
                        },
                        currentPriceUsd = new
                        {
                            type = "integer",
                            description =
                                "Rough ballpark US list price in USD for work implied by devStack; use 0 if unknown.",
                        },
                    },
                    required = new[] { "assistantMessage", "devStack", "currentPriceUsd" },
                    additionalProperties = false,
                },
            },
        };

        var payload = new
        {
            model = ModelId,
            input,
            tools,
            tool_choice = new { type = "function", name = ConsultantToolName },
        };

        using var reqContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var resp = await http
            .PostAsync("https://api.openai.com/v1/responses", reqContent, cancellationToken)
            .ConfigureAwait(false);

        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI structured call failed: {StatusCode} {Body}", (int)resp.StatusCode, json);
            throw new InvalidOperationException($"OpenAI error: HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        var argsJson = TryExtractFunctionCallArguments(json, ConsultantToolName);
        if (string.IsNullOrWhiteSpace(argsJson))
            throw new InvalidOperationException("OpenAI response did not include the expected function call.");

        return ParseConsultantTurnArgs(argsJson);
    }

    private static ConsultantStructuredTurn ParseConsultantTurnArgs(string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var root = doc.RootElement;

        var assistant =
            root.TryGetProperty("assistantMessage", out var am) && am.ValueKind == JsonValueKind.String
                ? am.GetString() ?? ""
                : "";

        var stack = new List<string>();
        if (root.TryGetProperty("devStack", out var ds) && ds.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in ds.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        stack.Add(s.Trim());
                }
            }
        }

        var price = 0;
        if (root.TryGetProperty("currentPriceUsd", out var p) && p.ValueKind == JsonValueKind.Number &&
            p.TryGetInt32(out var pi))
            price = pi;

        return new ConsultantStructuredTurn(assistant, stack, price);
    }

    private static string? TryExtractFunctionCallArguments(string json, string expectedName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("output", out var output) ||
                output.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
                    continue;
                var t = typeEl.GetString();
                if (!string.Equals(t, "function_call", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                {
                    var n = nameEl.GetString();
                    if (!string.Equals(n, expectedName, StringComparison.Ordinal))
                        continue;
                }

                if (item.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.String)
                    return argsEl.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeRole(string role)
    {
        var r = (role ?? "").Trim().ToLowerInvariant();
        return r switch
        {
            "system" => "system",
            "assistant" => "assistant",
            "developer" => "developer",
            _ => "user",
        };
    }

    private static string? TryExtractOutputText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("output_text", out var outputText) &&
                outputText.ValueKind == JsonValueKind.String)
                return outputText.GetString();

            if (!doc.RootElement.TryGetProperty("output", out var output) ||
                output.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) ||
                    content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        return text.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
