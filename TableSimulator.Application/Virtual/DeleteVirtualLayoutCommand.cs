using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Virtual;

/// <summary>Odstrani vlastne rozlozenie, ktore ziadna tabula nepouziva.</summary>
public sealed record DeleteVirtualLayoutCommand(string Id) : ICommand;
