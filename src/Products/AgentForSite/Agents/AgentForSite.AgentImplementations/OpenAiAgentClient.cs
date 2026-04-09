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

    public async Task<string> GetReplyAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable(EnvKeyName);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Missing environment variable '{EnvKeyName}'.");

        var http = httpClientFactory.CreateClient(nameof(OpenAiAgentClient));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Legacy landing proxy: single user line (no server-side session / system from client).
        var payload = new
        {
            model = "gpt-4.1-mini",
            input = userMessage,
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

