using Microsoft.EntityFrameworkCore;
using MyOwnDb.Entities;

namespace MyOwnDb;

public sealed class MyOwnDbContext : DbContext
{
    public MyOwnDbContext(DbContextOptions<MyOwnDbContext> options) : base(options) { }

    public DbSet<KeyValueEntry> KeyValueEntries => Set<KeyValueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KeyValueEntry>(e =>
        {
            e.ToTable("key_value_entries");
            e.HasKey(x => x.Id);

            e.Property(x => x.Key)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Value)
                .HasMaxLength(4000)
                .IsRequired(false);

            e.HasIndex(x => x.Key).IsUnique();
        });
    }
}

