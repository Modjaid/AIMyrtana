using LinguaBot.Domain;

namespace LinguaBot.Data;

public interface IUserRepository
{
    Task<User?> FindByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default);

    /// <summary>Returns an existing user or creates and persists a new one on first encounter.</summary>
    Task<User> GetOrCreateAsync(long telegramUserId, string? username, string? firstName, CancellationToken ct = default);

    Task SaveAsync(User user, CancellationToken ct = default);
}
