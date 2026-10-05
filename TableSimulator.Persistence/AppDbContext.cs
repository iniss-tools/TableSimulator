using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Virtual;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Persistence;

/// <summary>
/// Databaza simulatora: pouzivatelia a roly (ASP.NET Core Identity), audit a virtualne tabule. Model je nezavisly od poskytovatela - kazdy
/// poskytovatel ma vlastnu odvodenu triedu a vlastne migracie (<see cref="Sqlite.SqliteAppDbContext" />).
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    /// <summary>Definicie virtualnych tabul.</summary>
    public DbSet<VirtualBoard> VirtualBoards => Set<VirtualBoard>();

    /// <summary>Vlastne rozlozenia virtualnych tabul.</summary>
    public DbSet<VirtualLayoutRecord> VirtualLayouts => Set<VirtualLayoutRecord>();

    /// <summary>Audit - kto co urobil.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Pristupove kluce zariadeni a programov.</summary>
    public DbSet<AccessKey> AccessKeys => Set<AccessKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AppUser>(user => user.Property(u => u.DisplayName).HasMaxLength(100));
        builder.Entity<VirtualBoard>(board =>
        {
            board.ToTable("VirtualBoards");
            // cislo zadava obsluha (ako v INISSView) - adresa zobrazenia sa nemeni
            board.HasKey(b => b.Id);
            board.Property(b => b.Id).ValueGeneratedNever();
            board.Property(b => b.Name).HasMaxLength(100);
            board.Property(b => b.Description).HasMaxLength(200);
            board.Property(b => b.Source).HasMaxLength(100);
            board.Property(b => b.Separator).HasMaxLength(20);
            board.Property(b => b.Template).HasMaxLength(100);
            board.Property(b => b.Parameter).HasMaxLength(200);
            board.Property(b => b.Layout).HasMaxLength(40).HasDefaultValue("");
        });
        builder.Entity<AuditEntry>(entry =>
        {
            entry.ToTable("AuditEntries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).ValueGeneratedOnAdd();
            entry.Property(e => e.User).HasMaxLength(64);
            entry.Property(e => e.Action).HasMaxLength(64);
            entry.Property(e => e.Target).HasMaxLength(200);
            entry.Property(e => e.Error).HasMaxLength(100);
            entry.Property(e => e.Details).HasMaxLength(4000);
            entry.Property(e => e.Address).HasMaxLength(64);
            entry.HasIndex(e => e.Time);
        });
        builder.Entity<AccessKey>(key =>
        {
            key.ToTable("AccessKeys");
            key.HasKey(k => k.Id);
            key.Property(k => k.Name).HasMaxLength(100);
            key.Property(k => k.Kind).HasMaxLength(10);
            key.Property(k => k.Role).HasMaxLength(20);
            key.Property(k => k.Prefix).HasMaxLength(20);
            key.Property(k => k.Hash).HasMaxLength(64);
            key.Property(k => k.CreatedBy).HasMaxLength(64);
            key.HasIndex(k => k.Hash).IsUnique();
        });
        builder.Entity<VirtualLayoutRecord>(layout =>
        {
            layout.ToTable("VirtualLayouts");
            layout.HasKey(l => l.Id);
            layout.Property(l => l.Id).HasMaxLength(40);
            layout.Property(l => l.Name).HasMaxLength(100);
        });
    }
}
