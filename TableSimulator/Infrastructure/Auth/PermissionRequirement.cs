using Microsoft.AspNetCore.Authorization;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Poziadavka politiky: aspon jedno z opravneni.</summary>
public sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}
