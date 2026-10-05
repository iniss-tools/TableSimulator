using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Obnova pristupu spravcu z prikazoveho riadka (<c>--reset-admin [meno]</c>) - zabudnute heslo, zablokovany alebo
/// vypnuty ucet, odobrana rola. Ucet dostane nahodne heslo (vypise sa len na konzolu, do logu nie), odblokuje sa,
/// zapne, ma rolu spravca a jeho prihlasenia na zariadeniach skoncia. Meno, ktore neexistuje, sa zalozi ako novy
/// spravca. Bez mena sa obnovi jediny spravca. Zapise sa do auditu ako akcia pouzivatela <see cref="Actor" />.
/// </summary>
public static partial class AdminReset
{
    /// <summary>Kto akciu urobil - v mene pouzivatela sa zatvorky nemozu vyskytnut.</summary>
    public const string Actor = "(console)";

    /// <summary>Akcia v audite.</summary>
    public const string Action = "ResetAdmin";

    /// <summary>Dlzka noveho hesla.</summary>
    public const int PasswordLength = 16;

    // bez znakov, ktore sa pri prepisovani pletu (0/O, 1/l/I)
    private const string PasswordCharacters = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Obnovi spravcu; vrati kod ukoncenia (0 = obnoveny, 1 = chyba, 2 = treba zadat meno).</summary>
    public static async Task<int> RunAsync(IServiceProvider services, string? userName, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(output);
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var time = provider.GetRequiredService<TimeProvider>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminReset));

        AppUser? user;
        if (userName is null)
        {
            var admins = (await users.GetUsersInRoleAsync(Roles.Admin).ConfigureAwait(false)).OrderBy(u => u.UserName, StringComparer.Ordinal).ToList();
            if (admins.Count != 1)
            {
                await output.WriteLineAsync(admins.Count == 0
                    ? "Ziadny spravca nie je. Zadajte meno noveho spravcu: TableSimulator --reset-admin <meno>"
                    : $"Spravcov je viac ({string.Join(", ", admins.Select(a => a.UserName))}). Zadajte meno: TableSimulator --reset-admin <meno>").ConfigureAwait(false);
                return 2;
            }

            user = admins[0];
        }
        else
        {
            if (!UserNameRules.IsValid(userName))
            {
                await output.WriteLineAsync($"Neplatne meno '{userName}' - 2 az 64 znakov: pismena bez diakritiky, cislice, bodka, pomlcka, podciarknik.")
                    .ConfigureAwait(false);
                return 1;
            }

            user = await users.FindByNameAsync(userName).ConfigureAwait(false);
        }

        var password = NewPassword();
        var created = user is null;
        var result = created
            ? await CreateAsync(users, userName!, password, time).ConfigureAwait(false)
            : await ResetAsync(users, user!, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            await output.WriteLineAsync($"Spravcu sa nepodarilo obnovit: {string.Join(" ", result.Errors.Select(e => e.Description))}").ConfigureAwait(false);
            return 1;
        }

        var name = created ? userName! : user!.UserName!;
        await provider.GetRequiredService<IAuditLog>().WriteAsync(new AuditEntry(0, time.GetUtcNow().UtcDateTime, Actor, Action, name, true, null,
            JsonSerializer.Serialize(new { userName = name, created }), null), CancellationToken.None).ConfigureAwait(false);
        Reset(logger, name, created);

        await output.WriteLineAsync(created ? $"Zalozeny novy spravca {name}." : $"Spravca {name} je odblokovany a zapnuty.").ConfigureAwait(false);
        await output.WriteLineAsync($"Nove heslo: {password}").ConfigureAwait(false);
        await output.WriteLineAsync("Po prihlaseni si ho zmente (ponuka pod menom v hlavicke stranky).").ConfigureAwait(false);
        return 0;
    }

    /// <summary>Nahodne heslo bez zamenitelnych znakov.</summary>
    public static string NewPassword() => new(RandomNumberGenerator.GetItems<char>(PasswordCharacters, PasswordLength));

    private static async Task<IdentityResult> CreateAsync(UserManager<AppUser> users, string userName, string password, TimeProvider time)
    {
        var user = new AppUser { UserName = userName, DisplayName = userName, CreatedAt = time.GetUtcNow() };
        var created = await users.CreateAsync(user, password).ConfigureAwait(false);
        return created.Succeeded ? await users.AddToRoleAsync(user, Roles.Admin).ConfigureAwait(false) : created;
    }

    private static async Task<IdentityResult> ResetAsync(UserManager<AppUser> users, AppUser user, string password)
    {
        if (await users.HasPasswordAsync(user).ConfigureAwait(false) && await users.RemovePasswordAsync(user).ConfigureAwait(false) is { Succeeded: false } removed)
            return removed;
        if (await users.AddPasswordAsync(user, password).ConfigureAwait(false) is { Succeeded: false } added)
            return added;
        await users.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        user.IsDisabled = false;
        var roles = await users.GetRolesAsync(user).ConfigureAwait(false);
        if (!roles.Contains(Roles.Admin))
        {
            if (roles.Count > 0 && await users.RemoveFromRolesAsync(user, roles).ConfigureAwait(false) is { Succeeded: false } removedRoles)
                return removedRoles;
            if (await users.AddToRoleAsync(user, Roles.Admin).ConfigureAwait(false) is { Succeeded: false } addedRole)
                return addedRole;
        }

        // nove razitko ukonci prihlasenia ucta na vsetkych zariadeniach
        return await users.UpdateSecurityStampAsync(user).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Spravca {UserName} obnoveny z prikazoveho riadka (novy ucet: {Created})")]
    private static partial void Reset(ILogger logger, string userName, bool created);
}
