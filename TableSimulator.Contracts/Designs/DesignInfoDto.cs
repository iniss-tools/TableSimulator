namespace TableSimulator.Contracts.Designs;

/// <summary>Vzhlad tabule v zozname.</summary>
/// <param name="Id">nazov priecinka vzhladu</param>
/// <param name="BuiltIn">vstavany (inak vlastny z priecinka vzhladov)</param>
public sealed record DesignInfoDto(string Id, string Name, string Renderer, bool BuiltIn)
{
    /// <summary>Vlastny vzhlad prekryva vstavany s rovnakym id (zmazanim sa vstavany vrati).</summary>
    public bool Overrides { get; init; }
}
