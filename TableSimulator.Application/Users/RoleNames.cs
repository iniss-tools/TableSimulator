using System.Globalization;
using System.Text;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

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
