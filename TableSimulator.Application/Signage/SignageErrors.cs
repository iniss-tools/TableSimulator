using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Signage;

/// <summary>Chyby a kody chyb informacnych panelov.</summary>
public static class SignageErrors
{
    public const string Id = "signage.id";
    public const string Name = "signage.name";
    public const string Note = "signage.note";
    public const string Kind = "signage.kind";
    public const string Duration = "signage.duration";
    public const string VirtualBoard = "signage.virtualBoard";
    public const string Board = "signage.board";
    public const string Paragraphs = "signage.paragraphs";
    public const string Color = "signage.color";
    public const string Media = "signage.media";
    public const string Url = "signage.url";
    public const string Period = "signage.period";
    public const string Dates = "signage.dates";
    public const string Weekdays = "signage.weekdays";
    public const string Window = "signage.window";
    public const string Items = "signage.items";
    public const string Playlists = "signage.playlists";
    public const string Default = "signage.default";
    public const string Group = "signage.group";
    public const string MediaName = "signage.mediaName";

    /// <summary>Najdlhsie trvanie spravy v sekundach.</summary>
    public const int MaxDuration = 3600;

    /// <summary>Najviac odsekov textovej spravy.</summary>
    public const int MaxParagraphs = 20;

    /// <summary>Najviac poloziek playlistu.</summary>
    public const int MaxItems = 200;

    public static Error NotFound(SignageEntity entity, int id) =>
        Error.NotFound($"signage.{Key(entity)}NotFound", string.Create(CultureInfo.InvariantCulture, $"Signage {Key(entity)} {id} does not exist."));

    public static Error InUse(SignageEntity entity, IEnumerable<string> users) =>
        Error.Conflict($"signage.{Key(entity)}InUse", $"Signage {Key(entity)} is used by: {string.Join(", ", users)}.");

    public static Error UnknownVirtual(int id) =>
        Error.Problem("signage.unknownVirtual", string.Create(CultureInfo.InvariantCulture, $"Virtual board {id} does not exist."));

    public static Error UnknownMessages(IEnumerable<int> ids) =>
        Error.Problem("signage.unknownMessage", $"Messages {string.Join(", ", ids)} do not exist.");

    public static Error UnknownPlaylists(IEnumerable<int> ids) =>
        Error.Problem("signage.unknownPlaylist", $"Playlists {string.Join(", ", ids)} do not exist.");

    public static Error UnknownGroup(int id) =>
        Error.Problem("signage.unknownGroup", string.Create(CultureInfo.InvariantCulture, $"Panel group {id} does not exist."));

    public static Error UnknownMedia() => Error.Problem("signage.unknownMedia", "The uploaded file does not exist.");

    public static Error MediaKind() => Error.Problem("signage.mediaKind", "The uploaded file is not of the message kind (image or video).");

    public static Error MediaNotFound() => Error.NotFound("signage.mediaNotFound", "The file does not exist.");

    public static Error ImportFormat() => Error.Problem("signage.importFormat", "The file is not a backup of information panels.");

    public static Error ImportInvalid(string detail) => Error.Problem("signage.importInvalid", $"The backup is not consistent: {detail}");

    public static Error ImportTooLarge() => Error.Problem("signage.importTooLarge", "The backup is too large.");

    public static Error MediaTooLarge() =>
        Error.Problem("signage.mediaTooLarge", string.Create(CultureInfo.InvariantCulture, $"The file is larger than {SignageMediaStore.MaxBytes / 1024 / 1024} MB."));

    /// <summary>Kluc entity v kodoch chyb a v audite.</summary>
    public static string Key(SignageEntity entity) => entity switch
    {
        SignageEntity.Message => "message",
        SignageEntity.Playlist => "playlist",
        SignageEntity.Group => "group",
        _ => "panel"
    };
}
