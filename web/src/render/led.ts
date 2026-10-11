// Vykreslenie bodov tabule ako LED matice podla vzhladu, v okne cela tabule. Prekresluju sa len body, ktore sa
// zmenili; celo (plochy a texty) sa kresli pri celom prekresleni, jeho hodiny pri kazdom.
import type { BoardFaceDto } from "../api/generated/types.gen";
import { faceStyle, type BoardDesign } from "./design";
import { drawFace, drawFaceClocks, hasFaceClock, rowOffset, rowSpacing, type Box, type DisplayClock, type DotText, type FaceGeometry } from "./face";
import type { DotFrame } from "./rasterize";
import { drawFrame } from "./surface";

/** Co okrem bodov ovplyvnuje kreslenie. */
export interface LedOptions {
  rowHeight: number;
  /** Jas cervenej a zelenej 0-100. */
  brightness: number;
  brightnessGreen: number;
  /** Faza blikania - blikajuce body svietia. */
  blinkOn: boolean;
  /** Jednofarebny panel - kazdy rozsvieteny bod ma farbu mono. */
  monochrome: boolean;
  /** Panel so zamenenou cervenou a zelenou. */
  swapRedGreen: boolean;
  /** Celo tabule; null = bez cela. */
  face: BoardFaceDto | null;
  /** Hodnoty premennych cela. */
  values: Record<string, string>;
  /** Cas hodin cela. */
  time: Date;
}

/** Rozlozenie tabule v CSS px pri mierke 1: celkovy rozmer a geometria cela (od vnutorneho okraja ramu). */
export function ledLayout(frame: { width: number; height: number }, design: BoardDesign, rowHeight: number, face: BoardFaceDto | null) {
  const rows = Math.max(1, Math.ceil(frame.height / Math.max(1, rowHeight)));
  const p = design.pitch;
  const pad = design.padding;
  const spacing = rowSpacing(face, design.rowGap);
  const offset = (r: number) => rowOffset(r, spacing.rowGap, spacing.group, spacing.groupGap);
  const m = face?.margin ?? { left: 0, top: 0, right: 0, bottom: 0 };
  const matrixHeight = frame.height + offset(rows - 1);
  const inner = { x: 0, y: 0, w: (2 * pad + m.left + frame.width + m.right) * p, h: (2 * pad + m.top + matrixHeight + m.bottom) * p };
  const matrix = { x: (pad + m.left) * p, y: (pad + m.top) * p, w: frame.width * p, h: matrixHeight * p };
  const geometry: FaceGeometry = {
    unit: p,
    face: inner,
    matrix,
    rows: Array.from({ length: rows }, (_, r) => ({ x: matrix.x, y: matrix.y + (r * rowHeight + offset(r)) * p, w: matrix.w, h: rowHeight * p })),
  };
  return { width: 2 * design.frame.width + inner.w, height: 2 * design.frame.width + inner.h, geometry, offset, spacing };
}

/** Rozmer tabule v CSS px pri mierke 1. */
export function ledSize(frame: { width: number; height: number }, design: BoardDesign, rowHeight: number, face: BoardFaceDto | null = null) {
  const { width, height } = ledLayout(frame, design, rowHeight, face);
  return { width, height };
}

/** Index spritu: 1 cervena, 2 zelena, 3 zlta, 4 bez farby, 5 jednofarebny. */
const MONO = 5;

function sprite(size: number, design: BoardDesign, color: string, glow: number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = Math.max(1, size);
  const ctx = canvas.getContext("2d")!;
  const c = size / 2;
  const r = (design.dot * size) / 2;
  if (glow > 0) {
    const g = ctx.createRadialGradient(c, c, r * 0.6, c, c, size / 2);
    g.addColorStop(0, color);
    g.addColorStop(1, "transparent");
    ctx.globalAlpha = Math.min(1, glow) * 0.45;
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    ctx.globalAlpha = 1;
  }
  ctx.fillStyle = color;
  if (design.shape === "square") {
    ctx.fillRect(c - r, c - r, 2 * r, 2 * r);
  } else if (design.shape === "rounded") {
    ctx.beginPath();
    ctx.roundRect(c - r, c - r, 2 * r, 2 * r, r * 0.4);
    ctx.fill();
  } else {
    ctx.beginPath();
    ctx.arc(c, c, r, 0, Math.PI * 2);
    ctx.fill();
  }
  return canvas;
}

/** Jas 0-100 na nepriehladnost rozsvieteneho bodu; 0 = zhasnuty. */
function level(brightness: number): number {
  return brightness <= 0 ? 0 : 0.35 + (0.65 * Math.min(100, brightness)) / 100;
}

/**
 * Zhasnute body celej matice: jeden riadok bodov sa nakresli do pomocneho platna (pre kazdu vysku riadku zvlast)
 * a skopiruje na kazdy riadok - namiesto sirka × vyska volani drawImage len sirka + vyska.
 */
