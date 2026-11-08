using System.Globalization;

namespace TableSimulator.Core.Boards.RemoteAudio;

/// <summary>
/// Nahravka zo zoznamu <c>RWAV</c> vzdialeneho INISSu: odkaz do banky <c>jazyk/skupina/zvuk</c> a pauzy pred a po
/// nahravke [ms]. Viz docs/iniss/protokoly/vzdialeny-iniss.mdx.
/// </summary>
/// <param name="Name">odkaz do banky (kluc jazyka, skupiny a zvuku)</param>
/// <param name="PauseBefore">ticho pred nahravkou [ms]</param>
/// <param name="PauseAfter">ticho po nahravke [ms]</param>
public sealed record RemoteSound(string Name, int PauseBefore, int PauseAfter)
{
    /// <summary>
    /// Zoznam <c>RWAV</c>: trojice <c>odkaz,pauza pred,pauza po</c> oddelene ciarkou. Cislo, ktore sa neda precitat, je
    /// 0 (ako vo vzdialenom INISSe); prazdny odkaz sa vynecha.
    /// </summary>
    public static IReadOnlyList<RemoteSound> Parse(string recordings)
    {
        ArgumentNullException.ThrowIfNull(recordings);
        var parts = recordings.Split(',');
        var sounds = new List<RemoteSound>();
        for (var i = 0; i < parts.Length; i += 3)
        {
            var name = parts[i].Trim();
            if (name.Length > 0)
                sounds.Add(new RemoteSound(name, Number(parts, i + 1), Number(parts, i + 2)));
        }

        return sounds;
    }

    /// <summary>
    /// Dlzka hlasenia: sucet nahravok s pauzami. Dlzku nahravky da <paramref name="length" />; nezname nahravky maju
    /// <paramref name="fallback" />.
    /// </summary>
    public static TimeSpan Duration(IEnumerable<RemoteSound> sounds, Func<string, TimeSpan?>? length, TimeSpan fallback)
    {
        ArgumentNullException.ThrowIfNull(sounds);
        var total = TimeSpan.Zero;
        foreach (var sound in sounds)
        {
            var one = TimeSpan.FromMilliseconds(sound.PauseBefore + sound.PauseAfter) + (length?.Invoke(sound.Name) ?? fallback);
            if (one > TimeSpan.Zero)
                total += one;
        }

        return total;
    }

    private static int Number(string[] parts, int index) =>
        index < parts.Length && int.TryParse(parts[index].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
