using System.Text.Json;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

/// <summary>Cely vzhlad <paramref name="Id" /> - vlastny, inak vstavany (ako ho nacita prehliadac).</summary>
public sealed record GetDesignQuery(string Id) : IQuery<DesignDto>;

public sealed class GetDesignQueryValidator : AbstractValidator<GetDesignQuery>
{
    public GetDesignQueryValidator() =>
        RuleFor(q => q.Id).Must(SetDesignCommandValidator.IsValidId).WithErrorCode(DesignErrors.Id)
            .WithMessage("The design id must be 1-40 lowercase letters, digits and hyphens.");
}

public sealed class GetDesignQueryHandler(AppPaths paths, ISettingsStore settings) : IQueryHandler<GetDesignQuery, DesignDto>
{
    public async Task<Result<DesignDto>> Handle(GetDesignQuery query, CancellationToken cancellationToken)
    {
        var custom = Path.Combine(paths.DesignsFolder(settings.Current), query.Id, "design.json");
        var builtIn = Path.Combine(paths.BuiltInDesignsFolder, query.Id, "design.json");
        var file = File.Exists(custom) ? custom : File.Exists(builtIn) ? builtIn : null;
        if (file is null)
            return Result.Failure<DesignDto>(DesignErrors.NotFound(query.Id));
        try
        {
            var design = JsonSerializer.Deserialize<SetDesignRequest>(await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false), DesignFolder.Json);
            return design is null
                ? Result.Failure<DesignDto>(DesignErrors.Unreadable(query.Id, "empty file"))
                : Result.Success(new DesignDto(query.Id, file == builtIn, file == custom && File.Exists(builtIn), design));
        }
        catch (JsonException e)
        {
            return Result.Failure<DesignDto>(DesignErrors.Unreadable(query.Id, e.Message));
        }
    }
}
