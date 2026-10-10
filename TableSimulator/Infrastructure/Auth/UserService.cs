using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Users;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Sprava pouzivatelov cez ASP.NET Core Identity. Zmena roly sa prihlasenemu prejavi pri dalsom overeni prihlasenia
/// (do minuty); vypnutie, nove heslo a odstranenie zmenia razitko a prihlasenie zrusia. Pouzivatel bez niektoreho
/// opravnenia nesmie pridelit rolu s nim ani spravovat pouzivatela s takou rolou (napr. nastavit heslo spravcovi).
/// </summary>
public sealed class UserService(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    AuthMode mode,
    ICurrentUser current,
    RoleCatalog roles,
    LoginThrottle throttle,
    TimeProvider time) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await users.Users.OrderBy(u => u.UserName).ToListAsync(cancellationToken).ConfigureAwait(false);
        var now = time.GetUtcNow();
        var result = new List<UserDto>(list.Count);
        foreach (var user in list)
        {
            result.Add(new UserDto(user.UserName!, user.DisplayName, await RoleOfAsync(user).ConfigureAwait(false), user.IsDisabled,
                user.LockoutEnd is { } end && end > now ? end : null, user.CreatedAt, user.LastLoginAt));
        }

        return result;
    }

    public async Task<Result> CreateAsync(string userName, string displayName, string role, string password, CancellationToken cancellationToken)
    {
        if (!CanManage(role))
            return Result.Failure(UserErrors.RoleTooHigh());
        var user = new AppUser { UserName = userName, DisplayName = displayName, CreatedAt = time.GetUtcNow() };
        var created = await users.CreateAsync(user, password).ConfigureAwait(false);
        if (!created.Succeeded)
            return Result.Failure(IdentityErrors.ToError(created.Errors));
        var added = await users.AddToRoleAsync(user, role).ConfigureAwait(false);
        return added.Succeeded ? Result.Success() : Result.Failure(IdentityErrors.ToError(added.Errors));
    }

    public async Task<Result> UpdateAsync(string userName, string displayName, string role, bool disabled, CancellationToken cancellationToken)
    {
        if (await users.FindByNameAsync(userName).ConfigureAwait(false) is not { } user)
            return Result.Failure(UserErrors.NotFound(userName));
        var oldRole = await RoleOfAsync(user).ConfigureAwait(false);
        if (!CanManage(oldRole) || !CanManage(role))
            return Result.Failure(UserErrors.RoleTooHigh());
        if (IsSelf(user) && (role != oldRole || disabled))
            return Result.Failure(UserErrors.Self());
        if (LosesAdmin(user, oldRole, role == Roles.Admin && !disabled) && await EnabledAdminsAsync().ConfigureAwait(false) <= 1)
            return Result.Failure(UserErrors.LastAdmin());

        var disabling = disabled && !user.IsDisabled;
        user.DisplayName = displayName;
        user.IsDisabled = disabled;
        var updated = await users.UpdateAsync(user).ConfigureAwait(false);
        if (!updated.Succeeded)
            return Result.Failure(IdentityErrors.ToError(updated.Errors));
        if (role != oldRole)
        {
            await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user).ConfigureAwait(false)).ConfigureAwait(false);
            await users.AddToRoleAsync(user, role).ConfigureAwait(false);
        }

        // vypnuty pouzivatel sa odhlasi pri dalsom overeni prihlasenia
        if (disabling)
            await users.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> SetPasswordAsync(string userName, string password, CancellationToken cancellationToken)
    {
        if (await users.FindByNameAsync(userName).ConfigureAwait(false) is not { } user)
            return Result.Failure(UserErrors.NotFound(userName));
        if (!CanManage(await RoleOfAsync(user).ConfigureAwait(false)))
            return Result.Failure(UserErrors.RoleTooHigh());
        // heslo sa overi skor, nez sa stare odstrani - pouzivatel nesmie ostat bez hesla
        foreach (var validator in users.PasswordValidators)
        {
            var valid = await validator.ValidateAsync(users, user, password).ConfigureAwait(false);
            if (!valid.Succeeded)
                return Result.Failure(IdentityErrors.ToError(valid.Errors));
        }

        if (await users.HasPasswordAsync(user).ConfigureAwait(false))
        {
            var removed = await users.RemovePasswordAsync(user).ConfigureAwait(false);
            if (!removed.Succeeded)
                return Result.Failure(IdentityErrors.ToError(removed.Errors));
        }

        var added = await users.AddPasswordAsync(user, password).ConfigureAwait(false);
        if (!added.Succeeded)
            return Result.Failure(IdentityErrors.ToError(added.Errors));
        return await UnlockAsync(user).ConfigureAwait(false);
    }

    public async Task<Result> UnlockAsync(string userName, CancellationToken cancellationToken)
    {
        if (await users.FindByNameAsync(userName).ConfigureAwait(false) is not { } user)
            return Result.Failure(UserErrors.NotFound(userName));
        return CanManage(await RoleOfAsync(user).ConfigureAwait(false)) ? await UnlockAsync(user).ConfigureAwait(false) : Result.Failure(UserErrors.RoleTooHigh());
    }

    public async Task<Result> DeleteAsync(string userName, CancellationToken cancellationToken)
    {
        if (await users.FindByNameAsync(userName).ConfigureAwait(false) is not { } user)
            return Result.Failure(UserErrors.NotFound(userName));
        if (IsSelf(user))
            return Result.Failure(UserErrors.Self());
        var role = await RoleOfAsync(user).ConfigureAwait(false);
        if (!CanManage(role))
            return Result.Failure(UserErrors.RoleTooHigh());
        if (LosesAdmin(user, role, false) && await EnabledAdminsAsync().ConfigureAwait(false) <= 1)
            return Result.Failure(UserErrors.LastAdmin());
        var deleted = await users.DeleteAsync(user).ConfigureAwait(false);
        return deleted.Succeeded ? Result.Success() : Result.Failure(IdentityErrors.ToError(deleted.Errors));
    }

    public async Task<Result> ChangeOwnPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        // bez prihlasovania je kazdy spravca "local", ktory heslo nema
        if (!mode.LoginRequired || current.UserName is not { } name || await users.FindByNameAsync(name).ConfigureAwait(false) is not { } user)
            return Result.Failure(AuthErrors.NotRequired());
        // aj zmena hesla overuje heslo - ukradnute prihlasenie by inak hadalo heslo bez limitu
        if (throttle.Blocked() is not null)
            return Result.Failure(AuthErrors.TooManyAttempts());
        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);
        if (!changed.Succeeded)
        {
            if (!changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
                return Result.Failure(IdentityErrors.ToError(changed.Errors));
            throttle.Failed();
            return Result.Failure(UserErrors.CurrentPassword());
        }

        // zmena hesla zmeni razitko - nove cookie, aby toto prihlasenie platilo dalej
        await signIn.RefreshSignInAsync(user).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> UnlockAsync(AppUser user)
    {
        await users.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>Prihlaseny pouzivatel meni sam seba (bez prihlasovania je kazdy "local" - nikto z databazy).</summary>
    private bool IsSelf(AppUser user) => mode.LoginRequired && string.Equals(user.UserName, current.UserName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Prihlaseny ma vsetky opravnenia roly - smie ju pridelit a spravovat pouzivatelov s nou.</summary>
    private bool CanManage(string role) => roles.Covers(current.Role, roles.PermissionsOf(role));

    /// <summary>Aktivny spravca prestane byt aktivnym spravcom.</summary>
    private static bool LosesAdmin(AppUser user, string oldRole, bool staysAdmin) => oldRole == Roles.Admin && !user.IsDisabled && !staysAdmin;

    private async Task<int> EnabledAdminsAsync() => (await users.GetUsersInRoleAsync(Roles.Admin).ConfigureAwait(false)).Count(u => !u.IsDisabled);

    private async Task<string> RoleOfAsync(AppUser user)
    {
        var assigned = await users.GetRolesAsync(user).ConfigureAwait(false);
        return assigned.Count > 0 ? assigned[0] : Roles.Viewer;
    }
}
