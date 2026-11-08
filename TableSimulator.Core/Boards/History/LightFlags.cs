namespace TableSimulator.Core.Boards.History;

/// <summary>Stav svetla ustredne AK Signal ako bity (zaznam historie v databaze).</summary>
[Flags]
public enum LightFlags
{
    /// <summary>Nic - svetlo nesvieti, rezim vypnute, den.</summary>
    None = 0,

    /// <summary>Automaticke svietenie.</summary>
    Auto = 0x01,

    /// <summary>Rucne svietenie.</summary>
    Manual = 0x02,

    /// <summary>Snimac hlasi tmu.</summary>
    Dark = 0x04,

    /// <summary>Svetla svietia.</summary>
    On = 0x08,

    /// <summary>Porucha svetiel.</summary>
    Fault = 0x10,

    /// <summary>Server sa ustredne dlhsie nepyta.</summary>
    Silent = 0x20,

    /// <summary>Ustredna bez napajania (scenar) - nic neprijima ani neodpoveda.</summary>
    PoweredOff = 0x40
}
