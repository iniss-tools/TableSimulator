import { describe, expect, test } from "bun:test";
import type { Glyph } from "./fonts";
import { PixelCellGlyphSource, PixelGlyphSource, pixelFontFor } from "./pixel-fonts";

/** Znak ako riadky # a . (len riadky s kresbou). */
function picture(glyph: Glyph): string[] {
  const rows: string[] = [];
  for (let y = 0; y < glyph.height; y++) {
    let row = "";
    for (let x = 0; x < glyph.width; x++) row += glyph.isSet(x, y) ? "#" : ".";
    if (row.includes("#")) rows.push(row);
  }
  return rows;
}

// cislo pisma ELEN: bity 4-5 = rez (0 NEPROP, 1 PROP, 2 PROP_BOLD), bity 0-1 = farba
const NEPROP = 0x41;
const PROP = 0x51;
const BOLD = 0x61;

describe("bodove pismo bez pisma ELEN", () => {
  test("najvacsie pismo do vysky riadku a bunky", () => {
    expect(pixelFontFor(10)?.name).toBe("6x10");
    expect(pixelFontFor(16)?.name).toBe("9x15");
    expect(pixelFontFor(16, 7)?.name).toBe("7x14");
    expect(pixelFontFor(24)?.name).toBe("10x20");
    expect(pixelFontFor(6)).toBeNull();
    expect(PixelGlyphSource.forRow(6)).toBeNull();
  });

  test("pevna sirka ako pisma ELEN, proporcionalne kresba a bod medzery", () => {
    const source = PixelGlyphSource.forRow(10)!;
    expect(source.glyph(0x69, "i", NEPROP)!.width).toBe(6);
    const prop = source.glyph(0x69, "i", PROP)!;
    expect(prop.width).toBeLessThan(6);
    // tucne o bod sirsie
    expect(source.glyph(0x69, "i", BOLD)!.width).toBe(prop.width + 1);
    expect(source.glyph(0x20, " ", PROP)!.width).toBe(3);
    // od 16 riadkov pevna sirka 8
    expect(PixelGlyphSource.forRow(16)!.glyph(0x41, "A", NEPROP)!.width).toBe(8);
  });

  test("dvojbodka zo stvorcekov 2x2, nie z kriziku", () => {
    const colon = picture(PixelGlyphSource.forRow(10)!.glyph(0x3a, ":", PROP)!);
    // proporcionalne: kresba a bod medzery
    expect(colon).toEqual(["##.", "##.", "##.", "##."]);
  });

  test("ď s apostrofom a znak mimo CP1250 bez diakritiky", () => {
    const source = PixelGlyphSource.forRow(16)!;
    const d = picture(source.glyph(0, "d", PROP)!);
    const dCaron = picture(source.glyph(0, "ď", PROP)!);
    // apostrof pribudne nad tahom vpravo, telo pismena ostane
    expect(dCaron.length).toBeGreaterThan(d.length);
    expect(dCaron.slice(-d.length).map((r) => r.slice(0, d[0].length))).toEqual(d);
    // ẽ nie je v CP1250 - pismeno bez diakritiky
    expect(picture(source.glyph(0, "ẽ", PROP)!)).toEqual(picture(source.glyph(0, "e", PROP)!));
  });

  test("text v strede riadku", () => {
    const glyph = PixelGlyphSource.forRow(12)!.glyph(0x48, "H", NEPROP)!;
    expect(glyph.height).toBe(12);
    expect(glyph.isSet(0, 0)).toBe(false);
  });

  test("znakova tabula: pismo 7x14 do bunky 8x16 s volnym poslednym stlpcom", () => {
    const source = PixelCellGlyphSource.forCell(8, 16)!;
    const glyph = source.glyph(0x4d, "M")!;
    expect(glyph.width).toBe(8);
    expect(glyph.height).toBe(16);
    for (let y = 0; y < 16; y++) expect(glyph.isSet(7, y)).toBe(false);
    expect(picture(glyph).length).toBeGreaterThan(6);
  });
});
