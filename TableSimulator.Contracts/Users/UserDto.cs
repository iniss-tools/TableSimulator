namespace TableSimulator.Contracts.Users;

/// <summary>Pouzivatel simulatora pre spravu pouzivatelov.</summary>
/// <param name="UserName">prihlasovacie meno (neda sa zmenit)</param>
/// <param name="DisplayName">meno pre zobrazenie</param>
/// <param name="Role">rola (<c>admin</c>, <c>operator</c>, <c>viewer</c>)</param>
/// <param name="Disabled">ucet je vypnuty</param>
/// <param name="LockedUntil">ucet je po neuspesnych pokusoch zablokovany do (UTC); null = nie</param>
/// <param name="CreatedAt">kedy ucet vznikol</param>
/// <param name="LastLoginAt">posledne prihlasenie; null = este nikdy</param>
public sealed record UserDto(
    string UserName,
    string DisplayName,
    string Role,
    bool Disabled,
    DateTimeOffset? LockedUntil,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
