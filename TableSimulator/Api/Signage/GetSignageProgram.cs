using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Signage;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Api.Signage;

/// <summary>GET /api/signage/panels/{id}/program - co ma panel hrat (panel sa tym zaroven ohlasi).</summary>
public sealed class GetSignageProgram : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("signage/panels/{id:int}/program", async (int id, HttpContext context, IQueryHandler<GetSignageProgramQuery, SignageProgramDto> handler,
            CancellationToken cancellationToken) =>
        {
            // IPv4 v tvare IPv6 (::ffff:10.0.0.5) ako IPv4; ostatne tak, ako su (::1)
            var ip = context.Connection.RemoteIpAddress;
            var address = ip is null ? "" : ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString();
            return (await handler.Handle(new GetSignageProgramQuery(id, address), cancellationToken)).Match(Results.Ok, CustomResults.Problem);
        })
            .Produces<SignageProgramDto>()
            .RequireAuthorization(Permissions.SimulatorView);
}
