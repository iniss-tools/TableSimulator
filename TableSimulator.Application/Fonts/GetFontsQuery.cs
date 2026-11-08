using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Fonts;

/// <summary>Mena suborov pisiem ELEN.</summary>
public sealed record GetFontsQuery : IQuery<string[]>;
