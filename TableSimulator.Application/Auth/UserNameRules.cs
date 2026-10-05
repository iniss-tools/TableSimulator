namespace TableSimulator.Application.Auth;

/// <summary>Pravidla prihlasovacieho mena a hesla (rovnake pri zalozeni prveho spravcu aj pri sprave pouzivatelov).</summary>
public static class UserNameRules
{
    /// <summary>Najkratsie heslo.</summary>
    public const int MinPasswordLength = 8;

    /// <summary>Povolene znaky mena (bez medzier a diakritiky - meno sa pise pri kazdom prihlaseni).</summary>
    public const string AllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-";

    public static bool IsValid(string? userName) =>
        userName is { Length: >= 2 and <= 64 } && userName.All(c => AllowedCharacters.Contains(c, StringComparison.Ordinal));
}
