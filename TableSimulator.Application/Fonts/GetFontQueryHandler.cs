using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Fonts;

namespace TableSimulator.Application.Fonts;

public sealed class GetFontQueryHandler(FontCatalog fonts) : IQueryHandler<GetFontQuery, FontFileDto>
{
    public Task<Result<FontFileDto>> Handle(GetFontQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(fonts.Get(query.File) is { } font ? Result.Success(font) : Result.Failure<FontFileDto>(FontErrors.NotFound(query.File)));
}
