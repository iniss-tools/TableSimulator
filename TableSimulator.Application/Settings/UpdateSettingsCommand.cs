using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Settings;

namespace TableSimulator.Application.Settings;

/// <summary>Ulozi nastavenie servera.</summary>
public sealed record UpdateSettingsCommand(UpdateSettingsRequest Request) : ICommand<SettingsUpdatedDto>;
