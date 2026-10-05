using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Archive;
using static TableSimulator.Tests.Archive.ArchiveReaderTests;

namespace TableSimulator.Tests.Server;

/// <summary>Archiv tabul a hlaseni cez API: dni, stranky s vyberom, chybajuci den a neplatna stranka.</summary>
[TestClass]
[DoNotParallelize]
public sealed class ArchiveApiTests
{
    private static SimulatorServer Start(out string logs)
    {
        var server = SimulatorServer.Start();
        logs = Path.Combine(server.DataFolder, "Logy");
        Directory.CreateDirectory(logs);
        File.WriteAllText(Path.Combine(server.DataFolder, "settings.json"), JsonSerializer.Serialize(new { virtualFolders = new[] { logs } }));
        return server;
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [TestMethod]
    public async Task Archiv_DniAStrankySVyberom()
    {
        await using var server = Start(out var logs);
        using var client = server.Client();
        await File.WriteAllBytesAsync(Path.Combine(logs, "TAB2026-10-04.XML"), ArchiveBytes(
            TrainLine,
            """<T d="2026-10-04" c="14:10:00" s="Dolné Mesto" t="R" v="601" jm=""><t><n>NAST1</n><c n="Čas" s="14:12"/></t></T>""",
            """<T d="2026-10-04" c="14:20:00" s="Dolné Mesto" t="Os" v="7815" jm=""><t><n>NAST1</n><c n="Čas" s="14:25"/></t></T>"""));
        await File.WriteAllBytesAsync(Path.Combine(logs, "REP2026-10-03.XML"), ArchiveBytes(
            """<H d="2026-10-03" c="22:00:00" u="hlásenie" n="Príchod" s="Dolné Mesto" t="Os" v="7813" kp="1" zp="0" e="OK"/>""",
            "03.10.2026\t22:01:00\ttext"));

        var days = await client.GetFromJsonAsync<ArchiveDaysDto>("/api/archive");
        CollectionAssert.AreEqual(new[] { new DateOnly(2026, 10, 4) }, days!.Tables.ToArray());
        CollectionAssert.AreEqual(new[] { new DateOnly(2026, 10, 3) }, days.Reports.ToArray());

        var page = await client.GetFromJsonAsync<TableArchivePageDto>("/api/archive/tables/2026-10-04?train=78&newest=true&limit=1");
        Assert.AreEqual(2, page!.Total);
        Assert.AreEqual("7815", page.Entries.Single().Train!.Number);
        var next = await client.GetFromJsonAsync<TableArchivePageDto>("/api/archive/tables/2026-10-04?train=78&newest=true&offset=1&limit=1");
        Assert.AreEqual("7813", next!.Entries.Single().Train!.Number);
        Assert.AreEqual("Horná Ves", next.Entries[0].Tables[0].Columns[1].Value);

        var reports = await client.GetFromJsonAsync<ReportArchivePageDto>("/api/archive/reports/2026-10-03");
        Assert.AreEqual(1, reports!.Total);
        Assert.AreEqual(1, reports.Skipped);
        Assert.AreEqual(0, reports.Entries[0].ArrivalDelay);
    }

    [TestMethod]
    public async Task Archiv_ChybajuciDenANeplatnaStranka()
    {
        await using var server = Start(out var logs);
        using var client = server.Client();
        await File.WriteAllBytesAsync(Path.Combine(logs, "TAB2026-10-04.XML"), ArchiveBytes(TrainLine));

        var missing = await client.GetAsync("/api/archive/tables/2026-10-05");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual("archive.notFound", await CodeOf(missing));

        var invalid = await client.GetAsync("/api/archive/tables/2026-10-04?limit=0");
        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.AreEqual("archive.page", await CodeOf(invalid));
    }
}
