using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Poziadavka politiky: aspon jedno z opravneni.</summary>
public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

/// <summary>
/// Opravnenia podla roly v prihlaseni z <see cref="RoleCatalog" /> - zmena opravneni roly plati hned, zmena roly
/// pouzivatela pri dalsom overeni prihlasenia (do minuty).
/// </summary>
public sealed class PermissionHandler(RoleCatalog roles) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        if (context.User.Identity?.IsAuthenticated == true && AuthClaims.RoleOf(context.User) is { } role
            && requirement.Permissions.Any(p => roles.Has(role, p)))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
