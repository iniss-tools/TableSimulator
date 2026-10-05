using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Fonts;
using TableSimulator.Contracts.Fonts;

namespace TableSimulator.Api.Fonts;

/// <summary>GET /api/fonts/{file} - subor pisiem ELEN pre prehliadac.</summary>
public sealed class GetFont : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("fonts/{file}", async (string file, IQueryHandler<GetFontQuery, FontFileDto> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetFontQuery(file), cancellationToken)).Match(Results.Ok, CustomResults.Problem));
}
