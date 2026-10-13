using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

/// <summary>
/// Pocka, kym tabula <paramref name="Id" /> nebude mat verziu vacsiu nez <paramref name="AfterVersion" /> (nova sprava od
/// INISSu, zmena nastavenia), najdlhsie <paramref name="Timeout" />; ak uz ju ma, vrati sa hned.
/// </summary>
public sealed record WaitForBoardChangeQuery(string Id, long AfterVersion, TimeSpan Timeout) : IQuery<BoardChangeDto>;

public sealed class WaitForBoardChangeQueryValidator : AbstractValidator<WaitForBoardChangeQuery>
{
    /// <summary>Najdlhsie cakanie - poziadavka HTTP nema visiet dlhsie.</summary>
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromSeconds(60);

    public const string Version = "board.version";
    public const string Timeout = "board.waitTimeout";

    public WaitForBoardChangeQueryValidator()
    {
        RuleFor(q => q.Id).NotEmpty().WithErrorCode("board.id");
        RuleFor(q => q.AfterVersion).GreaterThanOrEqualTo(0).WithErrorCode(Version);
        RuleFor(q => q.Timeout).Must(t => t > TimeSpan.Zero && t <= MaxTimeout).WithErrorCode(Timeout)
            .WithMessage($"The timeout must be more than 0 and at most {MaxTimeout.TotalSeconds:0} seconds.");
    }
}

public sealed class WaitForBoardChangeQueryHandler(Simulator simulator, TimeProvider time) : IQueryHandler<WaitForBoardChangeQuery, BoardChangeDto>
{
    public async Task<Result<BoardChangeDto>> Handle(WaitForBoardChangeQuery query, CancellationToken cancellationToken)
    {
        // kazda zmena tabule (alebo zoznamu tabul - zmazanie) prebudi cakanie a obsah sa precita znova; prihlasenie
        // pred prvym citanim, aby sa nestratila zmena medzi citanim a cakanim. Semafor sa nezahadzuje - udalost z ineho
        // vlakna moze prist este tesne po odhlaseni a Release na zahodenom by zhodil linku, ktora ju vyvolala.
        var signal = new SemaphoreSlim(0);
        void OnChanged(SimulatorEvent e)
        {
            if (e is BoardChanged board && board.BoardId == query.Id || e is StructureChanged)
                signal.Release();
        }

        simulator.Changed += OnChanged;
        try
        {
            using var timeout = new CancellationTokenSource(query.Timeout, time);
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            while (true)
            {
                var board = simulator.Read(s => s.FindBoard(query.Id) is { } b ? DtoMapper.ToTextDto(b) : null);
                if (board is null)
                    return Result.Failure<BoardChangeDto>(BoardErrors.NotFound(query.Id));
                if (board.Version > query.AfterVersion)
                    return Result.Success(new BoardChangeDto(true, board));
                try
                {
                    await signal.WaitAsync(wait.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    return Result.Success(new BoardChangeDto(false, board));
                }
            }
        }
        finally
        {
            simulator.Changed -= OnChanged;
        }
    }
}
