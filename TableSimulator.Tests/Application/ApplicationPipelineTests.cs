using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Decorators;
using TableSimulator.Application.Https;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Https;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Settings;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Aplikacna vrstva cez DI ako v serveri: handlery obalene overenim (FluentValidation) a zaznamom, vysledky a chyby.
/// </summary>
[TestClass]
public class ApplicationPipelineTests
{
    private sealed class TestSettings : ISettingsStore
    {
        public AppSettings Current { get; private set; } = new();

        public void Save(AppSettings settings) => Current = settings;
    }

    private sealed class TestLive : ILiveNotifier
    {
        public List<LiveMessage> Messages { get; } = [];

        public void Publish(LiveMessage message) => Messages.Add(message);
    }

    private sealed class TestAudit : IAuditLog
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEntry>> ListAsync(long? before, string? user, string? action, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AuditEntry>>(Entries);

        public Task<int> DeleteOlderThanAsync(DateTime time, CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class TestUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserName => "operator1";
        public string? DisplayName => "Operátor";
        public string? Role => Roles.Operator;
        public string? Address => "192.168.1.20";
        public bool IsAccessKey => false;
    }

    /// <summary>Certifikaty: subor "zly.pfx" sa neda pouzit, ostatne ano.</summary>
    private sealed class TestCertificates : IHttpsCertificates
    {
        public HttpsStatusDto Status() => new(false, 0, false, HttpsSource.Generated, "", "", "", null, [], null, null);

        public HttpsCertificateFile? TrustedCertificate() => null;

        public Result Check(string certificate, string key, string password) =>
            certificate == "zly.pfx" ? Result.Failure(Error.Problem(HttpsErrors.CertificateInvalid, "Bad certificate.")) : Result.Success();
    }

    /// <summary>Sifrovanie len predponou - test vidi, co sa ulozilo.</summary>
    private sealed class TestSecrets : ISecretProtector
    {
        public string Protect(string value) => value.Length == 0 ? "" : "enc:" + value;

        public string Unprotect(string value) => value.StartsWith("enc:", StringComparison.Ordinal) ? value[4..] : "";
    }

    private static ServiceProvider Create(out Simulator simulator, out TestLive live) => Create(out simulator, out live, new TestAudit());

    private static ServiceProvider Create(out Simulator simulator, out TestLive live, TestAudit audit)
    {
        simulator = new Simulator();
        live = new TestLive();
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(simulator);
        services.AddSingleton<TransportManager>();
        services.AddSingleton(new AppPaths(folder, Path.Combine(folder, "wwwroot")));
        services.AddSingleton<ISettingsStore>(new TestSettings());
        services.AddSingleton<ILiveNotifier>(live);
        services.AddSingleton<IAuditLog>(audit);
        services.AddSingleton<ICurrentUser>(new TestUser());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHttpsCertificates, TestCertificates>();
        services.AddSingleton<ISecretProtector, TestSecrets>();
        services.AddApplication();
        return services.BuildServiceProvider();
    }

    private static SetLineRequest Pipe(int tableClass = 4) => new()
    {
        TableClass = tableClass,
        // rura: simulator sa k nej len pokusa pripojit, neobsadi port
        Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N")
    };

    [TestMethod]
    public async Task Handler_ObalenyZaznamomAOverenim()
    {
        await using var provider = Create(out _, out _);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetLineCommand>>();

        Assert.IsInstanceOfType<LoggingCommandHandler<SetLineCommand>>(handler);
    }

    [TestMethod]
    public async Task Prikaz_ZapisanyDoAuditu()
    {
        var audit = new TestAudit();
        await using var provider = Create(out _, out _, audit);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetLineCommand>>();

        await handler.Handle(new SetLineCommand(500, Pipe()), CancellationToken.None);

        var entry = audit.Entries.Single();
        Assert.AreEqual(("operator1", "SetLine", "500", false, "validation", "192.168.1.20"),
            (entry.User, entry.Action, entry.Target, entry.Success, entry.Error, entry.Address));
        StringAssert.Contains(entry.Details, "\"tableClass\":4");
    }

    [TestMethod]
    public void AuditDetails_BezHesielAVelkychObsahov()
    {
        var login = AuditDetails.Of(new LoginCommand("novak", "tajne-heslo", true));
        var upload = AuditDetails.Of(new TableSimulator.Application.Recordings.UploadRecordingCommand("a.tsrec", new byte[1000]));
        var long_ = AuditDetails.Of(new TableSimulator.Application.Recordings.DeleteRecordingCommand(new string('x', 500)));

        Assert.AreEqual("""{"userName":"novak","password":"***","remember":true}""", login);
        Assert.AreEqual("""{"fileName":"a.tsrec"}""", upload);
        Assert.AreEqual("{\"name\":\"" + new string('x', AuditDetails.MaxText) + "…\"}", long_);
        StringAssert.Contains(AuditDetails.Of(new TableSimulator.Application.Virtual.DeleteVirtualLayoutCommand("príchody")), "príchody");
        Assert.AreEqual("", AuditDetails.Of(new LogoutCommand()));

        // kody listov rozlisuju velke a male pismena
        var flaps = new FlapProfileDto
        {
            Lists = new Dictionary<string, IReadOnlyDictionary<string, string>> { ["Ciel"] = new Dictionary<string, string> { ["C"] = "BUDAPEST", ["c"] = "*BUDAPEST" } }
        };
        var station = AuditDetails.Of(new TableSimulator.Application.Station.ImportStationCommand(
            new Contracts.Station.StationImportRequest([], [new Contracts.Station.StationBoardDto(6, 20, "FERS") { Flaps = flaps }])));
        StringAssert.Contains(station, "\"c\":\"*BUDAPEST\"");
    }

    [TestMethod]
    public async Task NeplatnyPrikaz_ChybyOverenieBezVykonania()
    {
        await using var provider = Create(out var simulator, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetLineCommand>>();

        var result = await handler.Handle(new SetLineCommand(0, Pipe(99) with { Endpoint = "xx" }), CancellationToken.None);

        var error = result.Error as ValidationError;
        Assert.IsNotNull(error);
        CollectionAssert.AreEquivalent(new[] { LineErrors.Number, LineErrors.TableClass, LineErrors.Endpoint }, error.Errors.Select(e => e.Code).ToArray());
        Assert.AreEqual(0, simulator.Read(s => s.Lines.Count()));
        StringAssert.Contains(error.Errors[0].Description, "Number", "popis je anglicky, bez ohladu na jazyk systemu");
    }

    [TestMethod]
    public async Task PlatnyPrikaz_ZalozLinku()
    {
        await using var provider = Create(out var simulator, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetLineCommand>>();

        var result = await handler.Handle(new SetLineCommand(5, Pipe() with { Name = "Hala", Manufacturer = "ELEN16" }), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var line = simulator.Read(s => s.FindLine(5));
        Assert.AreEqual("Hala", line?.Name);
        Assert.AreEqual("ELEN16", line?.Manufacturer);
    }

    [TestMethod]
    public async Task TabulaNaNeexistujucejLinke_NenajdenaLinka()
    {
        await using var provider = Create(out _, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetBoardCommand>>();

        var result = await handler.Handle(new SetBoardCommand(55, 1, new SetBoardRequest { Manufacturer = "ELEN" }), CancellationToken.None);

        Assert.AreEqual(ErrorType.NotFound, result.Error.Type);
        Assert.AreEqual("line.notFound", result.Error.Code);
    }

    [TestMethod]
    public async Task NeplatnyProfilTabule_KodyChyb()
    {
        await using var provider = Create(out _, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetBoardCommand>>();
        var request = new SetBoardRequest
        {
            Manufacturer = "ELEN",
            SecondAddress = 1,
            Profile = new BoardProfileDto { Width = 2, Rows = 0 },
            Fault = new BoardFaultDto(FaultKind.Status, Status: 300)
        };

        var result = await handler.Handle(new SetBoardCommand(1, 1, request), CancellationToken.None);

        var codes = ((ValidationError)result.Error).Errors.Select(e => e.Code).ToArray();
        CollectionAssert.AreEquivalent(new[] { BoardErrors.SecondAddress, BoardErrors.Width, BoardErrors.Rows, BoardErrors.FaultStatus }, codes);
    }

    [TestMethod]
    public async Task NenajdenaTabula_Chyba()
    {
        await using var provider = Create(out _, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetBoardQuery, BoardDto>>();

        var result = await handler.Handle(new GetBoardQuery("9-9"), CancellationToken.None);

        Assert.AreEqual("board.notFound", result.Error.Code);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    [TestMethod]
    public async Task ZmenaNastavenia_UlozOznamARestartPriPorte()
    {
        await using var provider = Create(out _, out var live);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSettingsCommand, SettingsUpdatedDto>>();

        var result = await handler.Handle(new UpdateSettingsCommand(new UpdateSettingsRequest { HttpPort = 5999, FontsFolder = "  C:\\Fonts  " }),
            CancellationToken.None);

        Assert.IsTrue(result.Value.Restart);
        var settings = provider.GetRequiredService<ISettingsStore>().Current;
        Assert.AreEqual(5999, settings.HttpPort);
        Assert.AreEqual("C:\\Fonts", settings.FontsFolder);
        Assert.IsInstanceOfType<StructureMessage>(live.Messages.Single());
    }

    [TestMethod]
    public async Task ZmenaNastavenia_HttpsHesloSifrovaneANeplatnyCertifikat()
    {
        await using var provider = Create(out _, out _);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSettingsCommand, SettingsUpdatedDto>>();
        var store = provider.GetRequiredService<ISettingsStore>();
        Task<Result<SettingsUpdatedDto>> Update(string certificate, string? password, int httpsPort = 5471) =>
            handler.Handle(new UpdateSettingsCommand(new UpdateSettingsRequest
            {
                HttpPort = AppSettings.DefaultHttpPort, HttpsPort = httpsPort, HttpsCertificate = certificate, HttpsPassword = password
            }), CancellationToken.None);

        var set = await Update(" \"C:/certs/server.pfx\" ", "tajne");
        var kept = await Update("C:/certs/server.pfx", null);
        Assert.AreEqual("enc:tajne", store.Current.HttpsPassword);
        var cleared = await Update("C:/certs/server.pfx", "", httpsPort: 5471);
        var bad = await Update("zly.pfx", null);
        var samePort = await Update("", null, AppSettings.DefaultHttpPort);

        Assert.IsTrue(set.Value.Restart);
        Assert.AreEqual("C:/certs/server.pfx", store.Current.HttpsCertificate);
        Assert.IsFalse(kept.Value.Restart);
        Assert.IsTrue(cleared.IsSuccess);
        Assert.AreEqual("", store.Current.HttpsPassword);
        Assert.AreEqual(HttpsErrors.CertificateInvalid, bad.Error.Code);
        Assert.AreEqual(SettingsErrors.HttpsPort, ((ValidationError)samePort.Error).Errors[0].Code);
    }
}
