using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Prihlasovanie cez ASP.NET Core Identity (cookie) - overenie hesla, zablokovanie, vypnute ucty.</summary>
public sealed class AuthService(
    AuthMode mode,
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    IHttpContextAccessor accessor,
    TimeProvider time,
    ILogger<AuthService> logger) : IAuthService
{
    public bool LoginRequired => mode.LoginRequired;

    /// <summary>Spojenie z tohto pocitaca (TestServer adresu nema - tiez lokalne).</summary>
    public bool IsLocalRequest => accessor.HttpContext?.Connection.RemoteIpAddress is not { } remote || IPAddress.IsLoopback(remote);

    public async Task<bool> HasAdminAsync(CancellationToken cancellationToken) =>
        (await users.GetUsersInRoleAsync(Roles.Admin).ConfigureAwait(false)).Any(u => !u.IsDisabled);

    public async Task<(SignInOutcome Outcome, SignedInUser? User)> SignInAsync(string userName, string password, bool remember, CancellationToken cancellationToken)
    {
        var user = await users.FindByNameAsync(userName).ConfigureAwait(false);
        var outcome = SignInOutcome.InvalidCredentials;
        if (user is not null)
        {
            // heslo sa overi skor nez vypnutie uctu - z odpovede sa neda zistit, ktore ucty existuju
            var check = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true).ConfigureAwait(false);
            outcome = check.IsLockedOut ? SignInOutcome.LockedOut
                : !check.Succeeded ? SignInOutcome.InvalidCredentials
                : user.IsDisabled ? SignInOutcome.Disabled
                : SignInOutcome.Success;
        }

        if (outcome != SignInOutcome.Success || user is null)
        {
            AuthLog.SignInFailed(logger, userName, outcome);
            return (outcome, null);
        }

        user.LastLoginAt = time.GetUtcNow();
        await users.UpdateAsync(user).ConfigureAwait(false);
        await signIn.SignInAsync(user, isPersistent: remember).ConfigureAwait(false);
        var signedIn = new SignedInUser(user.UserName!, user.DisplayName, await RoleOfAsync(user).ConfigureAwait(false));
        AuthLog.SignedIn(logger, signedIn.UserName, signedIn.Role);
        return (SignInOutcome.Success, signedIn);
    }

    public async Task SignOutAsync()
    {
        AuthLog.SignedOut(logger, accessor.HttpContext?.User.Identity?.Name);
        await signIn.SignOutAsync().ConfigureAwait(false);
    }

    public async Task<Result<SignedInUser>> CreateFirstAdminAsync(string userName, string displayName, string password, CancellationToken cancellationToken)
    {
        var user = new AppUser { UserName = userName, DisplayName = displayName, CreatedAt = DateTimeOffset.UtcNow };
        var created = await users.CreateAsync(user, password).ConfigureAwait(false);
        if (!created.Succeeded)
            return Result.Failure<SignedInUser>(IdentityErrors.ToError(created.Errors));
        await users.AddToRoleAsync(user, Roles.Admin).ConfigureAwait(false);
        await signIn.SignInAsync(user, isPersistent: false).ConfigureAwait(false);
        AuthLog.AdminCreated(logger, userName);
        return Result.Success(new SignedInUser(userName, displayName, Roles.Admin));
    }

    private async Task<string> RoleOfAsync(AppUser user)
    {
        var roles = await users.GetRolesAsync(user).ConfigureAwait(false);
        return Roles.All.FirstOrDefault(roles.Contains) ?? Roles.Viewer;
    }
}
