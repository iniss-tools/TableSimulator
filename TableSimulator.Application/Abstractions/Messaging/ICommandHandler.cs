using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Abstractions.Messaging;

/// <summary>Vykona prikaz bez vysledku.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Vykona prikaz s vysledkom.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
