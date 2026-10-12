namespace TableSimulator.Contracts.Boards;

/// <summary>Pismo znakovej tabule (LCD1, ERP) pre jedno cislo pisma.</summary>
/// <param name="Font">bodove pismo (<c>5x7</c>, <c>5x8</c>, <c>6x10</c>, <c>6x12</c>, <c>7x13</c>, <c>7x14</c>); prazdne = najvacsie, ktore sa zmesti</param>
/// <param name="Bold">ztucnene o bod doprava</param>
public sealed record CharFontDto(string Font = "", bool Bold = false);
