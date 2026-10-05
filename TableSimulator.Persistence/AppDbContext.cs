using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Virtual;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Persistence;

/// <summary>
/// Databaza simulatora: pouzivatelia a roly (ASP.NET Core Identity) a definicie virtualnych tabul. Model je nezavisly od poskytovatela - kazdy
/// poskytovatel ma vlastnu odvodenu triedu a vlastne migracie (<see cref="Sqlite.SqliteAppDbContext" />).
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    /// <summary>Definicie virtualnych tabul.</summary>
    public DbSet<VirtualBoard> VirtualBoards => Set<VirtualBoard>();

    /// <summary>Vlastne rozlozenia virtualnych tabul.</summary>
    public DbSet<VirtualLayoutRecord> VirtualLayouts => Set<VirtualLayoutRecord>();

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
        builder.Entity<VirtualLayoutRecord>(layout =>
        {
            layout.ToTable("VirtualLayouts");
            layout.HasKey(l => l.Id);
            layout.Property(l => l.Id).HasMaxLength(40);
            layout.Property(l => l.Name).HasMaxLength(100);
        });
    }
}
