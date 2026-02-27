using Microsoft.EntityFrameworkCore;
using TKG.KenpinApp.Web.Models;

namespace TKG.KenpinApp.Web.Data;

/// <summary>
/// 検品アプリケーション用DbContext
/// </summary>
public class KenpinDbContext : DbContext
{
    public KenpinDbContext(DbContextOptions<KenpinDbContext> options) : base(options)
    {
    }

    public DbSet<KenpinSession> KenpinSessions { get; set; } = null!;
    public DbSet<KenpinDetail> KenpinDetails { get; set; } = null!;
    public DbSet<KenpinLock> KenpinLocks { get; set; } = null!;
    public DbSet<DenpyoKenpin> DenpyoKenpins { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;
    public DbSet<AppLog> AppLogs { get; set; } = null!;
    public DbSet<D365SyncQueue> D365SyncQueues { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // T_KENPIN_SESSION
        modelBuilder.Entity<KenpinSession>(entity =>
        {
            entity.HasIndex(e => e.SeiriNo);
            entity.HasIndex(e => e.Status);
        });

        // T_KENPIN_DETAIL
        modelBuilder.Entity<KenpinDetail>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Barcode);
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Details)
                  .HasForeignKey(e => e.SessionId);
        });

        // T_KENPIN_LOCK
        modelBuilder.Entity<KenpinLock>(entity =>
        {
            entity.HasIndex(e => new { e.SeiriNo, e.ShukkaDate });
        });

        // T_DENPYO_KENPIN
        modelBuilder.Entity<DenpyoKenpin>(entity =>
        {
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.DenpyoKenpins)
                  .HasForeignKey(e => e.SessionId);
        });

        // T_USER_SESSION
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserCode);
        });

        // T_APP_LOG
        modelBuilder.Entity<AppLog>(entity =>
        {
            entity.HasIndex(e => e.LogDatetime);
            entity.HasIndex(e => e.UserCode);
        });

        // T_D365_SYNC_QUEUE
        modelBuilder.Entity<D365SyncQueue>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryAt);
            entity.HasOne(e => e.Session)
                  .WithMany()
                  .HasForeignKey(e => e.SessionId);
        });
    }
}
