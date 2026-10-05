using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Fonts;

namespace TableSimulator.Application.Fonts;

/// <summary>Subor pisiem ELEN pre prehliadac.</summary>
/// <param name="File">meno suboru .bin</param>
public sealed record GetFontQuery(string File) : IQuery<FontFileDto>;
