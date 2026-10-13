using System.Text;
using TableSimulator.Application.Frames;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Protocols.Apel;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Tests.Application;

/// <summary>Vlozenie spravy na linku ako od INISSu (nastroj MCP send_frame).</summary>
[TestClass]
public sealed class SendFrameCommandTests
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Linka 10 (APEL) s tabulami 10-129 a 10-130.</summary>
    private static Simulator Create()
    {
        var sim = new Simulator();
        sim.SetLine(new LineConfig(10, 10, "TCP://127.0.0.1:47010") { AutoLearn = false, Manufacturer = "APEL" });
        foreach (var address in new[] { 0x81, 0x82 })
            sim.SetBoard(new BoardConfig(10, address, "APEL") { Profile = BoardProfile.DefaultFor("APEL") });
        return sim;
    }

    private static string Text(int address, string text) =>
        Convert.ToHexString(ApelFrame.Build(address, new ApelRecord(0, Encoding.ASCII.GetBytes(text))));

    private Task<TableSimulator.Application.Abstractions.Results.Result<TableSimulator.Contracts.Frames.SentFrameDto>> Send(Simulator sim, int line, string hex) =>
        new SendFrameCommandHandler(sim).Handle(new SendFrameCommand(line, hex), TestContext.CancellationToken);

    [TestMethod]
    public async Task Sprava_OdpovedeAZmenenaTabula()
    {
        var sim = Create();

        var result = (await Send(sim, 10, Text(0x81, "Praha"))).Value;

        Assert.AreEqual(Convert.ToHexString(ApelFrame.Reply(ApelFrame.DefaultComputerAddress, 0x81, ApelFrame.StateOk)), result.Replies.Single().Hex);
        Assert.AreEqual("10-129", result.Boards.Single().Id);
        Assert.AreEqual("Praha", result.Boards[0].Rows[0]);
        // ako od INISSu - v logu a v statistike linky
        Assert.IsNotEmpty(sim.LogSince(0));
        Assert.IsGreaterThan(0L, sim.Read(s => s.Lines.Single().Stats.Frames));
    }

    [TestMethod]
    public async Task Linka_PripojenyInissVypadokANeznama()
    {
        var sim = Create();
        var hex = Text(0x81, "Praha");

        sim.SetLineStatus(10, LineStatus.Connected, null);
        var connected = await Send(sim, 10, hex);
        sim.SetLineStatus(10, LineStatus.Listening, null);
        sim.SetLineOutage(10, true);
        var outage = await Send(sim, 10, hex);
        var unknown = await Send(sim, 11, hex);

        Assert.AreEqual("frames.inissConnected", connected.Error.Code);
        Assert.AreEqual("frames.lineOutage", outage.Error.Code);
        Assert.AreEqual("line.notFound", unknown.Error.Code);
        Assert.IsTrue(sim.Read(s => s.FindBoard("10-129")!.Display is CharDisplay));
    }

    [TestMethod]
    public void Hex_MedzeryPomlckyANeplatne()
    {
        var validator = new SendFrameCommandValidator();

        CollectionAssert.AreEqual(new byte[] { 0x10, 0x02, 0x81 }, SendFrameCommand.Parse("10-02 81"));
        CollectionAssert.AreEqual(new byte[] { 0xAB }, SendFrameCommand.Parse("ab"));
        foreach (var bad in new[] { "", "   ", "abc", "zz", new string('0', (SendFrameCommandValidator.MaxBytes + 1) * 2) })
            Assert.AreEqual(SendFrameCommandValidator.HexCode, validator.Validate(new SendFrameCommand(10, bad)).Errors.Single().ErrorCode, bad);
        Assert.IsTrue(validator.Validate(new SendFrameCommand(10, "10 02 81 03")).IsValid);
        Assert.AreEqual("line.number", validator.Validate(new SendFrameCommand(0, "00")).Errors.Single().ErrorCode);
    }
}
