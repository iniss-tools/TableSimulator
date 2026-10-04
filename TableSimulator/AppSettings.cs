using System.Text.Json;
using System.Text.Json.Serialization;

namespace TableSimulator;

/// <summary>
/// Nastavenie servera simulatora (nie liniek a tabul - tie su v <see cref="ConfigStore" />).
/// </summary>
public sealed record AppSettings
{
    /// <summary>Predvoleny port weboveho rozhrania.</summary>
    public const int DefaultHttpPort = 5470;

    /// <summary>Port weboveho rozhrania.</summary>
    public int HttpPort { get; init; } = DefaultHttpPort;

    /// <summary>Webove rozhranie dostupne aj z inych zariadeni v sieti (inak len z tohto pocitaca).</summary>
    public bool AllowNetwork { get; init; }

    /// <summary>Priecinok s pismami ELEN (.bin); prazdny = bez pisiem (priblizne pismo prehliadaca).</summary>
    public string FontsFolder { get; init; } = "";

    /// <summary>Priecinok vlastnych vzhladov; prazdny = <c>%AppData%\TableSimulator\designs</c>.</summary>
    public string DesignsFolder { get; init; } = "";

    /// <summary>Adresa tohto pocitaca z pohladu INISSu (pre <c>TablePort</c>); prazdna = 127.0.0.1.</summary>
    public string ConnectHost { get; init; } = "";

    /// <summary>Priecinok nastaveni - predvolene v profile pouzivatela, parametrom <c>--data</c> inde (prenosna instalacia, testy).</summary>
    [JsonIgnore]
    public static string DataFolder { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TableSimulator");

    /// <summary>Pouzije iny priecinok nastaveni; volat pred <see cref="Load" />.</summary>
    public static void UseDataFolder(string folder) => DataFolder = Path.GetFullPath(folder);

    /// <summary>Priecinok vlastnych vzhladov po doplneni predvoleneho.</summary>
    [JsonIgnore]
    public string EffectiveDesignsFolder => string.IsNullOrWhiteSpace(DesignsFolder) ? Path.Combine(DataFolder, "designs") : DesignsFolder;

    private static string FilePath => Path.Combine(DataFolder, "settings.json");

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    /// <summary>Nacita nastavenie; chybajuci alebo poskodeny subor = predvolene.</summary>
    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json) ?? new AppSettings() : new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Ulozi nastavenie.</summary>
    public void Save()
    {
        Directory.CreateDirectory(DataFolder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }
}
