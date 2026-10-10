using System.Globalization;
using System.Text;
using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Application.Users;

/// <summary>Roly v databaze (implementuje server cez ASP.NET Core Identity); po zmene obnovi <see cref="RoleCatalog" />.</summary>
public interface IRoleService
{
    /// <summary>Vsetky roly s poctom pouzivatelov a klucov.</summary>
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Zalozi vlastnu rolu; vrati jej kluc.</summary>
    Task<Result<string>> CreateAsync(string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    /// <summary>Zmeni nazov (len vlastnej roly) a opravnenia.</summary>
    Task<Result> UpdateAsync(string name, string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    /// <summary>Odstrani vlastnu rolu, ak ju nema ziaden pouzivatel ani kluc.</summary>
    Task<Result> DeleteAsync(string name, CancellationToken cancellationToken);
}

/// <summary>Chyby a kody chyb roli.</summary>
public static class RoleErrors
{
    public const string Name = "roles.name";
    public const string Permissions = "roles.permissions";
    public const string View = "roles.view";

    public static Error NotFound(string name) => Error.NotFound("roles.notFound", $"Role '{name}' does not exist.");

    public static Error Admin() => Error.Conflict("roles.admin", "The administrator role always has all permissions and cannot be changed.");

    public static Error BuiltIn() => Error.Conflict("roles.builtIn", "A built-in role cannot be deleted.");

    public static Error Self() => Error.Conflict("roles.self", "You cannot change your own role.");

    public static Error TooHigh() => Error.Forbidden("roles.tooHigh", "You cannot grant or change permissions you do not have yourself.");

    public static Error Duplicate(string displayName) => Error.Conflict("roles.duplicate", $"A role named '{displayName}' already exists.");

    public static Error InUse(int users, int keys) =>
        Error.Conflict("roles.inUse", $"The role is assigned to {users} user(s) and {keys} access key(s) - assign them another role first.");
}

/// <summary>Kluc vlastnej roly z nazvu: male pismena bez diakritiky a pomlcky (ako identifikator vzhladu).</summary>
public static class RoleNames
{
    /// <summary>Najdlhsi kluc bez cisla navyse.</summary>
    public const int MaxLength = 32;

    /// <summary>Kluc z nazvu; prazdny nazov = <c>role</c>.</summary>
    public static string Slug(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var text = new StringBuilder();
        foreach (var c in displayName.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            var lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                text.Append(lower);
            else if (text.Length > 0 && text[^1] != '-')
                text.Append('-');
        }

        var slug = text.ToString().Trim('-');
        if (slug.Length > MaxLength)
            slug = slug[..MaxLength].TrimEnd('-');
        return slug.Length == 0 ? "role" : slug;
    }

    /// <summary>Kluc, ktory este nema ziadna rola ani vstavana rola (<c>nazov</c>, <c>nazov-2</c>…).</summary>
    public static string Unique(string displayName, IEnumerable<string> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);
        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        taken.UnionWith(Roles.BuiltIn);
        var slug = Slug(displayName);
        var name = slug;
        for (var i = 2; taken.Contains(name); i++)
            name = $"{slug}-{i.ToString(CultureInfo.InvariantCulture)}";
        return name;
    }
}

/// <summary>Nazov a opravnenia roly; <paramref name="requireName" /> = false pri vstavanej roli (nazov sa nemeni).</summary>
public sealed class SetRoleRules : AbstractValidator<SetRoleRequest>
{
    public SetRoleRules(bool requireName = true)
    {
        RuleFor(r => r.DisplayName).Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= Roles.MaxDisplayName).When(_ => requireName)
            .WithErrorCode(RoleErrors.Name).WithMessage($"The role name must have 1-{Roles.MaxDisplayName} characters.");
        RuleFor(r => r.Permissions).NotNull().WithErrorCode(RoleErrors.Permissions);
        When(r => r.Permissions is not null, () =>
        {
            RuleFor(r => r.Permissions).Must(p => p.All(Permissions.All.Contains)).WithErrorCode(RoleErrors.Permissions)
                .WithMessage("Unknown permission.");
            RuleFor(r => r.Permissions).Must(p => p.Contains(Permissions.SimulatorView)).WithErrorCode(RoleErrors.View)
                .WithMessage("Every role needs the overview permission.");
        });
    }
}

/// <summary>Vsetky roly.</summary>
public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>;

public sealed class GetRolesQueryHandler(IRoleService roles) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery query, CancellationToken cancellationToken) =>
        Result.Success(await roles.ListAsync(cancellationToken).ConfigureAwait(false));
}

/// <summary>Zalozi vlastnu rolu.</summary>
public sealed record CreateRoleCommand(SetRoleRequest Request) : ICommand<RoleCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Request.DisplayName;
}

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator() => RuleFor(c => c.Request).NotNull().WithErrorCode(RoleErrors.Name).SetValidator(new SetRoleRules());
}

public sealed class CreateRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<CreateRoleCommand, RoleCreatedDto>
{
    public async Task<Result<RoleCreatedDto>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (!catalog.Covers(user.Role, command.Request.Permissions))
            return Result.Failure<RoleCreatedDto>(RoleErrors.TooHigh());
        var created = await service.CreateAsync(command.Request.DisplayName.Trim(), command.Request.Permissions, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess ? Result.Success(new RoleCreatedDto(created.Value)) : Result.Failure<RoleCreatedDto>(created.Error);
    }
}

/// <summary>Zmeni nazov a opravnenia roly (vstavanej len opravnenia, spravcu nie).</summary>
public sealed record UpdateRoleCommand(string Name, SetRoleRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Name;
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator() =>
        RuleFor(c => c.Request).NotNull().WithErrorCode(RoleErrors.Name).SetValidator((command, _) => new SetRoleRules(!Roles.IsBuiltIn(command.Name)));
}

public sealed class UpdateRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (catalog.Find(command.Name) is not { } role)
            return Result.Failure(RoleErrors.NotFound(command.Name));
        if (role.Name == Roles.Admin)
            return Result.Failure(RoleErrors.Admin());
        if (role.Name == user.Role)
            return Result.Failure(RoleErrors.Self());
        if (!catalog.Covers(user.Role, role.Permissions) || !catalog.Covers(user.Role, command.Request.Permissions))
            return Result.Failure(RoleErrors.TooHigh());
        return await service.UpdateAsync(role.Name, (command.Request.DisplayName ?? "").Trim(), command.Request.Permissions, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Odstrani vlastnu rolu.</summary>
public sealed record DeleteRoleCommand(string Name) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Name;
}

public sealed class DeleteRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        if (catalog.Find(command.Name) is not { } role)
            return Result.Failure(RoleErrors.NotFound(command.Name));
        if (Roles.IsBuiltIn(role.Name))
            return Result.Failure(RoleErrors.BuiltIn());
        if (!catalog.Covers(user.Role, role.Permissions))
            return Result.Failure(RoleErrors.TooHigh());
        return await service.DeleteAsync(role.Name, cancellationToken).ConfigureAwait(false);
    }
}
