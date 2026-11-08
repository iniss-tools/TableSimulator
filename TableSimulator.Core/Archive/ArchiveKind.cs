namespace TableSimulator.Core.Archive;

/// <summary>Druh denneho archivu INISSu.</summary>
public enum ArchiveKind
{
    /// <summary>Obsah tabul (<c>TAB&lt;datum&gt;.XML</c>, zaznamy <c>&lt;T&gt;</c>).</summary>
    Tables,

    /// <summary>Hlasenia a udalosti (<c>REP&lt;datum&gt;.XML</c>, zaznamy <c>&lt;H&gt;</c>).</summary>
    Reports
}
