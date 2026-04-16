using LinguaBot.Domain;

namespace LinguaBot.Agent;

public interface ILanguageTutorAgent
{
    /// <summary>
    /// Processes an inbound user message and returns the agent's text reply.
    /// Any modifications to <paramref name="user"/> (added words, status changes, etc.)
    /// are applied in-memory; the caller is responsible for persisting the user.
    /// </summary>
    Task<string> ProcessMessageAsync(User user, string userMessage, CancellationToken ct = default);
}
