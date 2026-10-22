using System.Text.Json;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Server;
using TableSimulator.Contracts.Server;
using ToolsCore.Iniss.Updates;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Nova verzia servera z update.json posledneho vydania (adresa v zostave z vydania - ToolsCore\Build\AppRelease.props). Posledny
/// vysledok drzi v pamati; chyba siete ponecha posledne zistenu verziu a len zapise chybu.
/// </summary>
public sealed class ServerUpdates : IServerUpdates, IDisposable
{
    private readonly UpdateSource? _source = UpdateSource.FromAssembly(typeof(ServerUpdates).Assembly);
    private readonly HttpClient? _http;
    private readonly SemaphoreSlim _checking = new(1, 1);
    private readonly TimeProvider _time;
    private readonly ILogger<ServerUpdates> _logger;
    private ServerUpdateDto _status;

    public ServerUpdates(IServerHost host, TimeProvider time, ILogger<ServerUpdates> logger)
    {
        _time = time;
        _logger = logger;
        if (_source is not null)
            _http = UpdateChecker.CreateClient(_source);
        var version = _source is null ? host.Version : ReleaseVersion.Format(_source.Version);
        _status = new ServerUpdateDto(_source is not null, version, null, null, null, null, null, null);
    }

    public ServerUpdateDto Status => Volatile.Read(ref _status);

    public async Task<ServerUpdateDto> CheckAsync(CancellationToken cancellationToken)
    {
        if (_source is null || _http is null)
            return Status;
        await _checking.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var release = await UpdateChecker.FindNewerAsync(_http, _source, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _status, Status with
            {
                Latest = release?.Version,
                Page = release?.Page,
                Installer = release?.Installer?.Url,
                Notes = release?.Notes,
                CheckedAt = _time.GetUtcNow(),
                Error = null
            });
            if (release is not null)
                ServerLog.UpdateAvailable(_logger, release.Version);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidDataException
                                      || e is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            Volatile.Write(ref _status, Status with { CheckedAt = _time.GetUtcNow(), Error = e.Message });
            ServerLog.UpdateCheckFailed(_logger, e.Message);
        }
        finally
        {
            _checking.Release();
        }
        return Status;
    }

    public void Dispose()
    {
        _http?.Dispose();
        _checking.Dispose();
    }
}
