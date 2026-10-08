// Pisma ELEN zo servera (/api/fonts/<subor>) a zdroje znakov pre rasterizaciu.
import { authHeaders } from "../api/auth";

export interface ElenFontData {
  name: string;
  maxWidth: number;
  height: number;
  proportional: boolean;
  bytesPerRow: number;
  widths: number[];
  glyphs: Uint8Array[];
}

export interface ElenFontSet {
  file: string;
  name: string;
  fonts: ElenFontData[];
}

/** Znak pripraveny na kreslenie. */
export interface Glyph {
  width: number;
  height: number;
  isSet(x: number, y: number): boolean;
}

/** Odkial rasterizacia berie znaky: presne pismo ELEN alebo priblizne pismo prehliadaca. */
export interface GlyphSource {
  /** Znak pre bajt code (podla neho pismo ELEN) a znak ch (podla neho pismo prehliadaca) v pisme font. */
  glyph(code: number, ch: string, font: number): Glyph | null;
}

interface FontFileDto {
  file: string;
  name: string;
  fonts: {
    name: string;
    maxWidth: number;
    height: number;
    proportional: boolean;
    bytesPerRow: number;
    widths: number[];
    glyphs: string[];
  }[];
}

function fromBase64(text: string): Uint8Array {
  const raw = atob(text);
  const bytes = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
  return bytes;
}

export function decodeFontSet(dto: FontFileDto): ElenFontSet {
  return {
    file: dto.file,
    name: dto.name,
    fonts: dto.fonts.map((f) => ({ ...f, glyphs: f.glyphs.map(fromBase64) })),
  };
}

const cache = new Map<string, Promise<ElenFontSet | null>>();

export function loadFontSet(file: string): Promise<ElenFontSet | null> {
  let promise = cache.get(file);
  if (!promise) {
    promise = fetch(`/api/fonts/${encodeURIComponent(file)}`, { headers: authHeaders() })
      .then((r) => (r.ok ? r.json() : null))
      .then((dto: FontFileDto | null) => (dto ? decodeFontSet(dto) : null))
      .catch(() => null);
    cache.set(file, promise);
  }
  return promise;
}

/** Zabudne nacitane pisma (zmena priecinka pisiem). */
export function clearFontCache() {
  cache.clear();
}

/**
 * Ktore pismo suboru zodpoveda cislu pisma ELEN: rozsirene pismo n (bit 0x8000, bity 8-11) → pismo n,
 * inak rez (bity 4-5) → pismo s cislom rezu. FontMap profilu to moze prepisat (face0…face3, ext1…).
 */
export function fontIndex(font: number, fontMap: Record<string, number> | null | undefined): number {
  const ext = font & 0x8000 ? (font >> 8) & 0x0f : 0;
  if (ext > 0) return fontMap?.[`ext${ext}`] ?? ext;
  const face = (font >> 4) & 0x03;
  return fontMap?.[`face${face}`] ?? face;
}

/** Vysoke cislice (bit 0x08): medzera 0x14, cislice 0x15-0x1E, dvojbodka 0x1F. */
export function tallDigitCode(code: number): number {
  if (code >= 0x30 && code <= 0x39) return 0x15 + (code - 0x30);
  if (code === 0x20) return 0x14;
  if (code === 0x3a) return 0x1f;
  return code;
}

/** Presne znaky zo suboru pisiem ELEN. */
export class ElenGlyphSource implements GlyphSource {
  constructor(
    private readonly set: ElenFontSet,
    private readonly fontMap: Record<string, number> | null | undefined,
  ) {}

  glyph(code: number, _ch: string, font: number): Glyph | null {
    const fonts = this.set.fonts;
    if (fonts.length === 0) return null;
    let index = fontIndex(font, this.fontMap);
    if (index < 0 || index >= fonts.length) index = 0;
    const f = fonts[index];
    const c = font & 0x08 ? tallDigitCode(code) : code;
    const data = f.glyphs[c];
    const width = f.widths[c];
    if (!data) return null;
    const bpr = f.bytesPerRow;
    return {
      width,
      height: f.height,
      isSet: (x, y) => x >= 0 && y >= 0 && y < f.height && x < bpr * 8 && (data[y * bpr + (x >> 3)] & (0x80 >> (x & 7))) !== 0,
    };
  }
}

