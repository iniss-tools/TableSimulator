using System.Security.Claims;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Pouzivatel aktualnej poziadavky z prihlasenia ASP.NET Core.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserName => IsAuthenticated ? Principal!.Identity!.Name : null;

    public string? DisplayName => IsAuthenticated ? Principal!.FindFirst(AuthClaims.DisplayName)?.Value ?? UserName : null;

    public string? Role => IsAuthenticated ? Roles.All.FirstOrDefault(Principal!.IsInRole) : null;
}
