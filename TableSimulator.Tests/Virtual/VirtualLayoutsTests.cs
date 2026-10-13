using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Tests.Virtual;

/// <summary>Vstavane rozlozenia virtualnych tabul, ich ulozenie a odhad rozlozenia podla sablony INISSView.</summary>
[TestClass]
public sealed class VirtualLayoutsTests
{
    [TestMethod]
    public void BuiltIn_PredvolenePrveAKazdePrejdeOverenim()
    {
        var validator = new SetVirtualLayoutCommandValidator();

        Assert.AreEqual(VirtualLayouts.DefaultId, VirtualLayouts.BuiltIn[0].Id);
        CollectionAssert.IsSubsetOf(new[] { "modra", "cierna", "odchody", "prichody", "pokladne" }, VirtualLayouts.BuiltIn.Select(l => l.Id).ToArray());
        foreach (var layout in VirtualLayouts.BuiltIn)
        {
            Assert.IsTrue(layout.BuiltIn, layout.Id);
            var result = validator.Validate(new SetVirtualLayoutCommand(layout.Id, layout));
            Assert.IsTrue(result.IsValid, $"{layout.Id}: {string.Join(", ", result.Errors.Select(e => e.ErrorCode))}");
        }
    }

    [TestMethod]
    public void Overenie_PrazdnyNazovANadpisSKodom()
    {
        var validator = new SetVirtualLayoutCommandValidator();

        var codes = validator.Validate(new SetVirtualLayoutCommand("moje", new VirtualLayoutDto { Name = " ", Title = null! })).Errors.Select(e => e.ErrorCode).ToList();

        // kazde pravidlo retazca ma svoj kod - web podla neho ukaze text
        CollectionAssert.AreEquivalent(new[] { VirtualErrors.LayoutName, VirtualErrors.LayoutTitle }, codes);
    }

    [TestMethod]
    public void Serialize_TamASpat()
    {
        var layout = VirtualLayouts.FindBuiltIn("odchody")! with { Id = "moje", Name = "Moje", ShowDate = true, Clock = VirtualClock.Minutes };

        var back = VirtualLayouts.Deserialize(new VirtualLayoutRecord("moje", "Moje", VirtualLayouts.Serialize(layout)));

        Assert.IsFalse(back.BuiltIn);
        Assert.AreEqual(VirtualClock.Minutes, back.Clock);
        Assert.IsTrue(back.ShowDate);
        Assert.AreEqual(layout.Colors, back.Colors);
        CollectionAssert.AreEqual(layout.Columns.Select(c => string.Join("+", c.Cells)).ToArray(), back.Columns.Select(c => string.Join("+", c.Cells)).ToArray());
        StringAssert.Contains(VirtualLayouts.Serialize(layout), "\"clock\":\"minutes\"");
    }

    [TestMethod]
    [DataRow("Pokladny_2r", "pokladne")]
    [DataRow("Odjezdy_velka_tabule_12r_Praha_cerna", "cierna")]
    [DataRow("Odj_CSNK", "odchody")]
    [DataRow("OdjezdyEZOP_2r", "odchody")]
    [DataRow("Prj_CSNK", "prichody")]
    [DataRow("Prijezdy_velka_tabule_10r_Praha_modra", "prichody")]
    [DataRow("Odchody web", "odchody")]
    [DataRow("Príchody web", "prichody")]
    [DataRow("Informacie", "")]
    public void Guess_PodlaSablonyAleboNazvu(string text, string layout) => Assert.AreEqual(layout, VirtualLayouts.Guess(text));
}
