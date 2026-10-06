namespace TableSimulator.Contracts.Config;

/// <summary>Vysledok importu liniek a tabul.</summary>
/// <param name="Lines">pridane linky (pri nahradeni vsetky)</param>
/// <param name="Boards">pridane tabule (pri nahradeni vsetky)</param>
/// <param name="Replaced">linky a tabule sa nahradili</param>
public sealed record ConfigImportedDto(int Lines, int Boards, bool Replaced);
