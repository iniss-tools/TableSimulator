using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Designs;

/// <summary>Chyby a kody chyb vzhladov.</summary>
public static class DesignErrors
{
    public const string Id = "designs.id";
    public const string Name = "designs.name";
    public const string Renderer = "designs.renderer";
    public const string Number = "designs.number";
    public const string Color = "designs.color";
    public const string Font = "designs.font";

    public static Error FolderMissing(string folder) =>
        Error.Problem("designs.folderMissing", $"The designs folder does not exist: {folder}");

    public static Error NotCustom(string id) => Error.NotFound("designs.notCustom", $"There is no custom design '{id}'.");
}
