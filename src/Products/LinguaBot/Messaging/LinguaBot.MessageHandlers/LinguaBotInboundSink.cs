using LinguaBot.Agent;
using LinguaBot.Data;
using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinguaBot.MessageHandlers;

/// <summary>
/// Terminal handler in the inbound message pipeline.
/// Resolves or creates the user, runs the AI agent, saves state, and sends the reply.
/// </summary>
public sealed class LinguaBotInboundSink(
    IServiceScopeFactory scopeFactory,
    ILanguageTutorAgent agent,
    IOutboundSender outboundSender,
    ILogger<LinguaBotInboundSink> logger) : IInboundMessageSink
{
    public async Task HandleAsync(InboundMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        // Prefer the `from.id` field (individual user); fall back to chat ID for group-chat scenarios.
        long telegramUserId;
        if (message.Metadata is not null &&
            message.Metadata.TryGetValue("telegram:from:id", out var fromIdStr) &&
            long.TryParse(fromIdStr, out var parsedFromId))
        {
            telegramUserId = parsedFromId;
        }
        else if (!long.TryParse(message.ExternalChatId, out telegramUserId))
        {
            logger.LogWarning("Cannot determine Telegram user ID from message chat={Chat}", message.ExternalChatId);
            return;
        }

        var username = message.Metadata?.GetValueOrDefault("telegram:from:username");
        var firstName = message.Metadata?.GetValueOrDefault("telegram:from:first_name");

        // IUserRepository is Scoped (wraps DbContext) — create a scope per message.
        await using var scope = scopeFactory.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetOrCreateAsync(telegramUserId, username, firstName, cancellationToken);

        string reply;
        try
        {
            reply = await agent.ProcessMessageAsync(user, message.Text, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent failed for user {TelegramUserId}", telegramUserId);
            reply = "Что-то пошло не так. Попробуй ещё раз.";
        }

        // Persist any in-memory mutations made by the agent's kernel plugin.
        await userRepository.SaveAsync(user, cancellationToken);

        var outbound = new OutboundMessage(ChannelKind.Telegram, message.ExternalChatId, reply);
        var sendResult = await outboundSender.SendAsync(outbound, cancellationToken);

        if (!sendResult.Ok)
            logger.LogError("Failed to send reply to chat {ChatId}: {Error}", message.ExternalChatId, sendResult.Error);
    }
}
