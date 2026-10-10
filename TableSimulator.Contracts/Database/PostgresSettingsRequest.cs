namespace TableSimulator.Contracts.Database;

/// <summary>Pripojenie k PostgreSQL (zmena vyberu databazy, skuska pripojenia).</summary>
public sealed record PostgresSettingsRequest
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "tablesimulator";

    /// <summary>Pouzivatel; prazdny = prihlasenie Windows, ak ho server dovoli.</summary>
    public string Username { get; init; } = "";

    /// <summary>Heslo; null = nechat ulozene, prazdne = zmazat.</summary>
    public string? Password { get; init; }

    /// <summary>Dalsie parametre pripojenia Npgsql (bez hesla), napr. <c>SSL Mode=Require</c>.</summary>
    public string Options { get; init; } = "";
}
