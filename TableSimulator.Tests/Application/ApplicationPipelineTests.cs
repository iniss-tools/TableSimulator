using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Boards;
using TableSimulator.Application.Decorators;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Boards;
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

    private static ServiceProvider Create(out Simulator simulator, out TestLive live)
    {
        simulator = new Simulator();
        live = new TestLive();
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(simulator);
        services.AddSingleton<TransportManager>();
        services.AddSingleton(new AppPaths(folder, Path.Combine(folder, "config.json"), Path.Combine(folder, "wwwroot")));
        services.AddSingleton<ISettingsStore>(new TestSettings());
        services.AddSingleton<ILiveNotifier>(live);
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
}
