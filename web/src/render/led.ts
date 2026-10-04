// Vykreslenie bodov tabule ako LED matice podla vzhladu. Prekresluju sa len body, ktore sa zmenili.
import type { LedDesign } from "./design";
import type { DotFrame } from "./rasterize";

/** Rozmer tabule v CSS px pri mierke 1. */
export function ledSize(frame: { width: number; height: number }, design: LedDesign, rowHeight: number) {
  const rows = Math.max(1, Math.ceil(frame.height / Math.max(1, rowHeight)));
  const p = design.pitch;
  const inner = design.padding * p;
  const width = 2 * design.frame.width + 2 * inner + frame.width * p;
  const height = 2 * design.frame.width + 2 * inner + frame.height * p + (rows - 1) * design.rowGap * p;
  return { width, height };
}

interface Sprites {
  size: number;
  off: HTMLCanvasElement | null;
  lit: Record<number, HTMLCanvasElement>;
}

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

export class LedRenderer {
  private key = "";
  private prev: Uint8Array | null = null;
  private sprites: Sprites | null = null;

  constructor(private readonly canvas: HTMLCanvasElement) {}

  /**
   * Vykresli body v mierke scale (CSS px × devicePixelRatio).
   * @param brightness jas tabule 0-100
   * @param blinkOn faza blikania - blikajuce body svietia
   */
  render(frame: DotFrame, design: LedDesign, rowHeight: number, brightness: number, blinkOn: boolean, scale: number) {
    const size = ledSize(frame, design, rowHeight);
    const cw = Math.max(1, Math.round(size.width * scale));
    const ch = Math.max(1, Math.round(size.height * scale));
    const level = brightness <= 0 ? 0 : 0.35 + (0.65 * Math.min(100, brightness)) / 100;
    const key = `${cw}x${ch}|${frame.width}x${frame.height}|${level}|${JSON.stringify(design)}`;
    const ctx = this.canvas.getContext("2d")!;
    const p = design.pitch * scale;
    const cell = Math.max(1, Math.round(p));
    const origin = (design.frame.width + design.padding * design.pitch) * scale;
    const gap = design.rowGap * design.pitch * scale;
    const full = key !== this.key || !this.prev || this.prev.length !== frame.color.length;

    if (full) {
      this.key = key;
      this.canvas.width = cw;
      this.canvas.height = ch;
      this.sprites = {
        size: cell,
        off: design.off ? sprite(cell, { ...design }, design.off, 0) : null,
        lit: Object.fromEntries([1, 2, 3, 4].map((c) => [c, sprite(cell, design, design.colors[String(c === 4 ? 0 : c)] ?? "#fff", design.glow)])),
      };
      // ram a pozadie
      const fw = design.frame.width * scale;
      ctx.fillStyle = design.frame.color;
      ctx.beginPath();
      ctx.roundRect(0, 0, cw, ch, design.frame.radius * scale);
      ctx.fill();
      ctx.fillStyle = design.background;
      ctx.fillRect(fw, fw, cw - 2 * fw, ch - 2 * fw);
      this.prev = new Uint8Array(frame.color.length).fill(255);
    }

    // bunky su prichytene na cele pixely a neprekryvaju sa - prekreslenie jednej nezasiahne susednu
    const xs = new Int32Array(frame.width + 1);
    for (let x = 0; x <= frame.width; x++) xs[x] = Math.round(origin + x * p);
    const ys = new Int32Array(frame.height + 1);
    for (let y = 0; y <= frame.height; y++) ys[y] = Math.round(origin + y * p + Math.floor(Math.min(y, frame.height - 1) / Math.max(1, rowHeight)) * gap);

    const sprites = this.sprites!;
    const prev = this.prev!;
    for (let y = 0; y < frame.height; y++) {
      const py = ys[y];
      const h = Math.max(1, Math.round(origin + (y + 1) * p + Math.floor(y / Math.max(1, rowHeight)) * gap) - py);
      for (let x = 0; x < frame.width; x++) {
        const i = y * frame.width + x;
        let v = frame.color[i];
        if (v !== 0 && (level === 0 || (frame.blink[i] && !blinkOn))) v = 0;
        if (v === prev[i]) continue;
        prev[i] = v;
        const px = xs[x];
        const w = Math.max(1, xs[x + 1] - px);
        ctx.fillStyle = design.background;
        ctx.fillRect(px, py, w, h);
        if (sprites.off) ctx.drawImage(sprites.off, px, py, w, h);
        if (v !== 0) {
          ctx.globalAlpha = level;
          ctx.drawImage(sprites.lit[v], px, py, w, h);
          ctx.globalAlpha = 1;
        }
      }
    }
  }
}
