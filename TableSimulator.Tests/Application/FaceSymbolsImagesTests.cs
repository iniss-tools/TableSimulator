using TableSimulator.Application.Boards;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Faces;
using TableSimulator.Core.Boards;
using ContractKind = TableSimulator.Contracts.Faces.FaceElementKind;
using ContractSymbol = TableSimulator.Contracts.Faces.FaceSymbol;

namespace TableSimulator.Tests.Application;

/// <summary>
/// Symboly a obrazky na celach tabul (overenie, prevod jadro ↔ kontrakty, ulozenie predlohy) a pisma znakovej tabule
/// LCD1 podla cisla pisma v profile.
/// </summary>
[TestClass]
public class FaceSymbolsImagesTests
{
    /// <summary>Najmensi PNG (1 × 1 bod) ako data URL.</summary>
    private const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private static BoardFaceDto Face(params FaceElementDto[] elements) => new()
    {
        Name = "Logo a šípka", Margin = new FaceMarginDto(0, 16, 0, 0), Elements = elements
    };

    private static bool Valid(BoardFaceDto face) => new SetFaceCommandValidator().Validate(new SetFaceCommand("logo", face)).IsValid;

    [TestMethod]
    public void Overenie_ObrazokAkoDataUrlASymbolZoSady()
    {
        Assert.IsTrue(Valid(Face(
            new FaceElementDto { Kind = ContractKind.Image, Image = Png, Width = 30, Height = 12 },
            new FaceElementDto { Kind = ContractKind.Symbol, Symbol = ContractSymbol.ArrowsLeftRight, Size = 10 })));

        // len obrazky webu ako data URL, nie adresa ani skript
        Assert.IsFalse(Valid(Face(new FaceElementDto { Kind = ContractKind.Image, Image = "https://example.com/logo.png" })));
        Assert.IsFalse(Valid(Face(new FaceElementDto { Kind = ContractKind.Image, Image = "data:text/html;base64,PHNjcmlwdD4=" })));
        Assert.IsFalse(Valid(Face(new FaceElementDto { Kind = ContractKind.Image, Image = "data:image/png;base64," + new string('A', FaceErrors.MaxImage) })));
        Assert.IsFalse(Valid(Face(new FaceElementDto { Kind = ContractKind.Symbol, Symbol = (ContractSymbol)99 })));
    }

    [TestMethod]
    public void Prevod_SymbolAObrazokTamASpat()
    {
        var face = Face(
            new FaceElementDto { Kind = ContractKind.Symbol, Symbol = ContractSymbol.ArrowDownLeft, Size = 9, Color = "accent" },
            new FaceElementDto { Kind = ContractKind.Image, Image = Png, Width = -2, Height = 0 },
            // obrazok pri inom prvku sa neulozi
            new FaceElementDto { Kind = ContractKind.Text, Text = "SEVER", Image = Png });

        var core = RequestMapper.ToFace(face, "logo");
        var stored = BoardFaces.Deserialize(new FaceRecord("logo", core.Name, BoardFaces.Serialize(core)));
        var back = DtoMapper.ToDto(stored, false);

        Assert.AreEqual(Core.Boards.FaceElementKind.Symbol, core.Elements[0].Kind);
        Assert.AreEqual(Core.Boards.FaceSymbol.ArrowDownLeft, stored.Elements[0].Symbol);
        Assert.AreEqual(ContractSymbol.ArrowDownLeft, back.Elements[0].Symbol);
        Assert.AreEqual(Png, back.Elements[1].Image);
        Assert.AreEqual("", back.Elements[2].Image);
        StringAssert.Contains(BoardFaces.Serialize(core), "\"symbol\":\"arrowDownLeft\"");
        foreach (var symbol in Enum.GetValues<ContractSymbol>())
            Assert.AreEqual(symbol, DtoMapper.ToDto(RequestMapper.ToElement(new FaceElementDto { Kind = ContractKind.Symbol, Symbol = symbol })).Symbol);
    }

    [TestMethod]
    public void PismaLcd1_CisloAZnamePismo()
    {
        var validator = new BoardProfileValidator();
        BoardProfileDto Profile(Dictionary<string, CharFontDto> fonts) => new() { Width = 320, Rows = 4, RowHeight = 16, CharFonts = fonts };

        Assert.IsTrue(validator.Validate(Profile(new() { ["16"] = new CharFontDto(), ["18"] = new CharFontDto("7x14", true) })).IsValid);
        Assert.IsFalse(validator.Validate(Profile(new() { ["64"] = new CharFontDto() })).IsValid, "cislo s bitom inverzie");
        Assert.IsFalse(validator.Validate(Profile(new() { ["x"] = new CharFontDto() })).IsValid);
        Assert.IsFalse(validator.Validate(Profile(new() { ["16"] = new CharFontDto("10x20") })).IsValid, "do bunky 8 bodov sa nezmesti");

        var core = RequestMapper.ToProfile(Profile(new() { [" 17 "] = new CharFontDto(" 6x10 ", true) }));
        Assert.AreEqual(new CharFont("6x10", true), core.CharFonts!["17"]);
        Assert.AreEqual(new CharFontDto("6x10", true), DtoMapper.ToDto(core).CharFonts!["17"]);
        Assert.IsNull(RequestMapper.ToProfile(Profile([])).CharFonts);
    }
}
