namespace TableSimulator.Contracts.Virtual;

/// <summary>Definicie tabul zo suboru <c>Web.config</c> INISSView (POST /api/virtual/import).</summary>
/// <param name="Content">obsah suboru</param>
/// <param name="Replace">tabulu s rovnakym cislom prepisat; inak ostane</param>
public sealed record InissViewImportRequest(string Content, bool Replace);
