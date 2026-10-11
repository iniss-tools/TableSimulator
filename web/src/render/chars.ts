// Rasterizacia znakovej tabule Elektrocas (LCD, LCD1, ERP) na body: znak = bunka 8 bodov × vyska riadku, riadky
// zhora od 0. Co tabula v danom okamihu ukazuje (vrstvy striedania, blikanie), urcuje tento subor; ako body
// vyzeraju, rozhoduje renderer podla vzhladu - rovnako ako pri ELEN.
import type { BoardDto as Board, CharRowDto as CharRow } from "../api/generated/types.gen";
import type { Glyph, GlyphSource } from "./fonts";
import { COLOR_NONE, type DotFrame } from "./frame";

/** Sirka znaku v bodoch - INISS pocita stlpec ako START / 8. */
export const CHAR_WIDTH = 8;

/** Pismo LCD1: 0x40 inverzne, 0x80 blikajuce (vyznam ostatnych bitov urcuje tabula). */
const INVERSE = 0x40;
const BLINK = 0x80;

/** Vyrobcovia LCD Elektrocas - len ich tabule poznaju cervene pole a velke pismena druhu vlaku. */
const LCD_MANUFACTURERS = ["LCD", "LCD1", "ERP"];

/** Cervene pole pri vlakoch vyssej kategorie (pravidlo PRIZNAK v TabTab) - cela bunka farbou 1 (cervena). */
const RED_FIELD = 0x7f;
const RED = 1;

/** Velke pismena druhu vlaku ("velke fonty" LCD) - kody mimo CP1250; kreslia sa cez celu vysku bunky a tucne. */
const BIG_LETTERS: Record<number, string> = { 0x81: "R", 0x83: "I", 0x88: "E", 0x90: "C" };

/** Pocet casti striedania riadku: vrstva 0 a vsetky dalsie vrstvy. */
function partCount(row: CharRow): number {
  return row.layers.reduce((max, l) => Math.max(max, l.layer + 1), 1);
}

/** Ci sa obsah znakovej tabule meni v case (vrstvy striedania, blikajuce pismo). */
export function isCharsAnimated(board: Board): boolean {
  return (board.display.charRows ?? []).some((r) => partCount(r) > 1 || r.layers.some((l) => l.fonts.some((f, i) => l.codes[i] >= 0 && (f & BLINK) !== 0)));
}

/**
 * Body znakovej tabule v case timeMs. Striedanie: v casti k sa ukaze vrstva k tam, kde ma znaky, inde vrstva 0.
 */
export function rasterizeChars(board: Board, source: GlyphSource, timeMs: number): DotFrame {
  const p = board.profile;
  const width = Math.max(1, p.width);
  const height = Math.max(1, p.rows * p.rowHeight);
  const frame: DotFrame = { width, height, color: new Uint8Array(width * height), blink: new Uint8Array(width * height) };
  const d = board.display;
  if (d.brightness <= 0) return frame;

  const step = Math.floor(timeMs / Math.max(100, p.alternationMs));
  const lcd = LCD_MANUFACTURERS.includes(board.manufacturer.toUpperCase());
  for (const row of d.charRows ?? []) {
    if (row.row < 0 || row.row >= p.rows) continue;
    const base = row.layers.find((l) => l.layer === 0);
    const shown = row.layers.find((l) => l.layer === step % partCount(row));
    const columns = Math.max(base?.codes.length ?? 0, shown?.codes.length ?? 0);
    for (let col = 0; col < columns; col++) {
      const layer = shown && (shown.codes[col] ?? -1) >= 0 ? shown : base;
      if (!layer || (layer.codes[col] ?? -1) < 0) continue;
      const code = layer.codes[col];
      const font = layer.fonts[col] ?? 0;
      const x = col * CHAR_WIDTH;
      const y = row.row * p.rowHeight;
      if (lcd && code === RED_FIELD) fillCell(frame, font, x, y, p.rowHeight);
      else if (lcd && BIG_LETTERS[code]) drawCell(frame, bigLetter(source, BIG_LETTERS[code], p.rowHeight), font, x, y, p.rowHeight);
      // pismo prehliadaca s pevnou sirkou (rez 0), pri subore pisiem ELEN jeho prve pismo
      else drawCell(frame, source.glyph(code, layer.text[col] ?? " ", 0), font, x, y, p.rowHeight);
    }
  }
  return frame;
}

/** Cervene pole: cela bunka svieti cervenou (aj pri inverznom pisme), blikajuce pismo blika. */
function fillCell(frame: DotFrame, font: number, x0: number, y0: number, rowHeight: number) {
  const blink = (font & BLINK) !== 0 ? 1 : 0;
  for (let y = y0; y < Math.min(frame.height, y0 + rowHeight); y++)
    for (let x = x0; x < Math.min(frame.width, x0 + CHAR_WIDTH); x++) {
      frame.color[y * frame.width + x] = RED;
      frame.blink[y * frame.width + x] = blink;
    }
}

/**
 * Velke pismeno: kresba pismena zo zdroja natiahnuta na vysku bunky bez okrajoveho bodu hore a dole, ztucnena
 * o bod doprava - tvar skutocneho pisma tabule nepozname, ide o priblizenie.
 */
function bigLetter(source: GlyphSource, letter: string, rowHeight: number): Glyph | null {
  const glyph = source.glyph(letter.charCodeAt(0), letter, 0);
  if (!glyph) return null;
  let top = -1;
  let bottom = -1;
  for (let y = 0; y < glyph.height; y++)
    for (let x = 0; x < glyph.width; x++)
      if (glyph.isSet(x, y)) {
        if (top < 0) top = y;
        bottom = y;
        break;
      }
  if (top < 0) return glyph;
  const margin = rowHeight >= 5 ? 1 : 0;
  const rows = rowHeight - 2 * margin;
  const ink = bottom - top + 1;
  const at = (x: number, y: number) => x >= 0 && x < glyph.width && glyph.isSet(x, y);
  return {
    width: CHAR_WIDTH,
    height: rowHeight,
    isSet: (x, y) => {
      if (y < margin || y >= margin + rows || x >= CHAR_WIDTH - 1) return false;
      const gy = top + Math.floor(((y - margin) * ink) / rows);
      return at(x, gy) || at(x - 1, gy);
    },
  };
}

function drawCell(frame: DotFrame, glyph: Glyph | null, font: number, x0: number, y0: number, rowHeight: number) {
  const inverse = (font & INVERSE) !== 0;
  const blink = (font & BLINK) !== 0 ? 1 : 0;
  for (let gy = 0; gy < rowHeight; gy++) {
    const y = y0 + gy;
    if (y >= frame.height) break;
    for (let gx = 0; gx < CHAR_WIDTH; gx++) {
      const x = x0 + gx;
      if (x >= frame.width) break;
      const set = !!glyph && gy < glyph.height && gx < glyph.width && glyph.isSet(gx, gy);
      if (set === inverse) continue;
      const i = y * frame.width + x;
      frame.color[i] = COLOR_NONE;
      frame.blink[i] = blink;
    }
  }
}
