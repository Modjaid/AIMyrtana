using AgentForSite.AgentImplementations;
using AgentForSite.Api.Chat;
using AgentForSite.WebAdapter;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ChatSessionService>();
builder.Services.AddAgentForSiteStack();

var app = builder.Build();

// Redirect /ru and /en to trailing-slash URLs without registering both /ru and /ru/ as endpoints
// (otherwise GET /ru/ can match both and throws AmbiguousMatchException).
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path is "/ru" or "/en")
    {
        context.Response.Redirect(path + "/", permanent: false);
        return;
    }

    await next();
});

async Task<IResult> ServeLocalizedLanding(string locale)
{
    if (locale is not "ru" and not "en")
        return Results.Redirect("/ru/");

    var path = Path.Combine(app.Environment.ContentRootPath, "Landing", "landing.html");
    if (!File.Exists(path))
        return Results.NotFound();

    var html = await File.ReadAllTextAsync(path).ConfigureAwait(false);
    var langAttr = locale == "en" ? "en" : "ru";
    html = html.Replace("__HTML_LANG__", langAttr, StringComparison.Ordinal);
    html = html.Replace("__AFS_LOCALE__", locale, StringComparison.Ordinal);
    return Results.Content(html, "text/html; charset=utf-8");
}

app.MapGet("/", () => Results.Redirect("/ru/"));
app.MapGet("/ru/", () => ServeLocalizedLanding("ru"));
app.MapGet("/en/", () => ServeLocalizedLanding("en"));
app.MapGet("/ru/index.html", () => ServeLocalizedLanding("ru"));
app.MapGet("/en/index.html", () => ServeLocalizedLanding("en"));

app.UseDefaultFiles();
app.UseStaticFiles();
// Skip UseHttpsRedirection: HTTP-only Kestrel (local run, TLS at nginx) + Production otherwise yields 500.

app.MapGet("/health", () => Results.Ok(new { status = "ok", product = "AgentForSite" }));
app.MapAgentForSiteWebAdapter();

app.MapPost(
    "/api/chat",
    async Task<IResult> (
        HttpContext http,
        ChatRequest body,
        IOpenAiAgentClient openAi,
        ChatSessionService sessions,
        CancellationToken cancellationToken) =>
    {
        var messages = body.Messages;
        if (messages is null || messages.Count == 0)
            return Results.BadRequest(new { error = "Missing messages." });

        var lastUser = messages.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase));
        if (lastUser is null || string.IsNullOrWhiteSpace(lastUser.Content))
            return Results.BadRequest(new { error = "Missing user message." });

        string? sessionId = null;
        if (http.Request.Headers.TryGetValue("X-Chat-Session-Id", out var headerSid) && headerSid.Count > 0)
            sessionId = headerSid.ToString();
        sessionId ??= body.SessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            try
            {
                var turn = await openAi
                    .GetStructuredConsultantTurnAsync(lastUser.Content, cancellationToken)
                    .ConfigureAwait(false);
                var historyContent = ConsultantChatFormatting.BuildHistoryContent(
                    turn.AssistantMessage,
                    turn.DevStack);
                return Results.Ok(new
                {
                    assistantMessage = turn.AssistantMessage,
                    devStack = turn.DevStack,
                    currentPriceUsd = turn.CurrentPriceUsd,
                    historyContent,
                    reply = turn.AssistantMessage,
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }

        if (!ChatSessionService.IsValidSessionId(sessionId))
        {
            return Results.BadRequest(new
            {
                error = "Invalid sessionId. Expected a UUID (e.g. from crypto.randomUUID()).",
            });
        }

        if (!sessions.TryPrepareTurn(sessionId, messages, out var forLlm, out var prepError))
            return Results.BadRequest(new { error = prepError ?? "Invalid request." });

        try
        {
            var turn = await openAi.GetStructuredConsultantTurnAsync(forLlm!, cancellationToken).ConfigureAwait(false);
            var historyContent = ConsultantChatFormatting.BuildHistoryContent(
                turn.AssistantMessage,
                turn.DevStack);
            sessions.AppendAssistant(sessionId, historyContent);
            return Results.Ok(new
            {
                assistantMessage = turn.AssistantMessage,
                devStack = turn.DevStack,
                currentPriceUsd = turn.CurrentPriceUsd,
                historyContent,
                reply = turn.AssistantMessage,
                sessionId,
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });

app.MapPost(
    "/api/pricing/estimate",
    async Task<IResult> (
        PricingEstimateRequest body,
        IOpenAiAgentClient openAi,
        CancellationToken cancellationToken) =>
    {
        static bool TryParseListPriceUsd(string? llmText, out int usd)
        {
            usd = 0;
            var s = (llmText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s))
                return false;

            // Prefer strict JSON.
            try
            {
                using var doc = JsonDocument.Parse(s);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("listPriceUsd", out var p) &&
                    p.ValueKind is JsonValueKind.Number &&
                    p.TryGetInt32(out usd))
                    return true;
            }
            catch
            {
                // fall through
            }

            // Fallback: extract first integer number.
            var m = Regex.Match(s, @"\b(\d{2,9})\b");
            if (!m.Success)
                return false;

            return int.TryParse(m.Groups[1].Value, out usd);
        }

        var locale = string.Equals(body.Locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
        var stackLines = body.StackLines ?? Array.Empty<string>();
        var stackCount = stackLines.Count;

        var system =
            "You are a pricing estimator for US-based software development services.\n"
            + "Given an implementation task stack, you must estimate the US list price in USD.\n"
            + "Return ONLY valid JSON with this exact shape: {\"listPriceUsd\": 1234}.\n"
            + "Rules:\n"
            + "- listPriceUsd must be an integer number of USD.\n"
            + "- Use only the stack size and complexity implied by the items.\n"
            + "- No extra keys, no prose, no formatting, no code blocks.";

        var user =
            "Estimate US list price in USD.\n"
            + $"Stack item count: {stackCount}\n"
            + "Stack items:\n"
            + string.Join("\n", stackLines.Select((s, i) => $"{i + 1}. {s}"));

        int usd;
        try
        {
            var reply = await openAi
                .GetReplyFromConversationAsync(
                    new[]
                    {
                        new OpenAiChatMessage("system", system),
                        new OpenAiChatMessage("user", user),
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (!TryParseListPriceUsd(reply, out usd))
                return Results.Problem("Could not parse listPriceUsd from LLM response.");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }

        usd = Math.Clamp(usd, 100, 250_000);

        if (locale == "en")
            return Results.Ok(new { listPrice = usd, currency = "USD", listPriceUsd = usd });

        const decimal usdToRub = 95m;
        var rub = (int)Math.Round(usd * usdToRub, MidpointRounding.AwayFromZero);
        return Results.Ok(new { listPrice = rub, currency = "RUB", listPriceUsd = usd });
    });

app.Run();

internal sealed record ChatRequest(string? SessionId, IReadOnlyList<ChatRequestMessage>? Messages);

internal sealed record PricingEstimateRequest(string? Locale, IReadOnlyList<string>? StackLines);
