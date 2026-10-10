using TableSimulator.Application.Auth;
using TableSimulator.Application.Users;

namespace TableSimulator.Tests.Application;

[TestClass]
public sealed class RoleNamesTests
{
    [TestMethod]
    [DataRow("Informačné panely", "informacne-panely")]
    [DataRow("  Technik haly – noc ", "technik-haly-noc")]
    [DataRow("Žltá/ČERVENÁ 2", "zlta-cervena-2")]
    [DataRow("–––", "role")]
    public void Slug_BezDiakritikyAPomlcky(string displayName, string expected) => Assert.AreEqual(expected, RoleNames.Slug(displayName));

    [TestMethod]
    public void Unique_VynechaVstavaneAPouzite()
    {
        Assert.AreEqual("operator-2", RoleNames.Unique("Operátor", []));
        Assert.AreEqual("panely-3", RoleNames.Unique("Panely", ["panely", "PANELY-2"]));
        Assert.AreEqual(RoleNames.MaxLength, RoleNames.Slug(new string('a', 50)).Length);
    }

    [TestMethod]
    public void Katalog_SpravcaMaVsetkoNezname()
    {
        var catalog = new RoleCatalog();
        catalog.Load([
            new RoleInfo(Roles.Admin, "", []),
            new RoleInfo("panely", "Panely", [Permissions.SimulatorView, Permissions.SignageManage, "simulator.edit"])
        ]);
        CollectionAssert.AreEquivalent(Permissions.All.ToArray(), catalog.PermissionsOf(Roles.Admin).ToArray());
        CollectionAssert.AreEqual(new[] { Permissions.SimulatorView, Permissions.SignageManage }, catalog.PermissionsOf("panely").ToArray());
        Assert.IsTrue(catalog.Covers(Roles.Admin, catalog.PermissionsOf("panely")));
        Assert.IsFalse(catalog.Covers("panely", [Permissions.LinesManage]));
        Assert.IsEmpty(catalog.PermissionsOf("neznama"));
        Assert.IsNull(catalog.Find(Roles.Viewer));
    }
}
