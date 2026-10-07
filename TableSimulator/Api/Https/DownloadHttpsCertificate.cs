using Microsoft.AspNetCore.Routing;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Https;

namespace TableSimulator.Api.Https;

/// <summary>
/// GET /api/https/certificate - certifikat, ktoremu maju zariadenia doverovat (certifikacna autorita simulatora alebo
/// vlastny certifikat). Verejny - zariadenie si ho stiahne skor, nez sa prihlasi; obsahuje len verejny kluc.
/// </summary>
public sealed class DownloadHttpsCertificate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("https/certificate", async (IQueryHandler<GetHttpsCertificateQuery, HttpsCertificateFile> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetHttpsCertificateQuery(), cancellationToken))
            .Match(file => Results.File(file.Content, file.Authority ? "application/x-x509-ca-cert" : "application/pkix-cert", file.FileName),
                CustomResults.Problem))
            .Produces(StatusCodes.Status200OK, null, "application/pkix-cert", "application/x-x509-ca-cert")
            .AllowAnonymous();
}
