using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Scenarios;
using TableSimulator.Application.Signage;
using TableSimulator.Application.Statistics;
using TableSimulator.Application.Virtual;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;
using TableSimulator.Persistence.Identity;
using TableSimulator.Persistence.Settings;
using TableSimulator.Persistence.Simulation;

namespace TableSimulator.Persistence;

/// <summary>
/// Databaza simulatora: nastavenie servera, linky a tabule, pouzivatelia a roly (ASP.NET Core Identity), pristupove
/// kluce, audit, virtualne tabule, informacne panely, scenare porucha a statistika komunikacie. Model je nezavisly od poskytovatela - kazdy
/// poskytovatel ma vlastnu odvodenu triedu a vlastne migracie (<see cref="Sqlite.SqliteAppDbContext" />).
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    /// <summary>Nastavenie servera (jeden riadok).</summary>
    public DbSet<SettingsRecord> ServerSettings => Set<SettingsRecord>();

    /// <summary>Linky simulatora.</summary>
    public DbSet<LineRecord> Lines => Set<LineRecord>();

    /// <summary>Tabule simulatora.</summary>
    public DbSet<BoardRecord> Boards => Set<BoardRecord>();

    /// <summary>Definicie virtualnych tabul.</summary>
    public DbSet<VirtualBoard> VirtualBoards => Set<VirtualBoard>();

    /// <summary>Vlastne rozlozenia virtualnych tabul.</summary>
    public DbSet<VirtualLayoutRecord> VirtualLayouts => Set<VirtualLayoutRecord>();

    /// <summary>Audit - kto co urobil.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Pristupove kluce zariadeni a programov.</summary>
    public DbSet<AccessKey> AccessKeys => Set<AccessKey>();

    /// <summary>Spravy informacnych panelov.</summary>
    public DbSet<SignageMessageRecord> SignageMessages => Set<SignageMessageRecord>();

    /// <summary>Playlisty informacnych panelov.</summary>
    public DbSet<SignagePlaylistRecord> SignagePlaylists => Set<SignagePlaylistRecord>();

    /// <summary>Skupiny informacnych panelov.</summary>
    public DbSet<SignageGroupRecord> SignageGroups => Set<SignageGroupRecord>();

    /// <summary>Informacne panely (monitory).</summary>
    public DbSet<SignagePanelRecord> SignagePanels => Set<SignagePanelRecord>();

    /// <summary>Nahrate obrazky a videa sprav.</summary>
    public DbSet<SignageMediaRecord> SignageMedia => Set<SignageMediaRecord>();

    /// <summary>Minutova statistika komunikacie liniek.</summary>
    public DbSet<LineTrafficRecord> LineTraffic => Set<LineTrafficRecord>();

    /// <summary>Minutova statistika komunikacie tabul.</summary>
    public DbSet<BoardTrafficRecord> BoardTraffic => Set<BoardTrafficRecord>();

    /// <summary>Zmeny stavu liniek (pripojenie, odpojenie, vypadok).</summary>
    public DbSet<LineStatusRecord> LineStatuses => Set<LineStatusRecord>();

    /// <summary>Scenare porucha.</summary>
    public DbSet<FaultScenarioRecord> FaultScenarios => Set<FaultScenarioRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AppUser>(user => user.Property(u => u.DisplayName).HasMaxLength(100));
        builder.Entity<AppRole>(role => role.Property(r => r.DisplayName).HasMaxLength(TableSimulator.Application.Auth.Roles.MaxDisplayName));
        builder.Entity<SettingsRecord>(settings =>
        {
            settings.ToTable("ServerSettings");
            settings.HasKey(s => s.Id);
            settings.Property(s => s.Id).ValueGeneratedNever();
            settings.Property(s => s.FontsFolder).HasMaxLength(500);
            settings.Property(s => s.DesignsFolder).HasMaxLength(500);
            settings.Property(s => s.ConnectHost).HasMaxLength(200);
            settings.Property(s => s.MonitorFolder).HasMaxLength(500);
            settings.Property(s => s.InissProgram).HasMaxLength(500);
            settings.Property(s => s.InissViewFolder).HasMaxLength(500).HasDefaultValue("");
            settings.Property(s => s.InissRegistry).HasMaxLength(100);
            settings.Property(s => s.HttpsCertificate).HasMaxLength(500);
            settings.Property(s => s.HttpsKey).HasMaxLength(500);
            settings.Property(s => s.HttpsPassword).HasMaxLength(2000);
            settings.Property(s => s.Alerts).HasMaxLength(8000).HasDefaultValue("");
        });
        // enumy jadra ako kluce z JSON (display, noReply…), nie mena C#
        builder.Entity<LineRecord>(line =>
        {
            line.ToTable("Lines");
            line.HasKey(l => l.Number);
            line.Property(l => l.Number).ValueGeneratedNever();
            line.Property(l => l.Endpoint).HasMaxLength(200);
            line.Property(l => l.Name).HasMaxLength(100);
            line.Property(l => l.Mode).HasMaxLength(20).HasConversion(m => SimulatorConfigJson.Key(m), k => SimulatorConfigJson.Parse<LineMode>(k));
            line.Property(l => l.Manufacturer).HasMaxLength(40);
            line.Property(l => l.Mirror).HasMaxLength(100);
            line.HasMany(l => l.Boards).WithOne().HasForeignKey(b => b.Line).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<BoardRecord>(board =>
        {
            board.ToTable("Boards");
            board.HasKey(b => new { b.Line, b.Address });
            board.Property(b => b.Address).ValueGeneratedNever();
            board.Property(b => b.Manufacturer).HasMaxLength(40);
            board.Property(b => b.Name).HasMaxLength(100);
            board.Property(b => b.Profile).HasConversion(p => ProfileJson.Write(p), j => ProfileJson.Read(j));
            board.Property(b => b.Design).HasMaxLength(100);
            board.Property(b => b.FaultKind).HasMaxLength(20).HasConversion(f => SimulatorConfigJson.Key(f), k => SimulatorConfigJson.Parse<FaultKind>(k));
        });
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
        // statistika: minuta od 1. 1. 1970 UTC ako cislo - porovnava sa a zoskupuje v kazdej databaze rovnako
        builder.Entity<LineTrafficRecord>(traffic =>
        {
            traffic.ToTable("LineTraffic");
            traffic.HasKey(t => new { t.Line, t.Minute });
            traffic.HasIndex(t => t.Minute);
        });
        builder.Entity<BoardTrafficRecord>(traffic =>
        {
            traffic.ToTable("BoardTraffic");
            traffic.HasKey(t => new { t.Line, t.Address, t.Minute });
            traffic.HasIndex(t => t.Minute);
        });
        builder.Entity<LineStatusRecord>(status =>
        {
            status.ToTable("LineStatusChanges");
            status.HasKey(s => s.Id);
            status.Property(s => s.Id).ValueGeneratedOnAdd();
            status.Property(s => s.Status).HasMaxLength(20);
            status.Property(s => s.Detail).HasMaxLength(200);
            // zmien stavu je malo (desiatky za den) - staci index casu
            status.HasIndex(s => s.TimeMs);
        });
        // kroky scenara su JSON ako v API - scenarov je malo a citaju sa vzdy cele
        builder.Entity<FaultScenarioRecord>(scenario =>
        {
            scenario.ToTable("FaultScenarios");
            scenario.HasKey(s => s.Id);
            scenario.Property(s => s.Id).ValueGeneratedOnAdd();
            scenario.Property(s => s.Name).HasMaxLength(100);
        });
        // obsah sprav, playlistov a skupin je JSON ako v API - je ich malo a citaju sa vzdy vsetky
        builder.Entity<SignageMessageRecord>(message =>
        {
            message.ToTable("SignageMessages");
            message.HasKey(m => m.Id);
            message.Property(m => m.Id).ValueGeneratedOnAdd();
            message.Property(m => m.Name).HasMaxLength(100);
        });
        builder.Entity<SignagePlaylistRecord>(playlist =>
        {
            playlist.ToTable("SignagePlaylists");
            playlist.HasKey(p => p.Id);
            playlist.Property(p => p.Id).ValueGeneratedOnAdd();
            playlist.Property(p => p.Name).HasMaxLength(100);
        });
        builder.Entity<SignageGroupRecord>(group =>
        {
            group.ToTable("SignageGroups");
            group.HasKey(g => g.Id);
            group.Property(g => g.Id).ValueGeneratedOnAdd();
            group.Property(g => g.Name).HasMaxLength(100);
        });
        builder.Entity<SignagePanelRecord>(panel =>
        {
            panel.ToTable("SignagePanels");
            panel.HasKey(p => p.Id);
            panel.Property(p => p.Id).ValueGeneratedOnAdd();
            panel.Property(p => p.Name).HasMaxLength(100);
            panel.Property(p => p.Note).HasMaxLength(500);
        });
        builder.Entity<SignageMediaRecord>(media =>
        {
            media.ToTable("SignageMedia");
            media.HasKey(m => m.Id);
            media.Property(m => m.Name).HasMaxLength(200);
            media.Property(m => m.ContentType).HasMaxLength(50);
            media.Property(m => m.FileName).HasMaxLength(60);
        });
    }
}
