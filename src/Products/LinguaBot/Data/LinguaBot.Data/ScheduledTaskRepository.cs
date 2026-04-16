using LinguaBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace LinguaBot.Data;

public sealed class ScheduledTaskRepository(LinguaBotDbContext db) : IScheduledTaskRepository
{
    public Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ScheduledTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<ScheduledTask>> GetDueTasksAsync(DateTimeOffset now, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Status == ScheduledTaskStatus.Pending && t.ScheduledAt <= now)
            .ToListAsync(ct);

    public Task<List<ScheduledTask>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.UserId == userId && t.Status != ScheduledTaskStatus.Cancelled)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync(ct);

    public async Task AddAsync(ScheduledTask task, CancellationToken ct = default)
    {
        db.ScheduledTasks.Add(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ScheduledTask task, CancellationToken ct = default)
    {
        db.ScheduledTasks.Update(task);
        await db.SaveChangesAsync(ct);
    }
}
