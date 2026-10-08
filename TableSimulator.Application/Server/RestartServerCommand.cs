using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Server;

/// <summary>Restart servera sluzby Windows (zmeny nastavenia, ktore sa prejavia az po restarte).</summary>
public sealed record RestartServerCommand : ICommand;
