using AgentForSite.AgentImplementations;
using AgentForSite.Api.Chat;
using AgentForSite.WebAdapter;

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
                var reply = await openAi.GetReplyAsync(lastUser.Content, cancellationToken).ConfigureAwait(false);
                return Results.Ok(new { reply });
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
            var reply = await openAi.GetReplyFromConversationAsync(forLlm!, cancellationToken).ConfigureAwait(false);
            sessions.AppendAssistant(sessionId, reply);
            return Results.Ok(new { reply, sessionId });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });

app.Run();

internal sealed record ChatRequest(string? SessionId, IReadOnlyList<ChatRequestMessage>? Messages);
