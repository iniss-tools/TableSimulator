using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Simulation.Events;

/// <summary>Novy zaznam komunikacie.</summary>
public sealed record FrameLogged(FrameRecord Record) : SimulatorEvent;
