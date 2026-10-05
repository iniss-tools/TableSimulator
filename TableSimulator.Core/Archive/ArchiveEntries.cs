namespace TableSimulator.Core.Archive;

/// <summary>Druh denneho archivu INISSu.</summary>
public enum ArchiveKind
{
    /// <summary>Obsah tabul (<c>TAB&lt;datum&gt;.XML</c>, zaznamy <c>&lt;T&gt;</c>).</summary>
    Tables,

    /// <summary>Hlasenia a udalosti (<c>REP&lt;datum&gt;.XML</c>, zaznamy <c>&lt;H&gt;</c>).</summary>
    Reports
}

/// <summary>Vlak zaznamu archivu.</summary>
/// <param name="Type">druh (<c>t</c>)</param>
/// <param name="Number">cislo (<c>v</c>)</param>
/// <param name="Name">nazov (<c>jm</c>)</param>
/// <param name="Date">datum vlaku (<c>dt</c>), ak sa lisi od dna zaznamu</param>
public sealed record ArchiveTrain(string Type, string Number, string Name, DateOnly? Date);

/// <summary>Stlpec tabule v zazname - popis stlpca z katalogovej tabule a text (prazdny, ak bol stlpec prazdny).</summary>
public sealed record TableArchiveColumn(string Name, string Value);

/// <summary>
/// Skupina fyzickych tabul s rovnakym obsahom: ich nazvy a stlpce. Pri zazname informacneho textu ma logicku tabulu
/// (<c>lt</c>) a riadok (<c>r</c>) a stlpce nema.
/// </summary>
public sealed record TableArchiveTable(IReadOnlyList<string> Names, IReadOnlyList<TableArchiveColumn> Columns, string Logical, string Row);

/// <summary>Zaznam archivu tabul - co bolo na tabuliach o vlaku (alebo informacny text) od casu zaznamu.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Station">stanica (INISS 3.34 ju nezapisoval)</param>
/// <param name="Train">vlak; null pri informacnom texte</param>
/// <param name="Text">informacny text (<c>tx</c>); prazdny pri zazname vlaku alebo ked text zmizol</param>
/// <param name="Tables">tabule</param>
public sealed record TableArchiveEntry(TimeOnly Time, string Station, ArchiveTrain? Train, string Text, IReadOnlyList<TableArchiveTable> Tables);

/// <summary>Zaznam archivu hlaseni - hlasenie, zmena stavu vlaku, prihlasenie obsluhy a ine udalosti.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Kind">druh udalosti (<c>u</c>) - text INISSu v jazyku instalacie (hlásenie, stav, prihlásenie…)</param>
/// <param name="Name">nazov hlasenia alebo udalosti (<c>n</c>)</param>
/// <param name="Auto">spustila ho automatika (<c>a</c>)</param>
/// <param name="Station">stanica</param>
/// <param name="Train">vlak; null, ak sa udalost vlaku netyka</param>
/// <param name="ArrivalTrack">kolaj prichodu (<c>kp</c>)</param>
/// <param name="DepartureTrack">kolaj odchodu (<c>ko</c>), ak sa lisi</param>
/// <param name="ArrivalDelay">posun prichodu (predpokladane meskanie) v minutach (<c>zp</c>)</param>
/// <param name="DepartureDelay">posun odchodu v minutach (<c>zo</c>), ak sa lisi</param>
/// <param name="Text">text hlasenia (<c>x</c>)</param>
/// <param name="Result">vysledok (<c>e</c>): <c>OK</c>, <c>break</c> (prerusene), <c>deleted</c>…</param>
/// <param name="Details">ostatne atributy (vyluka a odklon s textom z legendy, meskanie <c>zcp</c>/<c>zco</c>, priznaky…)</param>
public sealed record ReportArchiveEntry(
    TimeOnly Time,
    string Kind,
    string Name,
    bool Auto,
    string Station,
    ArchiveTrain? Train,
    string ArrivalTrack,
    string DepartureTrack,
    int? ArrivalDelay,
    int? DepartureDelay,
    string Text,
    string Result,
    IReadOnlyList<KeyValuePair<string, string>> Details);

/// <summary>Precitany denny archiv.</summary>
/// <param name="Tables">zaznamy archivu tabul (pri archive hlaseni prazdne)</param>
/// <param name="Reports">zaznamy archivu hlaseni (pri archive tabul prazdne)</param>
/// <param name="Skipped">riadky, ktore sa nedali precitat (poskodene alebo prave zapisovane)</param>
public sealed record ArchiveContent(IReadOnlyList<TableArchiveEntry> Tables, IReadOnlyList<ReportArchiveEntry> Reports, int Skipped);
