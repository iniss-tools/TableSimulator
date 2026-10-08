// Nahradne pismo tabule bez pisma ELEN (.bin): bitmapove pisma misc-fixed navrhnute bod po bode - ostre znaky
// namiesto roztraseneho pisma prehliadaca. Velkost podla vysky riadku (bunky), rez podla cisla pisma ELEN.
import { CanvasGlyphSource, CellGlyphSource, type Glyph, type GlyphSource } from "./fonts";
import { PIXEL_FONTS, type PixelFontData } from "./pixel-fonts.data";

/** Najvacsie pismo, ktore sa zmesti do vysky (a sirky); null = riadok je na bitmapove pisma prilis nizky. */
export function pixelFontFor(height: number, maxWidth = Number.POSITIVE_INFINITY): PixelFontData | null {
  let best: PixelFontData | null = null;
  for (const font of PIXEL_FONTS) {
    if (font.height > height || font.width > maxWidth) continue;
    if (!best || font.height > best.height || (font.height === best.height && font.width > best.width)) best = font;
  }
  return best;
}

/** Bity znaku v bunke pisma (riadok po riadku); null = znak v pisme nie je. */
function decode(font: PixelFontData, ch: string): Uint8Array | null {
  const code = ch.codePointAt(0) ?? 0x20;
  let hex = font.glyphs[code];
  // znak mimo CP1250 - aspon pismeno bez diakritiky, inak otaznik
  if (hex === undefined) {
    const base = ch.normalize("NFD").codePointAt(0) ?? 0x3f;
    hex = font.glyphs[base] ?? font.glyphs[0x3f];
  }
  if (hex === undefined) return null;
  const bytes = Math.ceil(font.width / 8);
  const bits = new Uint8Array(font.width * font.height);
  for (let y = 0; y < font.height; y++) {
    const row = Number.parseInt(hex.slice(y * bytes * 2, (y + 1) * bytes * 2), 16);
    for (let x = 0; x < font.width; x++) if ((row >> (bytes * 8 - 1 - x)) & 1) bits[y * font.width + x] = 1;
  }
  return bits;
}

/** Stlpce s kresbou znaku [od, do]; null = prazdny znak (medzera). */
function inkColumns(bits: Uint8Array, font: PixelFontData): [number, number] | null {
  let from = font.width;
  let to = -1;
  for (let y = 0; y < font.height; y++)
    for (let x = 0; x < font.width; x++)
      if (bits[y * font.width + x]) {
        from = Math.min(from, x);
        to = Math.max(to, x);
      }
  return to < 0 ? null : [from, to];
}

/**
 * Znaky tabule ELEN z bitmapoveho pisma: rez 0 (NEPROP) a 3 (SPEC) s pevnou sirkou ako pisma ELEN (6 bodov,
 * od 16 riadkov 8), rez 1 (PROP) proporcionalne - kresba znaku a bod medzery, rez 2 (PROP_BOLD) ako PROP
 * s ztucnenim o bod doprava. Polohy textu urcuje INISS kurzorom, tvary su presne na mriezke bodov.
 */
export class PixelGlyphSource implements GlyphSource {
  private readonly cache = new Map<string, Glyph | null>();
  private readonly top: number;

  constructor(
    private readonly height: number,
    private readonly font: PixelFontData,
  ) {
    // pismo v strede riadku
    this.top = Math.floor((height - font.height) / 2);
  }

  /** Zdroj pre vysku riadku; null = riadok je prilis nizky (zostava pismo prehliadaca). */
  static forRow(height: number): PixelGlyphSource | null {
    const font = pixelFontFor(height);
    return font ? new PixelGlyphSource(height, font) : null;
  }

  glyph(_code: number, ch: string, fontNumber: number): Glyph | null {
    const face = (fontNumber >> 4) & 0x03;
    const key = `${ch}|${face}`;
    if (this.cache.has(key)) return this.cache.get(key)!;
    const glyph = this.build(ch, face);
    this.cache.set(key, glyph);
    return glyph;
  }

  private build(ch: string, face: number): Glyph | null {
    const { font, height, top } = this;
    const bits = decode(font, ch);
    if (!bits) return null;
    const ink = inkColumns(bits, font);
    const fixed = face === 0 || (face === 3 && ch !== ":");
    const bold = face === 2;
    // pevna sirka ako pisma ELEN; proporcionalne kresba + bod medzery (medzera polovica bunky)
    const from = fixed || !ink ? 0 : ink[0];
    const width = fixed ? (height >= 20 ? font.width : height >= 16 ? 8 : 6) : ink ? ink[1] - ink[0] + 2 + (bold ? 1 : 0) : Math.ceil(font.width / 2);
    const at = (x: number, y: number) => x >= 0 && x < font.width && y >= 0 && y < font.height && bits[y * font.width + x] === 1;
    return {
      width,
      height,
      isSet: (x, y) => {
        if (x < 0 || x >= width || y < top || y >= top + font.height) return false;
        const fx = x + from;
        const fy = y - top;
        return at(fx, fy) || (bold && at(fx - 1, fy));
      },
    };
  }
}

/** Znaky znakovej tabule (LCD Elektrocas): bitmapove pismo do bunky sirka x vyska s bodom medzery vpravo. */
export class PixelCellGlyphSource implements GlyphSource {
  private readonly cache = new Map<string, Glyph | null>();

  private constructor(
    private readonly width: number,
    private readonly height: number,
    private readonly font: PixelFontData,
  ) {}

  /** Zdroj pre bunku; null = bunka je na bitmapove pisma prilis mala. */
  static forCell(width: number, height: number): PixelCellGlyphSource | null {
    const font = pixelFontFor(height, width - 1);
    return font ? new PixelCellGlyphSource(width, height, font) : null;
  }

  glyph(_code: number, ch: string): Glyph | null {
    if (this.cache.has(ch)) return this.cache.get(ch)!;
    const { font, width, height } = this;
    const bits = decode(font, ch);
    const top = Math.floor((height - font.height) / 2);
    const glyph: Glyph | null = bits && {
      width,
      height,
      isSet: (x, y) => {
        const fy = y - top;
        return x >= 0 && x < font.width && x < width - 1 && fy >= 0 && fy < font.height && bits[fy * font.width + x] === 1;
      },
    };
    this.cache.set(ch, glyph);
    return glyph;
  }
}

/**
 * Znaky tabule bez pisma ELEN: bitmapove pismo podla vysky riadku (znakova tabula - bunky sirky cellWidth);
 * na prilis nizke riadky pismo prehliadaca.
 */
export function fallbackGlyphSource(family: string, rowHeight: number, cellWidth: number): GlyphSource {
  if (family === "chars") return PixelCellGlyphSource.forCell(cellWidth, rowHeight) ?? new CellGlyphSource(cellWidth, rowHeight);
  return PixelGlyphSource.forRow(rowHeight) ?? new CanvasGlyphSource(rowHeight);
}
