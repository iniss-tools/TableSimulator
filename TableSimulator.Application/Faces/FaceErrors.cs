using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Faces;

/// <summary>Chyby a kody chyb predloh ciel tabul.</summary>
public static class FaceErrors
{
    public const string Id = "face.id";
    public const string Name = "face.name";
    public const string Margin = "face.margin";
    public const string Rows = "face.rows";
    public const string Elements = "face.elements";
    public const string Element = "face.element";
    public const string Image = "face.image";

    /// <summary>Najviac prvkov cela.</summary>
    public const int MaxElements = 200;

    /// <summary>Najdlhsi obrazok (data URL) - asi 300 kB suboru.</summary>
    public const int MaxImage = 400_000;

    public static Error BuiltIn(string id) => Error.Problem("face.builtIn", $"Face '{id}' is built in and cannot be changed.");

    public static Error NotFound(string id) => Error.NotFound("face.notFound", $"Face '{id}' does not exist.");

    public static Error InUse(string id, IEnumerable<string> boards) =>
        Error.Problem("face.inUse", $"Face '{id}' is used by boards {string.Join(", ", boards)}.");
}
