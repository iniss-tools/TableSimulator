using System.Text.Json.Serialization;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Audit;
using TableSimulator.Contracts.Config;

namespace TableSimulator.Application.Config;

/// <summary>
/// Linky a tabule zo suboru exportu alebo povodneho <c>config.json</c>: chybajuce sa pridaju (ako zo zaznamu), alebo
/// nahradia vsetky.
/// </summary>
/// <param name="Content">obsah suboru</param>
/// <param name="Replace">nahradit vsetky linky a tabule</param>
public sealed record ImportConfigCommand([property: JsonIgnore] string Content, bool Replace) : ICommand<ConfigImportedDto>, IAuditable
{
    string IAuditable.AuditTarget => Replace ? "replace" : "add";
}
