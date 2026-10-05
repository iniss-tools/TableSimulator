using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Users;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Prihlasovanie: ASP.NET Core Identity nad databazou simulatora s cookie, politika pre kazde opravnenie
/// a pre ostatne poziadavky aspon prihlasenie. Bez prihlasovania je kazda poziadavka spravca (<see cref="LocalAuthenticationHandler" />).
/// </summary>
public static class AuthSetup
{
    /// <summary>Nazov prihlasovacej cookie.</summary>
    public const string CookieName = "TableSimulator.Auth";

    /// <summary>Schema, ktora podla poziadavky vyberie kluc, cookie alebo rezim bez prihlasenia.</summary>
    public const string AutoScheme = "Auto";

    public static IServiceCollection AddAuth(this IServiceCollection services, AppPaths paths, AuthMode mode)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(mode);
        services.AddSingleton(mode);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // kluce podpisu cookie pri nastaveniach - po restarte (aj prenesenej instalacii) prihlasenie plati
        var protection = services.AddDataProtection().SetApplicationName("TableSimulator").PersistKeysToFileSystem(new DirectoryInfo(paths.KeysFolder));
        if (OperatingSystem.IsWindows())
            protection.ProtectKeysWithDpapi();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.AllowedUserNameCharacters = UserNameRules.AllowedCharacters;
                options.Password.RequiredLength = UserNameRules.MinPasswordLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddClaimsPrincipalFactory<AppClaimsFactory>()
            .AddSignInManager();
        // zmena hesla alebo vypnutie uctu zrusi prihlasenia najneskor do minuty
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

        // poziadavka s pristupovym klucom ide cez kluc (aj ked prehliadac posiela cookie), inak cookie alebo rezim bez prihlasenia
        services.AddAuthentication(AutoScheme)
            .AddPolicyScheme(AutoScheme, null, options => options.ForwardDefaultSelector = context =>
                AccessKeyAuthenticationHandler.KeyOf(context.Request) is not null ? AccessKeyAuthenticationHandler.SchemeName
                : mode.LoginRequired ? IdentityConstants.ApplicationScheme
                : LocalAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(LocalAuthenticationHandler.SchemeName, null)
            .AddScheme<AuthenticationSchemeOptions, AccessKeyAuthenticationHandler>(AccessKeyAuthenticationHandler.SchemeName, null)
            .AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            // API a WebSocket - namiesto presmerovania na prihlasovaciu stranku stav 401/403
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        var authorization = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        foreach (var permission in Permissions.All)
            authorization.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.WithPermission(permission)));
        return services;
    }
}
