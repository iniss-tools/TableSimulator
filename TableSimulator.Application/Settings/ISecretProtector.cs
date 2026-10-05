namespace TableSimulator.Application.Settings;

/// <summary>Sifrovanie tajnych hodnot nastavenia (heslo certifikatu) klucmi ochrany dat servera.</summary>
public interface ISecretProtector
{
    /// <summary>Zasifrovana hodnota; prazdna ostane prazdna.</summary>
    string Protect(string value);

    /// <summary>Desifrovana hodnota; prazdna alebo neprecitatelna (iny pocitac, poskodena) = prazdna.</summary>
    string Unprotect(string value);
}
