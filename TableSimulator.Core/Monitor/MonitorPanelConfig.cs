namespace TableSimulator.Core.Monitor;

/// <summary>
/// Konfiguracia panelu ELEN pre program Monitor (<c>Sim\CONFIG\rwp_*.bin</c>, 512 B) - to, co sa do panelu nahra pri
/// vyrobe: rozmer matice, farebnost, hodiny a casy blikania.
/// </summary>
/// <param name="Type">typ panelu (bajt 0)</param>
/// <param name="Rows">pocet riadkov</param>
/// <param name="RowHeight">vyska riadku v bodoch</param>
/// <param name="Width">sirka v bodoch</param>
/// <param name="Color">farebnost (bajt 9): 0 jednofarebny, 1 cervena a zelena, 2 cervena a zelena zamenene</param>
/// <param name="Clock">panel ma hodiny v hlavicke (bajt 27 nenulovy)</param>
/// <param name="FlashOn">ako dlho blikajuci text svieti (bajt 25, jednotky podla Monitora)</param>
/// <param name="FlashOff">ako dlho blikajuci text nesvieti (bajt 26)</param>
/// <param name="Raw">cely obsah suboru</param>
public sealed record MonitorPanelConfig(int Type, int Rows, int RowHeight, int Width, int Color, bool Clock, int FlashOn, int FlashOff, byte[] Raw)
{
    /// <summary>Dlzka suboru.</summary>
    public const int Size = 512;

    /// <summary>Precita konfiguraciu; null = subor nema tvar konfiguracie panelu.</summary>
    public static MonitorPanelConfig? Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != Size || data[1] == 0 || data[2] == 0)
            return null;
        var width = data[3] << 8 | data[4];
        if (width == 0)
            return null;
        return new MonitorPanelConfig(data[0], Math.Min(16, (int)data[1]), Math.Min(16, (int)data[2]), Math.Min(512, width), data[9], data[27] != 0, data[25], data[26], data);
    }

    /// <summary>Nacita konfiguraciu zo suboru; null = chyba alebo zly tvar.</summary>
    public static MonitorPanelConfig? Load(string path)
    {
        try
        {
            return File.Exists(path) ? Read(File.ReadAllBytes(path)) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
