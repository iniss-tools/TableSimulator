using System.ComponentModel;

namespace TableSimulator.Mcp;

/// <summary>Vysledok zmeny bez dalsich udajov (odstranenie, zastavenie).</summary>
public sealed record DoneResult([property: Description("What was done.")] string Done);
