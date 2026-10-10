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

    public string? Role => IsAuthenticated ? AuthClaims.RoleOf(Principal!) : null;

    public string? Address => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAccessKey => IsAuthenticated && Principal!.HasClaim(c => c.Type == AuthClaims.AccessKey);
}
