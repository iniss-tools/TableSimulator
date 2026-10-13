namespace TableSimulator.Contracts.Designs;

/// <summary>Vzhlad tabule s celym obsahom <c>design.json</c>.</summary>
/// <param name="Id">nazov priecinka vzhladu</param>
/// <param name="BuiltIn">vstavany - zmena ulozi vlastny s rovnakym id, ktory ho prekryje</param>
/// <param name="Overrides">vlastny vzhlad prekryva vstavany s rovnakym id</param>
public sealed record DesignDto(string Id, bool BuiltIn, bool Overrides, SetDesignRequest Design);
