namespace TableSimulator.Application.Devices;

/// <summary>Uchovavanie historie ustredni.</summary>
public static class DeviceHistoryRetention
{
    /// <summary>Ako dlho sa historia uchovava [dni].</summary>
    public const int Days = 30;

    /// <summary>Najdlhsie obdobie jedneho dotazu [dni].</summary>
    public const int MaxSpanDays = 7;

    /// <summary>Najviac zaznamov logu servera v jednej odpovedi.</summary>
    public const int MaxServerEvents = 5000;

    /// <summary>Najdlhsi zapisany zoznam zvukov hlasenia (znaky).</summary>
    public const int MaxSounds = 4000;

    /// <summary>Najdlhsi zapisany titulok hlasenia (znaky).</summary>
    public const int MaxTitle = 400;

    /// <summary>Najdlhsie meno fronty a okruhu vzdialeneho INISSu v databaze.</summary>
    public const int MaxName = 100;
}