function drawOffRows(ctx: CanvasRenderingContext2D, off: HTMLCanvasElement, xs: Int32Array, ys: Int32Array, hs: Int32Array) {
  const left = xs[0];
  const width = xs[xs.length - 1] - left;
  if (width <= 0) return;
  const strips = new Map<number, HTMLCanvasElement>();
  for (let y = 0; y < hs.length; y++) {
    const h = hs[y];
    let strip = strips.get(h);
    if (!strip) {
      strip = document.createElement("canvas");
      strip.width = width;
      strip.height = h;
      const sctx = strip.getContext("2d")!;
      for (let x = 0; x + 1 < xs.length; x++) sctx.drawImage(off, xs[x] - left, 0, Math.max(1, xs[x + 1] - xs[x]), h);
      strips.set(h, strip);
    }
    ctx.drawImage(strip, left, ys[y]);
  }
}

/**
 * Moduly pod bodmi: riadky (alebo skupiny riadkov z cela) s okrajom - zvislo od-do v px platna. Vidno ich, ked ma
 * plech inu farbu nez pozadie, a svy medzi modulmi idu cez ne.
 */
function moduleSpans(ys: Int32Array, hs: Int32Array, rowHeight: number, group: number, margin: number): [number, number][] {
  const rows = Math.ceil(hs.length / Math.max(1, rowHeight));
  const size = group > 0 ? group : 1;
  const spans: [number, number][] = [];
  for (let first = 0; first < rows; first += size) {
    const top = ys[first * rowHeight];
    const last = Math.min(hs.length, (first + size) * rowHeight) - 1;
    spans.push([Math.round(top - margin), Math.round(ys[last] + hs[last] + margin)]);
  }
  return spans;
}

export class LedRenderer {
  private key = "";
  private prev: Uint8Array | null = null;
  private sprites: { off: HTMLCanvasElement | null; lit: Record<number, HTMLCanvasElement> } | null = null;
  /** Sprity rozsvietenych bodov hodin podla farby (velkost bunky ako matica). */
  private clockSprites = new Map<string, HTMLCanvasElement>();

  constructor(private readonly canvas: HTMLCanvasElement) {}

