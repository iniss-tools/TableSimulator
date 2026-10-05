using System.Text.Json.Serialization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>
/// Virtualne tabule z definicii tabul v subore <c>Web.config</c> INISSView (cislo, nazov, popis, subor XML, riadky,
/// oddelovac; sablona XSL a parameter <c>x</c> sa ulozia na neskor).
/// </summary>
/// <param name="Content">obsah suboru</param>
/// <param name="Replace">tabulu s rovnakym cislom prepisat; inak ostane</param>
public sealed record ImportInissViewCommand([property: JsonIgnore] string Content, bool Replace) : ICommand<InissViewImportedDto>;
