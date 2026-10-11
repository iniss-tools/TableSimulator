using FluentValidation;
using TableSimulator.Contracts.Station;

namespace TableSimulator.Application.Station;

/// <summary>Cisla liniek mimo 1-100 a nezname triedy nie su chyba - simulator ich vynecha a ohlasi vo vysledku.</summary>
public sealed class ImportStationCommandValidator : AbstractValidator<ImportStationCommand>
{
    public ImportStationCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode("station.request");
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Lines).NotNull().WithErrorCode("station.request");
            RuleFor(c => c.Request.Boards).NotNull().WithErrorCode("station.request");
            RuleForEach(c => c.Request.Lines).ChildRules(line =>
                line.RuleFor(l => l.Port).InclusiveBetween(1, 65535).WithErrorCode("station.port"));
            RuleFor(c => c.Request.LogFolder).MaximumLength(260).WithErrorCode("station.logFolder");
            RuleForEach(c => c.Request.XmlTables).ChildRules(table =>
                table.RuleFor(t => t.Source).Must(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 100
                                                       && s.IndexOfAny(['\\', '/', ':', '*', '?', '"', '<', '>', '|']) < 0)
                    .WithErrorCode("station.xmlTable"))
                .When(c => c.Request.XmlTables is not null);
            RuleForEach(c => c.Request.Boards).ChildRules(board =>
            {
                board.RuleFor(b => b.Address).InclusiveBetween(0, 255).WithErrorCode("station.address");
                board.RuleFor(b => b.Manufacturer).NotEmpty().WithErrorCode("station.manufacturer");
                board.RuleFor(b => b.Flaps).Must(Boards.FlapProfileRules.IsValid).WithErrorCode("station.flaps");
                board.RuleFor(b => b.FlapRecords).InclusiveBetween(1, 64).WithErrorCode("station.flaps");
                board.RuleFor(b => b.Face).Must(IsValidFace).WithErrorCode("station.face");
            });
            RuleFor(c => c.Request.FaceLanguage).IsInEnum().WithErrorCode("station.face");
        });
    }

    /// <summary>Data pre celo v medziach (ako rozmer tabule) - z nich simulator sklada predlohu.</summary>
    private static bool IsValidFace(StationFaceDto? face) =>
        face is null
        || (Enum.IsDefined(face.Kind) && face.LinesPerRecord is >= 1 and <= 16 && face.Records is >= 1 and <= 256 && face.Width is >= 0 and <= 4096
            && face.RowHeight is >= 0 and <= 64 && face.Columns is { Count: <= 64 } columns
            && columns.All(c => c is not null && Enum.IsDefined(c.Align) && c.Line is >= 0 and < 16 && c.Start >= 0 && c.End <= 8192 && c.Content is >= 0 and <= 255)
            && Texts(face.Tracks) && Texts(face.Platforms) && face.Station is { Length: <= 100 });

    private static bool Texts(IReadOnlyList<string>? texts) => texts is { Count: <= 64 } && texts.All(t => t is { Length: <= 20 });
}