  /**
   * Vykresli tabulu v mierke scale (CSS px × devicePixelRatio).
   */
  render(frame: DotFrame, design: BoardDesign, options: LedOptions, scale: number) {
    const { rowHeight, face } = options;
    const layout = ledLayout(frame, design, rowHeight, face);
    const g = layout.geometry;
    const cw = Math.max(1, Math.round(layout.width * scale));
    const ch = Math.max(1, Math.round(layout.height * scale));
    const levels = [0, level(options.brightness), level(options.brightnessGreen), level(Math.max(options.brightness, options.brightnessGreen))];
    const key = `${cw}x${ch}|${frame.width}x${frame.height}|${levels.join()}|${options.monochrome}|${options.swapRedGreen}|${JSON.stringify(face)}|${JSON.stringify(options.values)}|${JSON.stringify(design)}`;
    const ctx = this.canvas.getContext("2d")!;
    const p = design.pitch * scale;
    const fw = design.frame.width * scale;
    const originX = fw + g.matrix.x * scale;
    const originY = fw + g.matrix.y * scale;
    const row = (y: number) => Math.floor(y / Math.max(1, rowHeight));
    const full = key !== this.key || !this.prev || this.prev.length !== frame.color.length;
    const style = faceStyle(design);
    const plate = design.plate ?? design.background;

    // bunky su prichytene na cele pixely a neprekryvaju sa - prekreslenie jednej nezasiahne susednu
    const xs = new Int32Array(frame.width + 1);
    for (let x = 0; x <= frame.width; x++) xs[x] = Math.round(originX + x * p);
    const ys = new Int32Array(frame.height + 1);
    for (let y = 0; y <= frame.height; y++) ys[y] = Math.round(originY + y * p + layout.offset(row(Math.min(y, frame.height - 1))) * p);
    const hs = new Int32Array(frame.height);
    for (let y = 0; y < frame.height; y++) hs[y] = Math.max(1, Math.round(originY + (y + 1) * p + layout.offset(row(y)) * p) - ys[y]);

    const spans = moduleSpans(ys, hs, rowHeight, layout.spacing.group, design.moduleMargin * p);
    const moduleLeft = Math.round(originX - design.padding * p);
    const moduleWidth = Math.round((frame.width + 2 * design.padding) * p);

    if (full) {
      this.key = key;
      this.canvas.width = cw;
      this.canvas.height = ch;
      this.clockSprites.clear();
      const cell = Math.max(1, Math.round(p));
      const color = (c: string) => design.colors[c] ?? "#fff";
      this.sprites = {
        off: design.off ? sprite(cell, design, design.off, 0) : null,
        lit: {
          1: sprite(cell, design, color("1"), design.glow),
          2: sprite(cell, design, color("2"), design.glow),
          3: sprite(cell, design, color("3"), design.glow),
          4: sprite(cell, design, color("0"), design.glow),
          [MONO]: sprite(cell, design, design.colors.mono ?? color("0"), design.glow),
        },
      };
      // ram, plech, moduly pod bodmi a vypln medzi riadkami
      drawFrame(ctx, cw, ch, design.frame, scale);
      ctx.fillStyle = plate;
      ctx.fillRect(fw, fw, cw - 2 * fw, ch - 2 * fw);
      if (plate !== design.background) {
        ctx.fillStyle = design.background;
        for (const [top, bottom] of spans) ctx.fillRect(moduleLeft, top, moduleWidth, bottom - top);
      }
      // vypln medzier medzi riadkami - v sirke matice s okrajom, celo vedla nej ju prekryje
      if (design.gapColor) {
        ctx.fillStyle = design.gapColor;
        const left = Math.round(originX - design.padding * p);
        const width = Math.round((frame.width + 2 * design.padding) * p);
        for (let y = rowHeight; y < frame.height; y += rowHeight) {
          const top = Math.round(originY + y * p + layout.offset(row(y) - 1) * p);
          const bottom = Math.round(originY + y * p + layout.offset(row(y)) * p);
          if (bottom > top) ctx.fillRect(left, top, width, bottom - top);
        }
      }
      if (this.sprites.off) drawOffRows(ctx, this.sprites.off, xs, ys, hs);
      if (face) this.inFace(ctx, fw, scale, () => drawFace(ctx, face, g, style, options.values));
      this.prev = new Uint8Array(frame.color.length);
    }

    if (face && hasFaceClock(face)) {
      const alpha = levels[3];
      const display: DisplayClock = (box, dots, color) => this.drawDots(ctx, design, box, dots, color ?? design.colors["3"] ?? "#fff", g.unit, p, alpha);
      this.inFace(ctx, fw, scale, () => drawFaceClocks(ctx, face, g, style, options.time, display));
    }

    const sprites = this.sprites!;
    const prev = this.prev!;
    for (let y = 0; y < frame.height; y++) {
      const py = ys[y];
      const h = hs[y];
      for (let x = 0; x < frame.width; x++) {
        const i = y * frame.width + x;
        let v = frame.color[i];
        if (v !== 0 && frame.blink[i] && !options.blinkOn) v = 0;
        if (v !== 0 && options.swapRedGreen && v <= 2) v = 3 - v;
        const alpha = v === 0 ? 0 : levels[v === 4 ? 3 : v];
        if (alpha === 0) v = 0;
        else if (options.monochrome) v = MONO;
        // po celom prekresleni su vsetky body zhasnute (prev = 0) - kresli sa len rozsvietene
        if (v === prev[i]) continue;
        const wasLit = prev[i] !== 0;
        prev[i] = v;
        const px = xs[x];
        const w = Math.max(1, xs[x + 1] - px);
        if (wasLit) {
          ctx.fillStyle = design.background;
          ctx.fillRect(px, py, w, h);
          if (sprites.off) ctx.drawImage(sprites.off, px, py, w, h);
        }
        if (v !== 0) {
          ctx.globalAlpha = alpha;
          ctx.drawImage(sprites.lit[v], px, py, w, h);
          ctx.globalAlpha = 1;
        }
      }
    }

    // svy medzi modulmi cez moduly - po bodoch, aby ich prekreslene body neprekryli
    const every = face?.moduleWidth ?? 0;
    if (design.seam.color && every > 0) {
      ctx.fillStyle = design.seam.color;
      const width = Math.max(1, design.seam.width * p);
      for (let x = every; x < frame.width; x += every)
        for (const [top, bottom] of spans) ctx.fillRect(Math.round(xs[x] - width / 2), top, Math.round(width), bottom - top);
    }
  }

  /** Hodiny bodmi v px pri mierke 1: zhasnute body a rozsvietene farbou color. */
  private drawDots(ctx: CanvasRenderingContext2D, design: BoardDesign, box: Box, dots: DotText, color: string, unit: number, cellPx: number, alpha: number) {
    let lit = this.clockSprites.get(color);
    if (!lit) {
      lit = sprite(Math.max(1, Math.round(cellPx)), design, color, design.glow);
      this.clockSprites.set(color, lit);
    }
    const off = this.sprites?.off ?? null;
    for (let y = 0; y < dots.height; y++)
      for (let x = 0; x < dots.width; x++) {
        const px = box.x + x * unit;
        const py = box.y + y * unit;
        if (off) ctx.drawImage(off, px, py, unit, unit);
        if (alpha > 0 && dots.lit[y * dots.width + x]) {
          ctx.globalAlpha = alpha;
          ctx.drawImage(lit, px, py, unit, unit);
          ctx.globalAlpha = 1;
        }
      }
  }

  /** Kreslenie cela v px pri mierke 1 od vnutorneho okraja ramu. */
  private inFace(ctx: CanvasRenderingContext2D, frameWidth: number, scale: number, draw: () => void) {
    ctx.save();
    ctx.translate(frameWidth, frameWidth);
    ctx.scale(scale, scale);
    draw();
    ctx.restore();
  }
}
