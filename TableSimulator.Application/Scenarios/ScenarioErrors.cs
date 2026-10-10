using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Scenarios;

/// <summary>Chyby a kody chyb scenarov porucha.</summary>
public static class ScenarioErrors
{
    public const string Name = "scenario.name";
    public const string Steps = "scenario.steps";
    public const string Cycle = "scenario.cycle";
    public const string StepTime = "scenario.stepTime";
    public const string StepLine = "scenario.stepLine";
    public const string StepAddress = "scenario.stepAddress";
    public const string StepFault = "scenario.stepFault";

    /// <summary>Najviac krokov scenara.</summary>
    public const int MaxSteps = 200;

    /// <summary>Najdlhsi cas v scenari (zaciatok, trvanie, cyklus) - 24 h [ms].</summary>
    public const int MaxMs = 24 * 60 * 60 * 1000;

    /// <summary>Najkratsie trvanie kroku [ms] - spustac kontroluje cas po 200 ms.</summary>
    public const int MinDurationMs = 200;

    public static Error NotFound(int id) =>
        Error.NotFound("scenario.notFound", string.Create(CultureInfo.InvariantCulture, $"Fault scenario {id} does not exist."));
}
