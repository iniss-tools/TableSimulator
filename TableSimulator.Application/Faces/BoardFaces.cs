using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Faces;

/// <summary>
/// Vstavane predlohy ciel tabul (subory <c>Faces/BuiltIn/*.json</c> vlozene do zostavy), prevod vlastnych predloh
/// z databazy a prevod starsej hlavicky tabule na celo.
/// </summary>
public static partial class BoardFaces
{
    private static readonly Lazy<BoardFace[]> Loaded = new(Load);

    /// <summary>Vstavane predlohy podla nazvu.</summary>
    public static IReadOnlyList<BoardFace> BuiltIn => Loaded.Value;

    /// <summary>Vstavana predloha s klucom; null, ak taka nie je.</summary>
    public static BoardFace? FindBuiltIn(string id) => BuiltIn.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Kluc predlohy: male pismena bez diakritiky, cislice a pomlcka, najviac 40 znakov.</summary>
    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    /// <summary>Predloha ako JSON do databazy (ten isty tvar ako v subore konfiguracie).</summary>
    public static string Serialize(BoardFace face) => JsonSerializer.Serialize(face, SimulatorConfigJson.Options);

    /// <summary>Predloha z databazy; kluc a nazov z riadku databazy.</summary>
    public static BoardFace Deserialize(FaceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var face = JsonSerializer.Deserialize<BoardFace>(record.Definition, SimulatorConfigJson.Options) ?? new BoardFace();
        return face with { Id = record.Id, Name = record.Name };
    }

    /// <summary>Obsah predlohy bez kluca a nazvu - rovnake cela maju rovnaky obsah.</summary>
    public static string Content(BoardFace face)
    {
        ArgumentNullException.ThrowIfNull(face);
        return Serialize(face with { Id = "", Name = "" });
    }

    /// <summary>
    /// Celo zo starsej hlavicky tabule: pas cez celu sirku nad maticou, texty a hodiny na tych istych miestach
    /// (polohy od laveho horneho rohu plochy vo vnutri ramu, ako ich kreslila hlavicka).
    /// </summary>
    public static BoardFace FromHeader(BoardHeader header, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(header);
        var elements = new List<FaceElement>
        {
            new() { Kind = FaceElementKind.Rect, Anchor = FaceAnchor.Face, Height = header.Height, Fill = "band" }
        };
        elements.AddRange(header.Texts.Select(t => new FaceElement { Kind = FaceElementKind.Text, X = t.X, Y = t.Y, Size = t.Height, Text = t.Text }));
        if (header.Clock is { } clock)
            elements.Add(new FaceElement { Kind = FaceElementKind.Clock, X = clock.X, Y = clock.Y, Size = clock.Height });
        return new BoardFace { Id = id, Name = name, Margin = new FaceMargin(Top: header.Height), Elements = elements };
    }

    /// <summary>Kluc z textu (napr. mena suboru): male pismena bez diakritiky, ostatne znaky pomlcka.</summary>
    public static string Slug(string text, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        var plain = new string(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        var slug = NotIdChars().Replace(plain.ToLowerInvariant(), "-").Trim('-');
        return slug.Length > maxLength ? slug[..maxLength].TrimEnd('-') : slug;
    }

    private static BoardFace[] Load()
    {
        var assembly = typeof(BoardFaces).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Faces.BuiltIn.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
            .Select(n => Read(assembly, n))
            .OrderBy(f => f.Name, StringComparer.CurrentCulture)
            .ToArray();
    }

    private static BoardFace Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;
        return JsonSerializer.Deserialize<BoardFace>(stream, SimulatorConfigJson.Options)
               ?? throw new InvalidOperationException($"Built-in face {resource} is empty.");
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NotIdChars();
}
