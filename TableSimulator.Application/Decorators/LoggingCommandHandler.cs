using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Decorators;

/// <summary>Zaznam vybavenia prikazu bez vysledku (cas, pripadne chyba).</summary>
public sealed class LoggingCommandHandler<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    ILogger<LoggingCommandHandler<TCommand>> logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var result = await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false);
        LoggingDecorator.Log(logger, typeof(TCommand).Name, result, started, true);
        return result;
    }
}

/// <summary>Zaznam vybavenia prikazu s vysledkom (cas, pripadne chyba).</summary>
public sealed class LoggingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    ILogger<LoggingCommandHandler<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var result = await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false);
        LoggingDecorator.Log(logger, typeof(TCommand).Name, result, started, true);
        return result;
    }
}
