namespace TableSimulator.Contracts.Config;

/// <summary>Linky a tabule zo suboru (POST /api/config/import) - tvar exportu aj povodneho <c>config.json</c>.</summary>
/// <param name="Content">obsah suboru</param>
/// <param name="Replace">nahradit vsetky linky a tabule; inak pridat len chybajuce</param>
public sealed record ImportConfigRequest(string Content, bool Replace);
