using System.Globalization;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Frames;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Frames;

/// <summary>
/// Vlozi na linku <paramref name="Line" /> bajty <paramref name="Hex" /> ako od INISSu (agent hra INISS): simulator ich spracuje
/// ako spravy z linky (log, statistika, zaznam, zrkadlo) a odpovede tabul vrati namiesto poslania. Na linke s pripojenym INISSom
/// nie - vlozene bajty by sa pomiesali s rozpracovanou spravou od neho.
/// </summary>
public sealed record SendFrameCommand(int Line, string Hex) : ICommand<SentFrameDto>, IAuditable
{
    string IAuditable.AuditTarget => Line.ToString(CultureInfo.InvariantCulture);

    /// <summary>Bajty z hex (medzery a pomlcky sa ignoruju); null = neplatny zapis.</summary>
    public static byte[]? Parse(string? hex)
    {
        var digits = string.Concat((hex ?? "").Where(c => !char.IsWhiteSpace(c) && c != '-'));
        if (digits.Length == 0 || digits.Length % 2 != 0)
            return null;
        try
        {
            return Convert.FromHexString(digits);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public sealed class SendFrameCommandValidator : AbstractValidator<SendFrameCommand>
{
    /// <summary>Najviac bajtov naraz.</summary>
    public const int MaxBytes = 4096;

    public const string HexCode = "frames.hex";

    public SendFrameCommandValidator()
    {
        RuleFor(c => c.Line).InclusiveBetween(1, 100).WithErrorCode(LineErrors.Number);
        RuleFor(c => c.Hex).Must(h => SendFrameCommand.Parse(h) is { Length: <= MaxBytes }).WithErrorCode(HexCode)
            .WithMessage($"The bytes must be hexadecimal pairs, at most {MaxBytes} bytes.");
    }
}

/// <summary>Chyby vlozenia spravy.</summary>
public static class SendFrameErrors
{
    public static Error InissConnected(int line) =>
        Error.Conflict("frames.inissConnected", string.Create(CultureInfo.InvariantCulture, $"INISS is connected to line {line}; frames can be sent only to a line without INISS."));

    public static Error LineOutage(int line) =>
        Error.Conflict("frames.lineOutage", string.Create(CultureInfo.InvariantCulture, $"Line {line} has a simulated outage."));
}

public sealed class SendFrameCommandHandler(Simulator simulator) : ICommandHandler<SendFrameCommand, SentFrameDto>
{
    public Task<Result<SentFrameDto>> Handle(SendFrameCommand command, CancellationToken cancellationToken)
    {
        var line = simulator.Read(s => s.Lines.FirstOrDefault(l => l.Number == command.Line) is { } l ? (Status: l.Status, Found: true) : default);
        var error = !line.Found ? LineErrors.NotFound(command.Line)
            : line.Status == LineStatus.Connected ? SendFrameErrors.InissConnected(command.Line)
            : line.Status == LineStatus.Outage ? SendFrameErrors.LineOutage(command.Line)
            : null;
        if (error is not null)
            return Task.FromResult(Result.Failure<SentFrameDto>(error));

        // ktore tabule linky sa zmenili - podla verzie pred a po
        var before = Versions(command.Line);
        var replies = simulator.Receive(command.Line, SendFrameCommand.Parse(command.Hex)!);
        var changed = simulator.Read(s => s.Boards.Where(b => b.Line == command.Line && (!before.TryGetValue(b.Id, out var v) || b.Version != v))
            .Select(DtoMapper.ToTextDto).ToList());
        return Task.FromResult(Result.Success(new SentFrameDto(
            replies.Select(r => new FrameReplyDto(Convert.ToHexString(r.Bytes), (int)r.Delay.TotalMilliseconds)).ToList(), changed)));
    }

    private Dictionary<string, long> Versions(int line) =>
        simulator.Read(s => s.Boards.Where(b => b.Line == line).ToDictionary(b => b.Id, b => b.Version, StringComparer.Ordinal));
}
