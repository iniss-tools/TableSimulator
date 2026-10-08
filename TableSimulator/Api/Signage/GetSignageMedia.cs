using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>
/// GET /api/signage/media/{id} - obrazok alebo video spravy (aj po castiach pre video); kluc zariadenia moze byt
/// v parametri <c>access_token</c> (obrazok a video stranka nacita bez hlavicky).
/// </summary>
public sealed class GetSignageMedia : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("signage/media/{id:guid}", async (Guid id, HttpContext context, IQueryHandler<GetSignageMediaQuery, SignageMediaFile> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetSignageMediaQuery(id), cancellationToken)).Match(file =>
            {
                // obsah pod cislom sa nemeni
                context.Response.Headers[HeaderNames.CacheControl] = "private, max-age=86400, immutable";
                return Results.File(file.Path, file.ContentType, enableRangeProcessing: true);
            }, CustomResults.Problem))
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .RequireAuthorization(Permissions.SimulatorView);
}
