using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Fonts;

/// <summary>Mena suborov pisiem ELEN.</summary>
public sealed record GetFontsQuery : IQuery<string[]>;
