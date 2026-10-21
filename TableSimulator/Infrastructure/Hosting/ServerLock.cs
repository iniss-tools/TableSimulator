using System.Text;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Zamok priecinka nastaveni: server drzi subor <c>server.lock</c> otvoreny na zapis po cely beh a po starte don zapise
/// adresu webu. Dalsi proces nad tym istym priecinkom server nespusti, len otvori adresu bieziaceho (<see cref="RunningServer" />).
/// Subor sa po skonceni procesu zmaze sam, aj po jeho pade.
/// </summary>
public sealed class ServerLock : IDisposable
{
    /// <summary>Subor zamku v priecinku nastaveni.</summary>
    public const string FileName = "server.lock";

    private readonly FileStream _stream;

    private ServerLock(FileStream stream) => _stream = stream;

    /// <summary>Zamkne priecinok nastaveni; <see langword="null" />, ak ho uz drzi iny server.</summary>
    public static ServerLock? TryAcquire(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        try
        {
            // citat smu ostatni (adresa), zapisovat nikto - druhy pokus o zamok skonci porusenim zdielania
            return new ServerLock(new FileStream(Path.Combine(dataFolder, FileName), FileMode.Create, FileAccess.Write,
                FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose));
        }
        catch (IOException e) when (e.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021))
        {
            return null;
        }
    }

    /// <summary>Zapise adresu webu bieziaceho servera.</summary>
    public void Publish(string url)
    {
        var bytes = Encoding.UTF8.GetBytes(url);
        _stream.SetLength(0);
        _stream.Write(bytes);
        _stream.Flush(true);
    }

    /// <summary>
    /// Adresa servera, ktory drzi zamok priecinka - pocka, kym ju server po starte zapise; <see langword="null" />, ak
    /// do <paramref name="wait" /> nijaka nie je.
    /// </summary>
    public static async Task<string?> ReadUrlAsync(string dataFolder, TimeSpan wait, CancellationToken cancellationToken = default)
    {
        var file = Path.Combine(dataFolder, FileName);
        var until = DateTime.UtcNow + wait;
        while (true)
        {
            try
            {
                await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                if ((await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).Trim() is { Length: > 0 } url)
                    return url;
            }
            catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
            {
                // server sa este len spusta (sluzba) alebo prave skoncil
            }

            if (DateTime.UtcNow >= until)
                return null;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => _stream.Dispose();
}
