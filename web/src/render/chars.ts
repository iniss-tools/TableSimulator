// Rasterizacia znakovej tabule Elektrocas (LCD, LCD1, ERP) na body: znak = bunka 8 bodov × vyska riadku, riadky
// zhora od 0. Co tabula v danom okamihu ukazuje (vrstvy striedania, blikanie), urcuje tento subor; ako body
// vyzeraju, rozhoduje renderer podla vzhladu - rovnako ako pri ELEN.
import type { BoardDto as Board, CharRowDto as CharRow } from "../api/generated/types.gen";
import type { GlyphSource } from "./fonts";
import { COLOR_NONE, type DotFrame } from "./frame";

/** Sirka znaku v bodoch - INISS pocita stlpec ako START / 8. */
export const CHAR_WIDTH = 8;

/** Pismo LCD1: 0x40 inverzne, 0x80 blikajuce (vyznam ostatnych bitov urcuje tabula). */
const INVERSE = 0x40;
const BLINK = 0x80;

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
  for (const row of d.charRows ?? []) {
    if (row.row < 0 || row.row >= p.rows) continue;
    const base = row.layers.find((l) => l.layer === 0);
    const shown = row.layers.find((l) => l.layer === step % partCount(row));
    const columns = Math.max(base?.codes.length ?? 0, shown?.codes.length ?? 0);
    for (let col = 0; col < columns; col++) {
      const layer = shown && (shown.codes[col] ?? -1) >= 0 ? shown : base;
      if (!layer || (layer.codes[col] ?? -1) < 0) continue;
      drawCell(frame, source, layer.codes[col], layer.text[col] ?? " ", layer.fonts[col] ?? 0, col * CHAR_WIDTH, row.row * p.rowHeight, p.rowHeight);
    }
  }
  return frame;
}

function drawCell(frame: DotFrame, source: GlyphSource, code: number, ch: string, font: number, x0: number, y0: number, rowHeight: number) {
  const inverse = (font & INVERSE) !== 0;
  const blink = (font & BLINK) !== 0 ? 1 : 0;
  // pismo prehliadaca s pevnou sirkou (rez 0), pri subore pisiem ELEN jeho prve pismo
  const glyph = source.glyph(code, ch, 0);
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
