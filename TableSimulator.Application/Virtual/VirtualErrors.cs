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
    public const string Layout = "virtual.layout";
    public const string LayoutId = "virtual.layoutId";
    public const string LayoutName = "virtual.layoutName";
    public const string LayoutTitle = "virtual.layoutTitle";
    public const string LayoutRows = "virtual.layoutRows";
    public const string LayoutFont = "virtual.layoutFont";
    public const string LayoutColor = "virtual.layoutColor";
    public const string LayoutColumn = "virtual.layoutColumn";
    public const string LayoutTiming = "virtual.layoutTiming";

    /// <summary>Najviac stlpcov rozlozenia.</summary>
    public const int MaxColumns = 16;

    /// <summary>Najvyssie cislo tabule.</summary>
    public const int MaxId = 99999;

    /// <summary>Najvyssi pocet a poradie zaznamov.</summary>
    public const int MaxRows = 999;

    public static Error NotFound(int id) =>
        Error.NotFound("virtual.notFound", string.Create(CultureInfo.InvariantCulture, $"Virtual board {id} does not exist."));

    public static Error InvalidConfig(string message) => Error.Problem("virtual.invalidConfig", $"Web.config is not valid XML: {message}");

    public static Error NoTables() => Error.Problem("virtual.noTables", "Web.config defines no tables.");

    public static Error LayoutNotFound(string id) => Error.NotFound("virtual.layoutNotFound", $"Layout '{id}' does not exist.");

    public static Error LayoutBuiltIn(string id) => Error.Conflict("virtual.layoutBuiltIn", $"Layout '{id}' is built in and cannot be changed.");

    public static Error LayoutInUse(string id, IEnumerable<int> boards) =>
        Error.Conflict("virtual.layoutInUse", $"Layout '{id}' is used by boards {string.Join(", ", boards)}.");
}
