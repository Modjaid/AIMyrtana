using LinguaBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace LinguaBot.Data;

public sealed class UserRepository(LinguaBotDbContext db) : IUserRepository
{
    public Task<User?> FindByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

    public async Task<User> GetOrCreateAsync(long telegramUserId, string? username, string? firstName, CancellationToken ct = default)
    {
        var existing = await FindByTelegramUserIdAsync(telegramUserId, ct);
        if (existing is not null)
            return existing;

        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramUsername = username,
            TelegramFirstName = firstName,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task SaveAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
