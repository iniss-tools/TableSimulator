namespace TableSimulator.Core.Boards;

/// <summary>
/// Spinacia jednotka ELSVO: nie tabula, ale zosilnovac rozhlasoveho okruhu, ktory INISS pred hlasenim zopne a po nom
/// uvolni. Obsah = ci je zopnuty a v ktorom pasme, od kedy a kolkokrat.
/// </summary>
public sealed class AmplifierDisplay : BoardDisplay
{
    /// <summary>Bit stavu: jednotka pripravena.</summary>
    public const int Ready = 0x80;

    /// <summary>Bit stavu: zosilnovac zopnuty pre hlasenie.</summary>
    public const int Announcement = 0x10;

    /// <summary>Bit stavu: chyba prenosu.</summary>
    public const int TransferError = 0x20;

    /// <summary>Pasmo, v ktorom je zosilnovac zopnuty: 1 (prikaz 1F), 2 (3F); 0 = vypnuty.</summary>
    public int Band { get; private set; }

    /// <summary>Od kedy je zopnuty; null = vypnuty.</summary>
    public DateTimeOffset? OnSince { get; private set; }

    /// <summary>Kolkokrat INISS zosilnovac zopol (z vypnuteho stavu).</summary>
    public int Switches { get; private set; }

    /// <summary>Posledny prikaz INISSu okrem dotazu; null = ziadny.</summary>
    public byte? LastCommand { get; private set; }

    /// <summary>Stav pre odpoved: pripravena a pri zopnuti bit hlasenia.</summary>
    public byte State => (byte)(Ready | (Band > 0 ? Announcement : 0));

    /// <summary>Zopne zosilnovac v pasme 1 alebo 2; vrati true, ak sa stav zmenil.</summary>
    public bool SwitchOn(byte command, int band, DateTimeOffset now)
    {
        LastCommand = command;
        if (Band == band)
            return false;
        if (Band == 0)
        {
            OnSince = now;
            Switches++;
        }

        Band = band;
        return true;
    }

    /// <summary>Uvolni alebo vypne zosilnovac; vrati true, ak bol zopnuty.</summary>
    public bool SwitchOff(byte command)
    {
        var changed = Band != 0 || LastCommand != command;
        LastCommand = command;
        Band = 0;
        OnSince = null;
        return changed;
    }

    public override void Clear()
    {
        Band = 0;
        OnSince = null;
        LastCommand = null;
        Switches = 0;
    }
}
