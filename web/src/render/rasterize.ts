// Rasterizacia obsahu tabule ELEN na body: co tabula v danom okamihu ukazuje (striedanie, pohyblivy text,
// blikanie). Ako body vyzeraju, rozhoduje az LED renderer podla vzhladu.
import type { Board, Profile, Run } from "../api";
import type { GlyphSource } from "./fonts";
import { isCharsAnimated, rasterizeChars } from "./chars";
import { COLOR_NONE, type DotFrame } from "./frame";

export { COLOR_NONE, type DotFrame } from "./frame";

/** Ci sa obsah tabule meni v case (striedanie, blikanie, pohyblivy text) - potom treba prekreslovat. */
export function isAnimated(board: Board): boolean {
  const d = board.display;
  if (d.family === "chars") return isCharsAnimated(board);
  if (d.movingRow > 0 && d.movingCodes && d.movingCodes.length > 0) return true;
  return d.rows.some((r) =>
    r.elements.some((e) => e.type === "alt" || (e.run !== null && (e.run.font & 0x04) !== 0) || (e.parts ?? []).some((p) => p.some((run) => (run.font & 0x04) !== 0))),
  );
}

/** Faza blikania v case timeMs: true = blikajuci text svieti. Casy z prikazu 9 maju prednost pred profilom. */
export function blinkPhase(profile: Profile, timeMs: number, display?: { flashOnMs: number | null; flashOffMs: number | null }): boolean {
  const on = display?.flashOnMs ?? profile.blinkOnMs;
  const off = display?.flashOffMs ?? profile.blinkOffMs;
  const period = Math.max(1, on + off);
  return timeMs % period < on;
}

/** Ci tabula ma hodiny v hlavicke - treba prekreslovat aspon raz za sekundu. */
export function hasClock(board: Board): boolean {
  return !!board.profile.header?.clock;
}

/** Cas hodin panelu: od nastavenia INISSom bezi dalej; kym ho INISS nenastavi, cas pocitaca. */
export function boardClock(board: Board, nowMs: number): Date {
  const d = board.display;
  if (d.clock && d.clockSetAt) return new Date(Date.parse(d.clock) + (nowMs - Date.parse(d.clockSetAt)));
  return new Date(nowMs);
}

function drawRun(frame: DotFrame, run: Run, x: number, y0: number, rowHeight: number, source: GlyphSource): number {
  const color = run.font & 0x03 || COLOR_NONE;
  const blink = (run.font & 0x04) !== 0 ? 1 : 0;
  for (let i = 0; i < run.codes.length; i++) {
    const glyph = source.glyph(run.codes[i], run.text[i] ?? "", run.font);
    if (!glyph) continue;
    const h = Math.min(glyph.height, rowHeight);
    for (let gy = 0; gy < h; gy++) {
      const y = y0 + gy;
      if (y < 0 || y >= frame.height) continue;
      for (let gx = 0; gx < glyph.width; gx++) {
        const px = x + gx;
        if (px < 0 || px >= frame.width || !glyph.isSet(gx, gy)) continue;
        const i2 = y * frame.width + px;
        frame.color[i2] = color;
        frame.blink[i2] = blink;
      }
    }
    x += glyph.width;
  }
  return x;
}

function textWidth(run: Run, source: GlyphSource): number {
  let w = 0;
  for (let i = 0; i < run.codes.length; i++) w += source.glyph(run.codes[i], run.text[i] ?? "", run.font)?.width ?? 0;
  return w;
}

/**
 * Body tabule v case timeMs.
 */
export function rasterize(board: Board, source: GlyphSource, timeMs: number): DotFrame {
  if (board.display.family === "chars") return rasterizeChars(board, source, timeMs);
  const p = board.profile;
  const width = Math.max(1, p.width);
  const height = Math.max(1, p.rows * p.rowHeight);
  const frame: DotFrame = { width, height, color: new Uint8Array(width * height), blink: new Uint8Array(width * height) };
  const d = board.display;
  if (d.brightness <= 0) return frame;

  const part = Math.floor(timeMs / Math.max(100, p.alternationMs));
  for (const row of d.rows) {
    const top = p.rows - row.row;
    if (top < 0 || top >= p.rows) continue;
    const y0 = top * p.rowHeight;
    let x = 0;
    for (const element of row.elements) {
      if (element.type === "run" && element.run) {
        x = drawRun(frame, element.run, element.run.x ?? x, y0, p.rowHeight, source);
      } else if (element.parts && element.parts.length > 0) {
        const runs = element.parts[part % element.parts.length];
        const start = x;
        let px = start;
        for (const run of runs) px = drawRun(frame, run, run.x ?? px, y0, p.rowHeight, source);
        x = px;
      }
    }
  }

  // pohyblivy text bezi sprava dolava cez cely riadok
  if (d.movingRow > 0 && d.movingRow <= p.rows && d.movingCodes && d.movingCodes.length > 0) {
    const run: Run = { x: 0, font: p.movingFont, codes: d.movingCodes, text: d.movingText ?? "" };
    const y0 = (p.rows - d.movingRow) * p.rowHeight;
    frame.color.fill(0, y0 * width, (y0 + p.rowHeight) * width);
    frame.blink.fill(0, y0 * width, (y0 + p.rowHeight) * width);
    const span = width + textWidth(run, source);
    const offset = Math.floor((timeMs * Math.max(1, p.scrollSpeed)) / 1000) % Math.max(1, span);
    drawRun(frame, run, width - offset, y0, p.rowHeight, source);
  }

  return frame;
}
