namespace TableSimulator.Infrastructure.Persistence;

/// <summary>Pripojenie k PostgreSQL v <c>database.json</c>.</summary>
public sealed record PostgresConfig
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "tablesimulator";

    /// <summary>Pouzivatel; prazdny = prihlasenie Windows (SSPI/Kerberos), ak ho server PostgreSQL dovoli.</summary>
    public string Username { get; init; } = "";

    /// <summary>Citatelne heslo - napisane rucne; server ho pri starte zasifruje do <see cref="ProtectedPassword" />.</summary>
    public string? Password { get; init; }

    /// <summary>Heslo zasifrovane DPAPI pre tento pocitac (kopia suboru inde nepomoze).</summary>
    public string? ProtectedPassword { get; init; }

    /// <summary>Dalsie parametre pripojenia Npgsql, napr. <c>SSL Mode=Require</c>.</summary>
    public string Options { get; init; } = "";
}
