using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Backup;

/// <summary>Chyby zalohy a obnovy.</summary>
public static class BackupErrors
{
    /// <summary>Najvacsia zaloha na obnovu.</summary>
    public const long MaxBytes = 8L * 1024 * 1024 * 1024;

    public static Error Format(string detail) => Error.Problem("backup.format", $"The file is not a simulator backup: {detail}");

    public static Error Newer(string migration) =>
        Error.Problem("backup.newer", $"The backup comes from a newer server version (database migration {migration}) - update the server first.");

    public static Error TooLarge() => Error.Problem("backup.tooLarge", "The backup is larger than 8 GB.");

    public static Error NoPending() => Error.NotFound("backup.noPending", "No restore is prepared.");
}
