using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Settings;

/// <summary>Nastavenie servera.</summary>
public sealed record GetSettingsQuery : IQuery<SettingsDto>;
