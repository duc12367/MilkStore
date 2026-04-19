

using Microsoft.EntityFrameworkCore;

namespace MilkStore.Models;

public partial class MilkStore4Context
{
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}