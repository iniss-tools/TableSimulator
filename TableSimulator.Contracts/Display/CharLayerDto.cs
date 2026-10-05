namespace TableSimulator.Contracts.Display;

/// <summary>Vrstva riadku znakovej tabule; kod -1 = znak vo vrstve nie je (plati znak vrstvy 0).</summary>
public sealed record CharLayerDto(int Layer, int[] Codes, int[] Fonts, string Text);
