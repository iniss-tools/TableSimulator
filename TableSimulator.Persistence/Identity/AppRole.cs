using Microsoft.AspNetCore.Identity;

namespace TableSimulator.Persistence.Identity;

/// <summary>Rola (pevne roly simulatora - <c>admin</c>, <c>operator</c>, <c>viewer</c>).</summary>
public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole()
    {
    }

    public AppRole(string name)
        : base(name)
    {
    }
}
