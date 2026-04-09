using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AgentForSite.AgentImplementations;

public readonly record struct OpenAiChatMessage(string Role, string Content);

public interface IOpenAiAgentClient
{
    Task<string> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full conversation for multi-turn context (Responses API message list).
    /// </summary>
    Task<string> GetReplyFromConversationAsync(
        IReadOnlyList<OpenAiChatMessage> messages,
        CancellationToken cancellationToken = default);
}

public sealed class OpenAiAgentClient(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiAgentClient> logger) : IOpenAiAgentClient
{
    private const string EnvKeyName = "OpenAI_Key_AgentForSite";

    /// <summary>
    /// Compact mirror of the landing <c>system_prompt</c> for stateless calls (no chat session / no client-supplied system).
    /// </summary>
    private const string StatelessAgentForSiteSystemPrompt =
        "You are a consultant for automation and AI agent projects. The user does not need technical internals.\n"
        + "EVERY reply: (1) Line \"Draft stack:\" then a numbered list of client-friendly building blocks only. "
        + "Do not name PostgreSQL, Redis, RabbitMQ, Kafka, Kubernetes, object storage, message queues, cron, "
        + "worker pools, embeddings, vector DBs, retry policies, etc., unless the user explicitly asks. "
        + "Use blocks like Data storage, Scheduler, OCR engine, Telegram API, Server / message handler. "
        + "The stack grows each turn — output the full current list in order (input → processing → memory → automation → output). "
        + "Add blocks only when clearly needed.\n"
        + "(2) Line \"Question:\" then one short plain-language follow-up (or two very short related ones). "
        + "No database/broker/orchestrator shopping questions.\n"
        + "Reply in the user's language. No timelines, prices, or invented facts about the customer.";

    public async Task<string> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable(EnvKeyName);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Missing environment variable '{EnvKeyName}'.");

        var http = httpClientFactory.CreateClient(nameof(OpenAiAgentClient));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var input = new object[]
        {
            new { type = "message", role = "system", content = StatelessAgentForSiteSystemPrompt },
            new { type = "message", role = "user", content = userMessage },
        };

        var payload = new
        {
            model = "gpt-4.1-mini",
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
            model = "gpt-4.1-mini",
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

            // Responses API: often provides output_text as a convenience field.
            if (doc.RootElement.TryGetProperty("output_text", out var outputText) &&
                outputText.ValueKind == JsonValueKind.String)
                return outputText.GetString();

            // Fallback: scan output[].content[].text
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

