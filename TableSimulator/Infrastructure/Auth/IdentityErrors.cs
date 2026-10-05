using Microsoft.AspNetCore.Identity;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Chyby ASP.NET Core Identity ako chyby overenia s kodmi pre webove rozhranie.</summary>
public static class IdentityErrors
{
    public static ValidationError ToError(IEnumerable<IdentityError> errors) =>
        new(errors.Select(e => Error.Problem(Code(e.Code), e.Description)).DistinctBy(e => e.Code).ToArray());

    private static string Code(string identityCode) => identityCode switch
    {
        _ when identityCode.StartsWith("Password", StringComparison.Ordinal) => AuthErrors.Password,
        "DuplicateUserName" => "auth.userExists",
        "InvalidUserName" => AuthErrors.UserName,
        _ => "auth.invalid"
    };
}
