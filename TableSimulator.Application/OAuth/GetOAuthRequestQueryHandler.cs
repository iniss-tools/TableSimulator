using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.OAuth;

public sealed class GetOAuthRequestQueryHandler(OAuthPending pending, ICurrentUser user, RoleCatalog roles) : IQueryHandler<GetOAuthRequestQuery, OAuthRequestDto>
{
    public Task<Result<OAuthRequestDto>> Handle(GetOAuthRequestQuery query, CancellationToken cancellationToken)
    {
        if (user.IsAccessKey || user.Role is not { } role)
            return Task.FromResult(Result.Failure<OAuthRequestDto>(OAuthErrors.AccessKey()));
        if (pending.FindRequest(query.Id) is not { } request)
            return Task.FromResult(Result.Failure<OAuthRequestDto>(OAuthErrors.RequestNotFound()));
        var target = Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : request.RedirectUri;
        var allowed = roles.All.Where(r => roles.Covers(role, r.Permissions)).Select(r => new OAuthRoleDto(r.Name, r.DisplayName)).ToArray();
        return Task.FromResult(Result.Success(new OAuthRequestDto(request.Id, request.Client.Name, target, allowed, role)));
    }
}