/**
 * Priblizne znaky z pisma prehliadaca - ked tabula nema nastavene pismo ELEN. Polohy textu sedia (urcuje ich
 * INISS kurzorom), tvary znakov su len priblizne.
 */
export class CanvasGlyphSource implements GlyphSource {
  private readonly cache = new Map<string, Glyph>();
  private readonly ctx: CanvasRenderingContext2D | null;

  constructor(private readonly height: number) {
    const canvas = document.createElement("canvas");
    canvas.width = height * 3;
    canvas.height = height + 4;
    this.ctx = canvas.getContext("2d", { willReadFrequently: true });
  }

  glyph(_code: number, ch: string, font: number): Glyph | null {
    const face = (font >> 4) & 0x03;
    const key = `${ch}|${face}`;
    const cached = this.cache.get(key);
    if (cached) return cached;
    const ctx = this.ctx;
    if (!ctx) return null;

    const h = this.height;
    const fixed = face === 0 || face === 3;
    ctx.font = fixed ? `${h}px Consolas, "DejaVu Sans Mono", monospace` : `${face === 2 ? "bold " : ""}${h}px "Arial Narrow", Arial, sans-serif`;
    ctx.textBaseline = "alphabetic";
    const advance = fixed ? (face === 3 && ch === ":" ? 2 : h >= 16 ? 8 : 6) : Math.max(2, Math.round(ctx.measureText(ch).width) + 1);

    ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
    ctx.fillStyle = "#fff";
    ctx.fillText(ch, 0, Math.round(h * 0.8));
    const pixels = ctx.getImageData(0, 0, advance, h).data;
    const glyph: Glyph = {
      width: advance,
      height: h,
      isSet: (x, y) => x >= 0 && y >= 0 && x < advance && y < h && pixels[(y * advance + x) * 4 + 3] > 110,
    };
    this.cache.set(key, glyph);
    return glyph;
  }
}

/**
 * Znaky znakovej tabule (LCD) z pisma prehliadaca: kazdy znak presne do bunky sirka × vyska bodov s bodom medzery
 * vpravo. Kresli sa vo stvornasobnom rozliseni a bod svieti, ked ho znak pokryva aspon zo 45 % - uzke znaky ako "m"
 * sa tak nezlievaju.
 */
export class CellGlyphSource implements GlyphSource {
  private static readonly SUPERSAMPLE = 4;
  private readonly cache = new Map<string, Glyph>();
  private readonly ctx: CanvasRenderingContext2D | null;

  constructor(
    private readonly width: number,
    private readonly height: number,
  ) {
    const canvas = document.createElement("canvas");
    canvas.width = width * CellGlyphSource.SUPERSAMPLE;
    canvas.height = height * CellGlyphSource.SUPERSAMPLE;
    this.ctx = canvas.getContext("2d", { willReadFrequently: true });
  }

  glyph(_code: number, ch: string): Glyph | null {
    const cached = this.cache.get(ch);
    if (cached) return cached;
    const ctx = this.ctx;
    if (!ctx) return null;

    const ss = CellGlyphSource.SUPERSAMPLE;
    const { width, height } = this;
    const w = width * ss;
    const h = height * ss;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, w, h);
    ctx.font = `${Math.round(h * 0.85)}px Consolas, "DejaVu Sans Mono", "Courier New", monospace`;
    ctx.textBaseline = "alphabetic";
    ctx.fillStyle = "#fff";
    // sirka znaku na bunku bez posledneho stlpca (medzera medzi znakmi)
    const advance = Math.max(1, ctx.measureText("M").width);
    ctx.setTransform((Math.max(1, width - 1) * ss) / advance, 0, 0, 1, 0, 0);
    ctx.fillText(ch, 0, Math.round(h * 0.78));
    const pixels = ctx.getImageData(0, 0, w, h).data;

    const bits = new Uint8Array(width * height);
    const threshold = 0.45 * ss * ss * 255;
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        let sum = 0;
        for (let sy = 0; sy < ss; sy++) {
          const row = ((y * ss + sy) * w + x * ss) * 4 + 3;
          for (let sx = 0; sx < ss; sx++) sum += pixels[row + sx * 4];
        }
        bits[y * width + x] = sum >= threshold ? 1 : 0;
      }
    }

    const glyph: Glyph = { width, height, isSet: (x, y) => x >= 0 && y >= 0 && x < width && y < height && bits[y * width + x] === 1 };
    this.cache.set(ch, glyph);
    return glyph;
  }
}
