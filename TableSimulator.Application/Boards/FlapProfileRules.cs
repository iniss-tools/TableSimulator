using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Rozumne medze modulov a zoznamov listov listovej tabule (profil tabule aj import stanice).</summary>
public static class FlapProfileRules
{
    public const int MaxModules = 1024;
    public const int MaxLists = 128;
    public const int MaxListEntries = 2048;
    public const int MaxCodeLength = 16;
    public const int MaxTextLength = 200;

    /// <summary>Moduly a zoznamy su v medziach; null (bez listov) je v poriadku.</summary>
    public static bool IsValid(FlapProfileDto? flaps) =>
        flaps is null ||
        (flaps.LinesPerRecord is >= 1 and <= 16 &&
         flaps.Modules.Length <= MaxModules &&
         flaps.Modules.All(m => m is { Line: >= 0 and < 16, Position: >= 0 and < 512, Span: >= 1 and <= 64 } && m.List is null or { Length: <= 100 }) &&
         flaps.Lists.Count <= MaxLists &&
         flaps.Lists.All(l => l.Key.Length <= 100 && l.Value.Count <= MaxListEntries &&
                              l.Value.All(e => e.Key.Length is >= 1 and <= MaxCodeLength && e.Value.Length <= MaxTextLength)) &&
         flaps.Colors.Count <= MaxLists &&
         flaps.Colors.All(l => l.Key.Length <= 100 && l.Value.Count <= MaxListEntries &&
                               l.Value.All(e => e.Key.Length is >= 1 and <= MaxCodeLength && Core.Boards.FlapColors.All.Contains(e.Value))));
}
