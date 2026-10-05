using TableSimulator.Application.Keys;
using TableSimulator.Contracts.Keys;

namespace TableSimulator.Application.Mapping;

/// <summary>Pristupove kluce.</summary>
public static partial class DtoMapper
{
    public static AccessKeyDto ToDto(AccessKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new AccessKeyDto(key.Id, key.Name, AccessKeys.KindOf(key.Kind), key.Role, key.Prefix, Utc(key.CreatedAt), key.CreatedBy,
            key.LastUsedAt is { } used ? Utc(used) : null);
    }

    private static DateTimeOffset Utc(DateTime time) => new(DateTime.SpecifyKind(time, DateTimeKind.Utc));
}
