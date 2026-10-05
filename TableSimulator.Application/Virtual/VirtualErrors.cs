using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Virtual;

/// <summary>Chyby a kody chyb virtualnych tabul.</summary>
public static class VirtualErrors
{
    public const string Id = "virtual.id";
    public const string Name = "virtual.name";
    public const string Description = "virtual.description";
    public const string Source = "virtual.source";
    public const string Rows = "virtual.rows";
    public const string Separator = "virtual.separator";
    public const string Template = "virtual.template";
    public const string Parameter = "virtual.parameter";
    public const string Content = "virtual.content";

    /// <summary>Najvyssie cislo tabule.</summary>
    public const int MaxId = 99999;

    /// <summary>Najvyssi pocet a poradie zaznamov.</summary>
    public const int MaxRows = 999;

    public static Error NotFound(int id) =>
        Error.NotFound("virtual.notFound", string.Create(CultureInfo.InvariantCulture, $"Virtual board {id} does not exist."));

    public static Error InvalidConfig(string message) => Error.Problem("virtual.invalidConfig", $"Web.config is not valid XML: {message}");

    public static Error NoTables() => Error.Problem("virtual.noTables", "Web.config defines no tables.");
}
