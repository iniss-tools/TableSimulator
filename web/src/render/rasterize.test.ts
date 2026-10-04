import { describe, expect, test } from "bun:test";
import type { Board, Run } from "../api";
import { ElenGlyphSource, fontIndex, tallDigitCode, type ElenFontSet } from "./fonts";
import { blinkPhase, COLOR_NONE, isAnimated, rasterize } from "./rasterize";

// pismo 3 body vysoke, znak 'A' (0x41) 2 body siroky: horny riadok plny, spodny lavy bod
function fontSet(): ElenFontSet {
  const glyphs = Array.from({ length: 256 }, () => new Uint8Array(3));
  glyphs[0x41] = new Uint8Array([0b1100_0000, 0, 0b1000_0000]);
  glyphs[0x15] = new Uint8Array([0b1000_0000, 0b1000_0000, 0b1000_0000]); // vysoka 0
  const widths = Array.from({ length: 256 }, () => 2);
  return { file: "t.bin", name: "t", fonts: [{ name: "F0", maxWidth: 2, height: 3, proportional: false, bytesPerRow: 1, widths, glyphs }] };
}

function run(x: number | null, text: string, font = 0x42): Run {
  return { x, font, codes: [...text].map((c) => c.charCodeAt(0)), text };
}

function board(rows: Board["display"]["rows"], extra: Partial<Board["display"]> = {}): Board {
  return {
    id: "1-1",
    line: 1,
    address: 1,
    name: "",
    manufacturer: "ELEN10",
    design: null,
    fault: { kind: "none", delayMs: 0 },
    autoCreated: false,
    version: 1,
    stats: { frames: 0, acks: 0, naks: 0, unanswered: 0, lastFrameAt: null },
    profile: {
      width: 8,
      rows: 2,
      rowHeight: 3,
      fontFile: "t.bin",
      fontMap: null,
      alternationMs: 1000,
      blinkOnMs: 500,
      blinkOffMs: 500,
      scrollSpeed: 10,
      movingFont: 0x52,
      autoSize: false,
      monochrome: false,
      swapRedGreen: false,
      header: null,
    },
    secondAddress: null,
    display: {
      family: "elen",
      brightness: 99,
      brightnessGreen: 99,
      flashOnMs: null,
      flashOffMs: null,
      clock: null,
      clockSetAt: null,
      rows,
      movingRow: 0,
      movingCodes: null,
      movingText: null,
      ...extra,
    },
  };
}

const at = (frame: { width: number; color: Uint8Array }, x: number, y: number) => frame.color[y * frame.width + x];

describe("rasterize", () => {
  test("riadky sa cisluju odspodu, kurzor urcuje polohu", () => {
    const source = new ElenGlyphSource(fontSet(), null);
    const frame = rasterize(board([{ row: 1, elements: [{ type: "run", run: run(2, "A", 0x52), parts: null }] }]), source, 0);

    // riadok 1 = spodny = body 3-5
    expect(at(frame, 2, 3)).toBe(2);
    expect(at(frame, 3, 3)).toBe(2);
    expect(at(frame, 2, 5)).toBe(2);
    expect(at(frame, 3, 5)).toBe(0);
    expect(at(frame, 2, 0)).toBe(0);
  });

  test("usek bez kurzora pokracuje za predchadzajucim", () => {
    const source = new ElenGlyphSource(fontSet(), null);
    const frame = rasterize(
      board([{ row: 2, elements: [{ type: "run", run: run(0, "A", 0x51), parts: null }, { type: "run", run: run(null, "A", 0x40), parts: null }] }]),
      source,
      0,
    );

    expect(at(frame, 0, 0)).toBe(1);
    expect(at(frame, 2, 0)).toBe(COLOR_NONE);
  });

  test("striedanie podla casu", () => {
    const source = new ElenGlyphSource(fontSet(), null);
    const b = board([{ row: 2, elements: [{ type: "alt", run: null, parts: [[run(0, "A")], [run(4, "A")]] }] }]);

    expect(at(rasterize(b, source, 0), 0, 0)).not.toBe(0);
    expect(at(rasterize(b, source, 1500), 0, 0)).toBe(0);
    expect(at(rasterize(b, source, 1500), 4, 0)).not.toBe(0);
    expect(isAnimated(b)).toBe(true);
  });

  test("zhasnuta tabula nema ziadny bod", () => {
    const source = new ElenGlyphSource(fontSet(), null);
    const frame = rasterize(board([{ row: 2, elements: [{ type: "run", run: run(0, "A"), parts: null }] }], { brightness: 0 }), source, 0);

    expect(frame.color.every((c) => c === 0)).toBe(true);
  });

  test("vysoke cislice a vyber pisma", () => {
    expect(tallDigitCode(0x30)).toBe(0x15);
    expect(tallDigitCode(0x39)).toBe(0x1e);
    expect(tallDigitCode(0x20)).toBe(0x14);
    expect(tallDigitCode(0x3a)).toBe(0x1f);
    expect(fontIndex(0x62, null)).toBe(2);
    expect(fontIndex(0x8642, null)).toBe(6);
    expect(fontIndex(0x52, { face1: 3 })).toBe(3);

    const source = new ElenGlyphSource(fontSet(), null);
    const frame = rasterize(board([{ row: 2, elements: [{ type: "run", run: run(0, "0", 0x5a), parts: null }] }]), source, 0);
    expect(at(frame, 0, 1)).toBe(2);
  });

  test("faza blikania podla profilu a prikazu 9", () => {
    const b = board([]);
    expect(blinkPhase(b.profile, 100)).toBe(true);
    expect(blinkPhase(b.profile, 600)).toBe(false);
    // prikaz 9: svieti 1,0 s, nesvieti 0,5 s
    const d = { flashOnMs: 1000, flashOffMs: 500 };
    expect(blinkPhase(b.profile, 900, d)).toBe(true);
    expect(blinkPhase(b.profile, 1200, d)).toBe(false);
    expect(blinkPhase(b.profile, 1600, d)).toBe(true);
  });
});
