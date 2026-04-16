using LinguaBot.Domain;

namespace LinguaBot.Data;

public interface IScheduledTaskRepository
{
    Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all Pending tasks whose ScheduledAt is on or before <paramref name="now"/>.</summary>
    Task<List<ScheduledTask>> GetDueTasksAsync(DateTimeOffset now, CancellationToken ct = default);

    /// <summary>Returns all non-cancelled tasks for a user, ordered by ScheduledAt ascending.</summary>
    Task<List<ScheduledTask>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(ScheduledTask task, CancellationToken ct = default);

    Task UpdateAsync(ScheduledTask task, CancellationToken ct = default);
}
