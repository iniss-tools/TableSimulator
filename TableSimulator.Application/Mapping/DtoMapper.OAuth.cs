using TableSimulator.Application.OAuth;
using TableSimulator.Contracts.OAuth;

namespace TableSimulator.Application.Mapping;

/// <summary>Povolenia OAuth pre MCP.</summary>
public static partial class DtoMapper
{
    public static OAuthGrantDto ToDto(OAuthGrant grant, string roleName)
    {
        ArgumentNullException.ThrowIfNull(grant);
        return new OAuthGrantDto(grant.Id, grant.ClientName, grant.UserName, grant.Role, roleName, Utc(grant.CreatedAt), grant.LastUsedAt is { } used ? Utc(used) : null);
    }
}
