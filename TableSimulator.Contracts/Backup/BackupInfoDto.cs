namespace TableSimulator.Contracts.Backup;

/// <summary>Zaloha simulatora - co obsahuje.</summary>
/// <param name="Created">kedy vznikla</param>
/// <param name="ServerVersion">verzia servera, ktory ju vytvoril</param>
/// <param name="Designs">pocet vlastnych vzhladov</param>
/// <param name="Media">pocet suborov informacnych panelov</param>
/// <param name="Recordings">pocet zaznamov komunikacie; null = zaloha zaznamy neobsahuje</param>
/// <param name="Lines">pocet liniek v databaze zalohy; null = nezistene (starsia obnova)</param>
/// <param name="Boards">pocet tabul v databaze zalohy; null = nezistene (starsia obnova)</param>
/// <param name="Machine">pocitac, na ktorom zaloha vznikla; null = starsia zaloha</param>
/// <param name="DataFolder">priecinok nastaveni servera, z ktoreho zaloha vznikla; null = starsia zaloha</param>
/// <param name="Service">zalohovany server bezal ako sluzba Windows; null = starsia zaloha</param>
/// <param name="SameServer">zaloha vznikla na tomto pocitaci z tohto priecinka nastaveni</param>
public sealed record BackupInfoDto(DateTimeOffset Created, string ServerVersion, int Designs, int Media, int? Recordings, int? Lines, int? Boards,
    string? Machine, string? DataFolder, bool? Service, bool SameServer);
