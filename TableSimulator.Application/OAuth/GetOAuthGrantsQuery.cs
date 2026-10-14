using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.OAuth;

/// <summary>Prepojene aplikacie - vlastne povolenia; spravca klucov vidi vsetky.</summary>
public sealed record GetOAuthGrantsQuery : IQuery<IReadOnlyList<OAuthGrantDto>>;
