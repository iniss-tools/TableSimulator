namespace TableSimulator.Contracts.Designs;

/// <summary>Material listovej tabule (vzhlad s <c>renderer</c> = <c>flaps</c>).</summary>
/// <param name="Colors">farby listov podla farby listu (<c>black</c>, <c>red</c>, <c>yellow</c>, <c>white</c>)</param>
/// <param name="Row">pozadie riadku listov</param>
/// <param name="Split">deliaca ciara v strede listu</param>
/// <param name="Unknown">text listu, ktory zoznam nepozna</param>
/// <param name="Font">pismo textov listov (CSS font-family)</param>
public sealed record DesignFlapsDto(IReadOnlyDictionary<string, DesignFlapColorDto> Colors, string Row, string Split, string Unknown, string Font);

