using TableSimulator.Application.Settings;

namespace TableSimulator.Application.Signage;

/// <summary>Nahrate obrazky a videa v priecinku <c>media</c> priecinka nastaveni.</summary>
public sealed class SignageMediaStore(AppPaths paths)
{
    /// <summary>Najvacsi nahraty subor.</summary>
    public const long MaxBytes = 512L * 1024 * 1024;

    /// <summary>Podporovane pripony a typ obsahu (bez SVG - mohlo by obsahovat skript).</summary>
    public static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm"
    };

    /// <summary>Video (inak obrazok).</summary>
    public static bool IsVideo(string contentType) => contentType.StartsWith("video/", StringComparison.Ordinal);

    public string PathOf(string fileName) => Path.Combine(paths.MediaFolder, fileName);

    /// <summary>Zapise obsah do suboru; vrati velkost, null = subor je vacsi nez <see cref="MaxBytes" /> (nic nezapise).</summary>
    public async Task<long?> WriteAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        Directory.CreateDirectory(paths.MediaFolder);
        var path = PathOf(fileName);
        long size = 0;
        var buffer = new byte[81920];
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, useAsync: true);
        await using (stream.ConfigureAwait(false))
        {
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                size += read;
                if (size > MaxBytes)
                    break;
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }

        if (size <= MaxBytes)
            return size;
        File.Delete(path);
        return null;
    }

    /// <summary>Odstrani subory (chybajuce preskoci).</summary>
    public void Delete(IEnumerable<SignageMediaRecord> media)
    {
        foreach (var file in media)
        {
            try
            {
                File.Delete(PathOf(file.FileName));
            }
            catch (IOException)
            {
                // subor prave cita panel - zostane, zaznam v databaze nie
            }
        }
    }
}
