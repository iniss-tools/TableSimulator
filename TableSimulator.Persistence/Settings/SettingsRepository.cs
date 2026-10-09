using TableSimulator.Application.Settings;

namespace TableSimulator.Persistence.Settings;

/// <summary>Nastavenie servera v databaze (jeden riadok). Synchronne - cita sa pri starte pred zostavenim servera.</summary>
public sealed class SettingsRepository(AppDbContext db)
{
    /// <summary>Ulozene nastavenie; null, ak databaza este ziadne nema (prvy start).</summary>
    public AppSettings? Load()
    {
        var record = db.ServerSettings.Find(SettingsRecord.SingleId);
        if (record is null)
            return null;
        db.ChangeTracker.Clear();
        return new AppSettings
        {
            HttpPort = record.HttpPort,
            AllowNetwork = record.AllowNetwork,
            RequireLogin = record.RequireLogin,
            FontsFolder = record.FontsFolder,
            DesignsFolder = record.DesignsFolder,
            ConnectHost = record.ConnectHost,
            MonitorFolder = record.MonitorFolder,
            VirtualFolders = record.VirtualFolders.ToArray(),
            InissProgram = record.InissProgram,
            InissViewFolder = record.InissViewFolder,
            InissRegistry = record.InissRegistry,
            InissElevated = record.InissElevated,
            HttpsPort = record.HttpsPort,
            HttpsRedirect = record.HttpsRedirect,
            HttpsCertificate = record.HttpsCertificate,
            HttpsKey = record.HttpsKey,
            HttpsPassword = record.HttpsPassword
        };
    }

    /// <summary>Ulozi nastavenie (prepise jediny riadok).</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var record = db.ServerSettings.Find(SettingsRecord.SingleId);
        if (record is null)
        {
            record = new SettingsRecord();
            db.ServerSettings.Add(record);
        }

        record.HttpPort = settings.HttpPort;
        record.AllowNetwork = settings.AllowNetwork;
        record.RequireLogin = settings.RequireLogin;
        record.FontsFolder = settings.FontsFolder;
        record.DesignsFolder = settings.DesignsFolder;
        record.ConnectHost = settings.ConnectHost;
        record.MonitorFolder = settings.MonitorFolder;
        record.VirtualFolders = settings.VirtualFolders.ToList();
        record.InissProgram = settings.InissProgram;
        record.InissViewFolder = settings.InissViewFolder;
        record.InissRegistry = settings.InissRegistry;
        record.InissElevated = settings.InissElevated;
        record.HttpsPort = settings.HttpsPort;
        record.HttpsRedirect = settings.HttpsRedirect;
        record.HttpsCertificate = settings.HttpsCertificate;
        record.HttpsKey = settings.HttpsKey;
        record.HttpsPassword = settings.HttpsPassword;
        db.SaveChanges();
        db.ChangeTracker.Clear();
    }
}
