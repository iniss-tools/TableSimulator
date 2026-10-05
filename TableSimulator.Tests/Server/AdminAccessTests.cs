using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Audit;
using TableSimulator.Contracts.Auth;
using TableSimulator.Contracts.Users;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Ochrana a obnova pristupu: limit neuspesnych pokusov o heslo z adresy, obnova spravcu z prikazoveho riadka
/// (<c>--reset-admin</c>) a jeho parameter.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AdminAccessTests
{
    private const string AdminPassword = "heslo-spravcu";

    private static async Task<(SimulatorServer Server, HttpClient Admin)> StartWithAdmin()
    {
        var server = SimulatorServer.Start("""{"requireLogin":true}""");
        var admin = server.Client();
        var setup = await admin.PostAsJsonAsync("/api/auth/setup", new SetupRequest("spravca", "Správca", AdminPassword));
        Assert.AreEqual(HttpStatusCode.OK, setup.StatusCode);
        return (server, admin);
    }

    private static Task<HttpResponseMessage> Login(SimulatorServer server, string userName, string password) =>
        server.Client().PostAsJsonAsync("/api/auth/login", new LoginRequest(userName, password, false));

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task<(int Code, string Output)> Reset(SimulatorServer server, string? userName)
    {
        await using var output = new StringWriter();
        var code = await AdminReset.RunAsync(server.Services, userName, output);
        return (code, output.ToString());
    }

    private static string PasswordOf(string output) =>
        output.Split('\n').Single(l => l.StartsWith("Nove heslo: ", StringComparison.Ordinal))["Nove heslo: ".Length..].Trim();

    [TestMethod]
    public async Task LimitPokusov_PoDesiatichNeuspechochZAdresyAniSpravneHeslo()
    {
        var (server, admin) = await StartWithAdmin();
        await using var host = server;
        admin.Dispose();

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(server, $"nikto{i}", "zle-heslo")).StatusCode);
        var blocked = await Login(server, "spravca", AdminPassword);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.AreEqual("auth.tooManyAttempts", await CodeOf(blocked));
    }

    [TestMethod]
    public void LimitPokusov_OknoAAdresy()
    {
        var time = new ManualTime();
        var context = new DefaultHttpContext();
        var throttle = new LoginThrottle(time, new HttpContextAccessor { HttpContext = context });
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8:1:2::10");

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
        {
            Assert.IsNull(throttle.Blocked());
            throttle.Failed();
            time.Advance(TimeSpan.FromMinutes(1));
        }

        // ina adresa z tej istej siete /64 je tiez zablokovana, ina siet nie
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8:1:2::99");
        Assert.AreEqual(TimeSpan.FromMinutes(5), throttle.Blocked());
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8:1:3::10");
        Assert.IsNull(throttle.Blocked());

        // najstarsi neuspech vypadne z okna
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8:1:2::10");
        time.Advance(TimeSpan.FromMinutes(5));
        Assert.IsNull(throttle.Blocked());
        Assert.AreEqual("192.168.1.5", LoginThrottle.AddressOf(IPAddress.Parse("::ffff:192.168.1.5")));
    }

    [TestMethod]
    public async Task Obnova_ZablokovanyVypnutySpravcaNoveHesloAudit()
    {
        var (server, admin) = await StartWithAdmin();
        await using var host = server;
        using (var scope = server.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByNameAsync("spravca"))!;
            user.IsDisabled = true;
            await users.UpdateAsync(user);
            await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        }

        var (code, output) = await Reset(server, null);

        Assert.AreEqual(0, code, output);
        var password = PasswordOf(output);
        Assert.HasCount(AdminReset.PasswordLength, password);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Login(server, "spravca", AdminPassword)).StatusCode);
        var login = await Login(server, "spravca", password);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        Assert.AreEqual(Roles.Admin, (await login.Content.ReadFromJsonAsync<MeDto>())!.Role);
        admin.Dispose();

        var (_, created) = await Reset(server, "druhy-spravca");
        var second = server.Client();
        Assert.AreEqual(HttpStatusCode.OK, (await second.PostAsJsonAsync("/api/auth/login", new LoginRequest("druhy-spravca", PasswordOf(created), false))).StatusCode);
        var audit = await second.GetFromJsonAsync<AuditPageDto>("/api/audit?action=ResetAdmin");
        CollectionAssert.AreEqual(new[] { "druhy-spravca", "spravca" }, audit!.Entries.Select(e => e.Target).ToArray());
        Assert.IsTrue(audit.Entries.All(e => e.User == AdminReset.Actor));
        Assert.Contains("\"created\":true", audit.Entries[0].Details);
        var list = await second.GetFromJsonAsync<UserDto[]>("/api/users");
        Assert.AreEqual(Roles.Admin, list!.Single(u => u.UserName == "druhy-spravca").Role);
        second.Dispose();
    }

    [TestMethod]
    public async Task Obnova_OperatorDostaneRoluSpravcu()
    {
        var (server, admin) = await StartWithAdmin();
        await using var host = server;
        Assert.IsTrue((await admin.PostAsJsonAsync("/api/users", new CreateUserRequest("operator1", "Operátor", Roles.Operator, "heslo-operatora")))
            .IsSuccessStatusCode);

        var (code, output) = await Reset(server, "operator1");

        Assert.AreEqual(0, code, output);
        var users = await admin.GetFromJsonAsync<UserDto[]>("/api/users");
        Assert.AreEqual(Roles.Admin, users!.Single(u => u.UserName == "operator1").Role);
        admin.Dispose();
    }

    [TestMethod]
    public async Task Obnova_ViacSpravcovBezMenaANeplatneMeno()
    {
        var (server, admin) = await StartWithAdmin();
        await using var host = server;
        admin.Dispose();
        await Reset(server, "druhy");

        var (many, manyOutput) = await Reset(server, null);
        var (invalid, _) = await Reset(server, "zle meno");

        Assert.AreEqual(2, many);
        Assert.Contains("druhy, spravca", manyOutput);
        Assert.AreEqual(1, invalid);
    }

    [TestMethod]
    [DataRow(new[] { "--reset-admin" }, true, null)]
    [DataRow(new[] { "--reset-admin", "spravca" }, true, "spravca")]
    [DataRow(new[] { "--reset-admin", "--data", "x" }, true, null)]
    [DataRow(new[] { "--port", "5000" }, false, null)]
    public void PrikazovyRiadok_ResetAdmin(string[] args, bool reset, string? name)
    {
        var line = CommandLine.Parse(args, AppContext.BaseDirectory);

        Assert.AreEqual(reset, line.ResetAdmin);
        Assert.AreEqual(name, line.ResetAdminName);
    }
}
