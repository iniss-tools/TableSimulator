using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Fonts;

public sealed class GetFontsQueryHandler(FontCatalog fonts) : IQueryHandler<GetFontsQuery, string[]>
{
    public Task<Result<string[]>> Handle(GetFontsQuery query, CancellationToken cancellationToken) => Task.FromResult(Result.Success(fonts.List()));
}
