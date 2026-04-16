using LinguaBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace LinguaBot.Data;

public class LinguaBotDbContext(DbContextOptions<LinguaBotDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledTask>(entity =>
        {
            entity.ToTable("scheduled_tasks");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => new { t.Status, t.ScheduledAt });
            entity.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.TelegramUserId).IsUnique();

            // LanguageLearning stored as a JSONB column — includes Words, Phrases, TargetLanguages.
            entity.OwnsOne(u => u.LanguageLearning, ll =>
            {
                ll.ToJson("language_learning");
                ll.OwnsMany(l => l.Words);
                ll.OwnsMany(l => l.Phrases);
            });

            // DialogState stored as a JSONB column.
            entity.OwnsOne(u => u.DialogState, ds =>
            {
                ds.ToJson("dialog_state");
            });
        });
    }
}
