// Vykreslenie bodov tabule ako LED matice podla vzhladu, s hlavickou nad maticou. Prekresluju sa len body, ktore
// sa zmenili; hlavicka (texty a hodiny) sa kresli pri kazdom prekresleni.
import type { BoardHeader } from "../api";
import type { LedDesign } from "./design";
import type { DotFrame } from "./rasterize";

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
  header: BoardHeader | null;
  /** Text hodin v hlavicke (HH:mm). */
  clock: string;
}

/** Rozmer tabule v CSS px pri mierke 1. */
export function ledSize(frame: { width: number; height: number }, design: LedDesign, rowHeight: number, header?: BoardHeader | null) {
  const rows = Math.max(1, Math.ceil(frame.height / Math.max(1, rowHeight)));
  const p = design.pitch;
  const inner = design.padding * p;
  const headerHeight = (header?.height ?? 0) * p;
  const width = 2 * design.frame.width + 2 * inner + frame.width * p;
  const height = 2 * design.frame.width + 2 * inner + headerHeight + frame.height * p + (rows - 1) * design.rowGap * p;
  return { width, height };
}

/** Index spritu: 1 cervena, 2 zelena, 3 zlta, 4 bez farby, 5 jednofarebny. */
const MONO = 5;

function sprite(size: number, design: LedDesign, color: string, glow: number): HTMLCanvasElement {
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

export class LedRenderer {
  private key = "";
  private prev: Uint8Array | null = null;
  private sprites: { off: HTMLCanvasElement | null; lit: Record<number, HTMLCanvasElement> } | null = null;

  constructor(private readonly canvas: HTMLCanvasElement) {}

  /**
   * Vykresli tabulu v mierke scale (CSS px × devicePixelRatio).
   */
  render(frame: DotFrame, design: LedDesign, options: LedOptions, scale: number) {
    const { rowHeight, header } = options;
    const size = ledSize(frame, design, rowHeight, header);
    const cw = Math.max(1, Math.round(size.width * scale));
    const ch = Math.max(1, Math.round(size.height * scale));
    const levels = [0, level(options.brightness), level(options.brightnessGreen), level(Math.max(options.brightness, options.brightnessGreen))];
    const key = `${cw}x${ch}|${frame.width}x${frame.height}|${levels.join()}|${options.monochrome}|${options.swapRedGreen}|${JSON.stringify(header)}|${JSON.stringify(design)}`;
    const ctx = this.canvas.getContext("2d")!;
    const p = design.pitch * scale;
    const fw = design.frame.width * scale;
    const headerHeight = (header?.height ?? 0) * p;
    const originX = fw + design.padding * p;
    const originY = fw + headerHeight + design.padding * p;
    const gap = design.rowGap * p;
    const full = key !== this.key || !this.prev || this.prev.length !== frame.color.length;

    // bunky su prichytene na cele pixely a neprekryvaju sa - prekreslenie jednej nezasiahne susednu
    const xs = new Int32Array(frame.width + 1);
    for (let x = 0; x <= frame.width; x++) xs[x] = Math.round(originX + x * p);
    const ys = new Int32Array(frame.height + 1);
    for (let y = 0; y <= frame.height; y++) ys[y] = Math.round(originY + y * p + Math.floor(Math.min(y, frame.height - 1) / Math.max(1, rowHeight)) * gap);

    if (full) {
      this.key = key;
      this.canvas.width = cw;
      this.canvas.height = ch;
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
      // ram, pozadie a vypln medzi riadkami
      ctx.fillStyle = design.frame.color;
      ctx.beginPath();
      ctx.roundRect(0, 0, cw, ch, design.frame.radius * scale);
      ctx.fill();
      ctx.fillStyle = design.background;
      ctx.fillRect(fw, fw, cw - 2 * fw, ch - 2 * fw);
      if (design.gapColor && gap > 0) {
        ctx.fillStyle = design.gapColor;
        for (let y = rowHeight; y < frame.height; y += rowHeight) {
          const top = Math.round(originY + y * p + (y / rowHeight - 1) * gap);
          ctx.fillRect(fw, top, cw - 2 * fw, Math.round(gap));
        }
      }
      this.prev = new Uint8Array(frame.color.length).fill(255);
    }

    if (header && headerHeight > 0) this.drawHeader(ctx, header, design, options.clock, fw, cw - 2 * fw, Math.round(headerHeight), p);

    const sprites = this.sprites!;
    const prev = this.prev!;
    for (let y = 0; y < frame.height; y++) {
      const py = ys[y];
      const h = Math.max(1, Math.round(originY + (y + 1) * p + Math.floor(y / Math.max(1, rowHeight)) * gap) - py);
      for (let x = 0; x < frame.width; x++) {
        const i = y * frame.width + x;
        let v = frame.color[i];
        if (v !== 0 && frame.blink[i] && !options.blinkOn) v = 0;
        if (v !== 0 && options.swapRedGreen && v <= 2) v = 3 - v;
        const alpha = v === 0 ? 0 : levels[v === 4 ? 3 : v];
        if (alpha === 0) v = 0;
        else if (options.monochrome) v = MONO;
        if (v === prev[i]) continue;
        prev[i] = v;
        const px = xs[x];
        const w = Math.max(1, xs[x + 1] - px);
        ctx.fillStyle = design.background;
        ctx.fillRect(px, py, w, h);
        if (sprites.off) ctx.drawImage(sprites.off, px, py, w, h);
        if (v !== 0) {
          ctx.globalAlpha = alpha;
          ctx.drawImage(sprites.lit[v], px, py, w, h);
          ctx.globalAlpha = 1;
        }
      }
    }
  }

  /** Hlavicka: pozadie, texty a hodiny; polohy v bodoch × p. */
  private drawHeader(ctx: CanvasRenderingContext2D, header: BoardHeader, design: LedDesign, clock: string, left: number, width: number, height: number, p: number) {
    const style = design.header;
    const top = left;
    ctx.save();
    ctx.beginPath();
    ctx.rect(left, top, width, height);
    ctx.clip();
    ctx.fillStyle = style.background;
    ctx.fillRect(left, top, width, height);
    ctx.textBaseline = "top";
    ctx.fillStyle = style.color;
    for (const text of header.texts) {
      ctx.font = `${style.bold ? "bold " : ""}${Math.max(1, Math.round(text.height * p))}px ${style.font}`;
      ctx.fillText(text.text, left + text.x * p, top + text.y * p);
    }
    if (header.clock) {
      const c = header.clock;
      ctx.font = `bold ${Math.max(1, Math.round(c.height * p))}px ${style.clockFont}`;
      const w = ctx.measureText(clock).width;
      const x = left + c.x * p;
      const y = top + c.y * p;
      ctx.fillStyle = style.clockBackground;
      ctx.fillRect(x - p, y - p * 0.5, w + 2 * p, c.height * p + p);
      ctx.fillStyle = style.clockColor;
      ctx.fillText(clock, x, y);
    }
    ctx.restore();
  }
}
