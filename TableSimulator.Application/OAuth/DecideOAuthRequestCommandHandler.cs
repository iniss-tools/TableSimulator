using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.OAuth;

public sealed partial class DecideOAuthRequestCommandHandler(OAuthPending pending, ICurrentUser user, RoleCatalog roles,
    ILogger<DecideOAuthRequestCommandHandler> logger) : ICommandHandler<DecideOAuthRequestCommand, OAuthRedirectDto>
{
    public Task<Result<OAuthRedirectDto>> Handle(DecideOAuthRequestCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Decide(command));

    private Result<OAuthRedirectDto> Decide(DecideOAuthRequestCommand command)
    {
        if (user.IsAccessKey || user.UserName is not { } userName || user.Role is not { } userRole)
            return Result.Failure<OAuthRedirectDto>(OAuthErrors.AccessKey());
        var role = command.Request.Role ?? userRole;
        // rola sa overi pred spotrebovanim ziadosti - pouzivatel moze vybrat inu
        if (command.Request.Allow && (roles.Find(role) is not { } info || !roles.Covers(userRole, info.Permissions)))
            return Result.Failure<OAuthRedirectDto>(OAuthErrors.RoleTooHigh());
        if (pending.TakeRequest(command.Id) is not { } request)
            return Result.Failure<OAuthRedirectDto>(OAuthErrors.RequestNotFound());

        if (!command.Request.Allow)
            return Result.Success(new OAuthRedirectDto(OAuthRedirect.With(request.RedirectUri, ("error", "access_denied"), ("state", request.State), ("iss", request.Issuer))));
        var code = pending.Approve(request, userName, role);
        Approved(logger, userName, request.Client.Name, role);
        return Result.Success(new OAuthRedirectDto(OAuthRedirect.With(request.RedirectUri, ("code", code), ("state", request.State), ("iss", request.Issuer))));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OAuth: {UserName} povolil pristup klientovi {Client} s rolou {Role}")]
    private static partial void Approved(ILogger logger, string userName, string client, string role);
}
