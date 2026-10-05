using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;

namespace TableSimulator.Application.Decorators;

/// <summary>Zapise vybaveny prikaz bez vysledku do auditu (aj neuspesny - napr. neplatne vstupy, zle heslo).</summary>
public sealed class AuditCommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler, AuditWriter audit) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var result = await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false);
        await audit.WriteAsync(command, result).ConfigureAwait(false);
        return result;
    }
}

/// <summary>Zapise vybaveny prikaz s vysledkom do auditu (aj neuspesny).</summary>
public sealed class AuditCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> innerHandler, AuditWriter audit)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var result = await innerHandler.Handle(command, cancellationToken).ConfigureAwait(false);
        await audit.WriteAsync(command, result).ConfigureAwait(false);
        return result;
    }
}
