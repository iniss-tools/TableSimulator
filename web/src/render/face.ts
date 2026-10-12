// Celo tabule (predloha BoardFaceDto): plochy, texty a hodiny okolo matice. Rozlozenie je ciste (testy), kreslenie ide
// na platno v px pri mierke 1 (volajuci nastavi posun a mierku). Maticu - LED, LCD aj listy - kresli jej renderer do
// okna cela; geometriu (matica a riadky v px) mu dava on. Polohy a rozmery prvkov su v bodoch, unit = px na bod.
import type { BoardDto, BoardFaceDto, BoardHeaderDto, FaceClockFormat, FaceElementDto, FaceSymbol } from "../api/generated/types.gen";
import type { TextStyle } from "./design";
import { faceImage } from "./images";
import { PixelGlyphSource } from "./pixel-fonts";

export interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** Geometria tabule v px pri mierke 1: plocha vo vnutri ramu, matica a jej riadky (zhora). */
export interface FaceGeometry {
  unit: number;
  face: Box;
  matrix: Box;
  rows: Box[];
}

/** Ako cela vyzeraju: pismo, farby podla nazvov (text, band, panel, plate, accent) a styly textov zo vzhladu tabule. */
export interface FaceStyle {
  font: string;
  bold: boolean;
  colors: Record<string, string>;
  styles: Record<string, TextStyle>;
  clockFont: string;
  clockColor: string;
  clockBackground: string;
}

/** Meranie textu - platno (v testoch jeho napodobenina). */
export interface TextMeasure {
  font: string;
  letterSpacing: string;
  measureText(text: string): { width: number };
}

/** Celo nad tabulou v editore: mierka (CSS px na px pri mierke 1), geometria od laveho horneho rohu platna a plochy prvkov. */
export interface FaceOverlayLayout {
  scale: number;
  geometry: FaceGeometry;
  boxes: (Box | null)[];
}

let measuring: CanvasRenderingContext2D | null = null;

/** Platno na meranie textu mimo kreslenia (editor cela). */
export function measureContext(): CanvasRenderingContext2D {
  measuring ??= document.createElement("canvas").getContext("2d")!;
  return measuring;
}

/** Geometria posunuta o (dx, dy) px - napr. o sirku ramu. */
export function shiftGeometry(g: FaceGeometry, dx: number, dy: number): FaceGeometry {
  const shift = (b: Box): Box => ({ ...b, x: b.x + dx, y: b.y + dy });
  return { unit: g.unit, face: shift(g.face), matrix: shift(g.matrix), rows: g.rows.map(shift) };
}

/** Riadok textu je o tolko vyssi nez pismo. */
const LINE_SPACING = 1.15;

/** Body medzi riadkami tabule nad riadkom r (od 0): medzera riadkov a navyse medzera skupin. */
export function rowOffset(r: number, rowGap: number, group: number, groupGap: number): number {
  return r * rowGap + (group > 0 ? Math.floor(r / group) * groupGap : 0);
}

/** Medzery medzi riadkami tabule: z cela, inak zo vzhladu. */
export function rowSpacing(face: BoardFaceDto | null, designGap: number) {
  return { rowGap: face?.rowGap ?? designGap, group: face?.rowGroup ?? 0, groupGap: face?.groupGap ?? 0 };
}

/** Predvoleny prvok (ako FaceElement na serveri). */
export const NEW_ELEMENT: FaceElementDto = {
  kind: "text",
  anchor: "face",
  row: 1,
  x: 0,
  y: 0,
  width: 0,
  height: 0,
  align: "left",
  vAlign: "top",
  text: "",
  size: 8,
  size2: 0,
  bold: null,
  style: "",
  color: "",
  fill: "",
  border: 0,
  radius: 0,
  maxWidth: 0,
  spacing: 0,
  format: "HH:mm",
  render: "text",
  symbol: "arrowUp",
  image: "",
  locked: false,
};

/** Sipka doprava v stvorci 1 × 1: driek a hrot (body obrysu). */
const ARROW: [number, number][] = [
  [0.05, 0.36],
  [0.5, 0.36],
  [0.5, 0.08],
  [0.95, 0.5],
  [0.5, 0.92],
  [0.5, 0.64],
  [0.05, 0.64],
];

/** Obojsmerna sipka vlavo a vpravo v stvorci 1 × 1. */
const DOUBLE_ARROW: [number, number][] = [
  [0.03, 0.5],
  [0.33, 0.12],
  [0.33, 0.36],
  [0.67, 0.36],
  [0.67, 0.12],
  [0.97, 0.5],
  [0.67, 0.88],
  [0.67, 0.64],
  [0.33, 0.64],
  [0.33, 0.88],
];

/** Tvar a otocenie (stupne v smere hodinovych ruciciek, os y dole) kazdeho symbolu. */
const SYMBOLS: Record<FaceSymbol, { shape: [number, number][]; angle: number }> = {
  arrowRight: { shape: ARROW, angle: 0 },
  arrowDownRight: { shape: ARROW, angle: 45 },
  arrowDown: { shape: ARROW, angle: 90 },
  arrowDownLeft: { shape: ARROW, angle: 135 },
  arrowLeft: { shape: ARROW, angle: 180 },
  arrowUpLeft: { shape: ARROW, angle: 225 },
  arrowUp: { shape: ARROW, angle: 270 },
  arrowUpRight: { shape: ARROW, angle: 315 },
  arrowsLeftRight: { shape: DOUBLE_ARROW, angle: 0 },
  arrowsUpDown: { shape: DOUBLE_ARROW, angle: 90 },
};

/** Symboly cela v poradi ponuky. */
export const FACE_SYMBOLS = Object.keys(SYMBOLS) as FaceSymbol[];

/** Obrys symbolu v stvorci 1 × 1 - otoceny okolo stredu, vzdy vnutri stvorca. */
export function symbolOutline(symbol: FaceSymbol): [number, number][] {
  const { shape, angle } = SYMBOLS[symbol] ?? SYMBOLS.arrowUp;
  const a = (angle * Math.PI) / 180;
  const cos = Math.cos(a);
  const sin = Math.sin(a);
  return shape.map(([x, y]) => {
    const dx = x - 0.5;
    const dy = y - 0.5;
    return [0.5 + dx * cos - dy * sin, 0.5 + dx * sin + dy * cos];
  });
}

/** Celo zo starsej hlavicky tabule - zodpoveda BoardFaces.FromHeader na serveri. */
export function headerFace(header: BoardHeaderDto): BoardFaceDto {
  const element = (patch: Partial<FaceElementDto>): FaceElementDto => ({ ...NEW_ELEMENT, ...patch });
  return {
    id: "",
    name: "",
    builtIn: false,
    margin: { left: 0, top: header.height, right: 0, bottom: 0 },
    rowGap: null,
    rowGroup: 0,
    groupGap: 0,
    moduleWidth: 0,
    elements: [
      element({ kind: "rect", height: header.height, fill: "band" }),
      ...header.texts.map((t) => element({ x: t.x, y: t.y, size: t.height, text: t.text })),
      ...(header.clock ? [element({ kind: "clock", x: header.clock.x, y: header.clock.y, size: header.clock.height })] : []),
    ],
  };
}

/** Celo tabule: predloha z profilu, inak hlavicka (stare zaznamy); null = bez cela alebo predloha neexistuje. */
export function boardFace(board: BoardDto, faces: readonly BoardFaceDto[] | undefined): BoardFaceDto | null {
  return resolveFace(board.profile.face, board.profile.header, faces);
}

/** Celo z kluca predlohy, inak z hlavicky. */
export function resolveFace(id: string | null, header: BoardHeaderDto | null, faces: readonly BoardFaceDto[] | undefined): BoardFaceDto | null {
  if (id) return faces?.find((f) => f.id === id) ?? null;
  return header ? headerFace(header) : null;
}

/** Hodnoty premennych cela tabule; {name} je nazov tabule. */
export function faceValues(board: BoardDto): Record<string, string> {
  return { ...board.profile.faceValues, name: board.name };
}

const VARIABLE = /\{([a-zA-Z][a-zA-Z0-9]*)\}/g;

/** Text s dosadenymi hodnotami premennych; neznama premenna = prazdny text. */
export function substitute(text: string, values: Record<string, string>): string {
  return text.replace(VARIABLE, (_, key: string) => values[key] ?? "");
}

/** Premenne v textoch cela v poradi vyskytu, okrem {name} (nazov tabule). */
export function faceVariables(face: BoardFaceDto | null): string[] {
  const keys: string[] = [];
  for (const element of face?.elements ?? [])
    if (element.kind === "text") for (const match of element.text.matchAll(VARIABLE)) if (match[1] !== "name" && !keys.includes(match[1])) keys.push(match[1]);
  return keys;
}

export function hasFaceClock(face: BoardFaceDto | null): boolean {
  return !!face?.elements.some((e) => e.kind === "clock");
}

/** Cas hodin v tvare predlohy. */
export function clockText(time: Date, format: FaceClockFormat): string {
  const two = (n: number) => String(n).padStart(2, "0");
  const hours = format === "H:mm" ? String(time.getHours()) : two(time.getHours());
  return format === "HH:mm:ss" ? `${hours}:${two(time.getMinutes())}:${two(time.getSeconds())}` : `${hours}:${two(time.getMinutes())}`;
}

/** Plocha, ku ktorej je prvok ukotveny; null = riadok, ktory tabula nema. */
export function anchorBox(g: FaceGeometry, element: FaceElementDto): Box | null {
  const { face, matrix } = g;
  switch (element.anchor) {
    case "matrix":
      return matrix;
    case "row":
      return g.rows[element.row - 1] ?? null;
    case "top":
      return { x: face.x, y: face.y, w: face.w, h: matrix.y - face.y };
    case "bottom":
      return { x: face.x, y: matrix.y + matrix.h, w: face.w, h: face.y + face.h - matrix.y - matrix.h };
    case "left":
      return { x: face.x, y: face.y, w: matrix.x - face.x, h: face.h };
    case "right":
      return { x: matrix.x + matrix.w, y: face.y, w: face.x + face.w - matrix.x - matrix.w, h: face.h };
    default:
      return face;
  }
}

/** Rozmer plochy v px: kladny = tolko bodov, 0 a menej = od polohy po vzdialeny okraj ukotvenia (zaporny o tolko menej). */
function extent(size: number, offset: number, room: number, centered: boolean, unit: number): number {
  if (size > 0) return size * unit;
  return Math.max(0, (centered ? room : room - offset * unit) + size * unit);
}

/** Poloha prvku sirky w a vysky h (px) v ukotveni podla zarovnania. */
export function place(anchor: Box, element: FaceElementDto, w: number, h: number, unit: number): Box {
  const x =
    element.align === "right"
      ? anchor.x + anchor.w - element.x * unit - w
      : element.align === "center"
        ? anchor.x + anchor.w / 2 + element.x * unit - w / 2
        : anchor.x + element.x * unit;
  const y =
    element.vAlign === "bottom"
      ? anchor.y + anchor.h - element.y * unit - h
      : element.vAlign === "middle"
        ? anchor.y + anchor.h / 2 + element.y * unit - h / 2
        : anchor.y + element.y * unit;
  return { x, y, w, h };
}

/** Plocha prvku druhu rect; null = ukotvenie neexistuje. */
export function rectBox(g: FaceGeometry, element: FaceElementDto): Box | null {
  const anchor = anchorBox(g, element);
  if (!anchor) return null;
  const w = extent(element.width, element.x, anchor.w, element.align === "center", g.unit);
  const h = extent(element.height, element.y, anchor.h, element.vAlign === "middle", g.unit);
  return place(anchor, element, w, h, g.unit);
}

/** Riadky textu prvku s velkostou pisma v px. */
export function textLines(element: FaceElementDto, values: Record<string, string>, unit: number): { text: string; size: number }[] {
  return substitute(element.text, values)
    .split("\n")
    .map((text, i) => ({ text, size: (i > 0 && element.size2 > 0 ? element.size2 : element.size) * unit }));
}

/** Vyska bloku riadkov: kazdy riadok o LINE_SPACING vyssi nez pismo, posledny len pismo. */
export function textHeight(lines: { size: number }[]): number {
  return lines.reduce((sum, line, i) => sum + line.size * (i < lines.length - 1 ? LINE_SPACING : 1), 0);
}

function resolveColor(style: FaceStyle, value: string, fallback: string | null): string | null {
  if (!value) return fallback;
  return value.startsWith("#") ? value : (style.colors[value] ?? fallback);
}

/** Pismo, hrubka a farba textu: prvok > pomenovany styl zo vzhladu > ostatne texty cela. */
export function textLook(style: FaceStyle, element: FaceElementDto): { font: string; bold: boolean; color: string } {
  const named = element.style ? style.styles[element.style] : undefined;
  return {
    font: named?.font || style.font,
    bold: element.bold ?? named?.bold ?? style.bold,
    color: resolveColor(style, element.color, named?.color ?? style.colors.text) ?? "#fff",
  };
}

function setTextStyle(ctx: TextMeasure, size: number, bold: boolean, font: string, spacing: number) {
  ctx.font = `${bold ? "bold " : ""}${size}px ${font}`;
  ctx.letterSpacing = `${spacing}px`;
}

/** Plochy a texty cela (bez hodin) v px pri mierke 1, orezane na plochu vo vnutri ramu. */
export function drawFace(ctx: CanvasRenderingContext2D, face: BoardFaceDto, g: FaceGeometry, style: FaceStyle, values: Record<string, string>) {
  ctx.save();
  ctx.beginPath();
  ctx.rect(g.face.x, g.face.y, g.face.w, g.face.h);
  ctx.clip();
  ctx.textBaseline = "top";
  ctx.textAlign = "left";
  for (const element of face.elements) {
    if (element.kind === "rect") drawRect(ctx, element, g, style);
    else if (element.kind === "text") drawText(ctx, element, g, style, values);
    else if (element.kind === "symbol") drawSymbol(ctx, element, g, style);
    else if (element.kind === "image") drawImage(ctx, element, g);
  }
  ctx.restore();
}

function drawRect(ctx: CanvasRenderingContext2D, element: FaceElementDto, g: FaceGeometry, style: FaceStyle) {
  const box = rectBox(g, element);
  if (!box || box.w <= 0 || box.h <= 0) return;
  const radius = Math.min(element.radius * g.unit, box.w / 2, box.h / 2);
  const fill = resolveColor(style, element.fill, null);
  if (fill) {
    ctx.fillStyle = fill;
    ctx.beginPath();
    ctx.roundRect(box.x, box.y, box.w, box.h, radius);
    ctx.fill();
  }
  const border = element.border * g.unit;
  if (border > 0) {
    ctx.strokeStyle = resolveColor(style, element.color, style.colors.text) ?? "#fff";
    ctx.lineWidth = border;
    ctx.beginPath();
    ctx.roundRect(box.x + border / 2, box.y + border / 2, Math.max(0, box.w - border), Math.max(0, box.h - border), Math.max(0, radius - border / 2));
    ctx.stroke();
  }
}

/** Rozlozenie textu: plocha bloku, riadky s ich sirkou a vzhlad; null = ukotvenie neexistuje. */
function textLayout(ctx: TextMeasure, element: FaceElementDto, g: FaceGeometry, style: FaceStyle, values: Record<string, string>) {
  const anchor = anchorBox(g, element);
  if (!anchor) return null;
  const lines = textLines(element, values, g.unit);
  const look = textLook(style, element);
  const spacing = element.spacing * g.unit;
  const limit = element.maxWidth > 0 ? element.maxWidth * g.unit : Number.POSITIVE_INFINITY;
  const widths = lines.map((line) => {
    setTextStyle(ctx, line.size, look.bold, look.font, spacing);
    return Math.min(limit, ctx.measureText(line.text).width);
  });
  ctx.letterSpacing = "0px";
  return { box: place(anchor, element, Math.max(0, ...widths), textHeight(lines), g.unit), lines, widths, look, spacing, limit };
}

/** Plocha symbolu: stvorec velkosti pisma; null = ukotvenie neexistuje. */
export function symbolBox(g: FaceGeometry, element: FaceElementDto): Box | null {
  const anchor = anchorBox(g, element);
  return anchor ? place(anchor, element, element.size * g.unit, element.size * g.unit, g.unit) : null;
}

function drawSymbol(ctx: CanvasRenderingContext2D, element: FaceElementDto, g: FaceGeometry, style: FaceStyle) {
  const box = symbolBox(g, element);
  if (!box) return;
  ctx.fillStyle = resolveColor(style, element.color, style.colors.text) ?? "#fff";
  ctx.beginPath();
  symbolOutline(element.symbol).forEach(([x, y], i) => {
    const px = box.x + x * box.w;
    const py = box.y + y * box.h;
    if (i === 0) ctx.moveTo(px, py);
    else ctx.lineTo(px, py);
  });
  ctx.closePath();
  ctx.fill();
}

/** Obrazok v ploche prvku (rozmery ako plocha), zmenseny so zachovanim pomeru a na stred. */
function drawImage(ctx: CanvasRenderingContext2D, element: FaceElementDto, g: FaceGeometry) {
  const box = rectBox(g, element);
  const image = faceImage(element.image);
  if (!box || !image || box.w <= 0 || box.h <= 0) return;
  const scale = Math.min(box.w / image.naturalWidth, box.h / image.naturalHeight);
  const w = image.naturalWidth * scale;
  const h = image.naturalHeight * scale;
  ctx.drawImage(image, box.x + (box.w - w) / 2, box.y + (box.h - h) / 2, w, h);
}

function drawText(ctx: CanvasRenderingContext2D, element: FaceElementDto, g: FaceGeometry, style: FaceStyle, values: Record<string, string>) {
  const layout = textLayout(ctx, element, g, style, values);
  if (!layout) return;
  const { box, lines, widths, look, spacing, limit } = layout;
  ctx.fillStyle = look.color;
  let y = box.y;
  lines.forEach((line, i) => {
    setTextStyle(ctx, line.size, look.bold, look.font, spacing);
    const w = widths[i];
    const x = element.align === "right" ? box.x + box.w - w : element.align === "center" ? box.x + (box.w - w) / 2 : box.x;
    if (Number.isFinite(limit)) ctx.fillText(line.text, x, y, limit);
    else ctx.fillText(line.text, x, y);
    y += line.size * LINE_SPACING;
  });
  ctx.letterSpacing = "0px";
}

/** Text v bodoch: sirka a vyska v bodoch, 1 = bod svieti (po riadkoch). */
export interface DotText {
  width: number;
  height: number;
  lit: Uint8Array;
}

/** Cas bodmi: bitmapove pismo len cislic (ako pismo ELEN s cislicami) vysoke najviac rows bodov; null = prilis nizke. */
export function dotText(text: string, rows: number): DotText | null {
  const height = Math.max(1, Math.round(rows));
  const source = PixelGlyphSource.forRow(height);
  if (!source) return null;
  const glyphs = [...text].map((ch) => source.glyph(ch.charCodeAt(0), ch, 0x30));
  const width = glyphs.reduce((sum, glyph) => sum + (glyph?.width ?? 0), 0);
  const lit = new Uint8Array(width * height);
  let x0 = 0;
  for (const glyph of glyphs) {
    if (!glyph) continue;
    for (let y = 0; y < height; y++) for (let x = 0; x < glyph.width; x++) if (glyph.isSet(x, y)) lit[y * width + x0 + x] = 1;
    x0 += glyph.width;
  }
  return { width, height, lit };
}

/** Kreslenie hodin bodmi materialu tabule: plocha cislic (px), body a farba rozsvieteneho bodu (null = predvolena). */
export type DisplayClock = (box: Box, dots: DotText, color: string | null) => void;

/** Rozlozenie hodin: plocha cislic, text casu a body (pri kresleni bodmi); null = ukotvenie neexistuje. */
function clockLayout(ctx: TextMeasure, element: FaceElementDto, g: FaceGeometry, style: FaceStyle, time: Date, dotted: boolean) {
  const anchor = anchorBox(g, element);
  if (!anchor) return null;
  const text = clockText(time, element.format);
  const dots = element.render === "display" && dotted ? dotText(text, element.size) : null;
  if (dots) return { box: place(anchor, element, dots.width * g.unit, dots.height * g.unit, g.unit), text, dots };
  const size = element.size * g.unit;
  setTextStyle(ctx, size, true, style.clockFont, element.spacing * g.unit);
  const width = ctx.measureText(text).width;
  ctx.letterSpacing = "0px";
  return { box: place(anchor, element, width, size, g.unit), text, dots: null };
}

/**
 * Plochy prvkov cela v px pri mierke 1 (v poradi prvkov; null = ukotvenie neexistuje) - ako ich kreslenie; dotted =
 * hodiny s kreslenim bodmi sa kreslia bodmi (maticove tabule, listove nie).
 */
export function faceBoxes(
  ctx: TextMeasure,
  face: BoardFaceDto,
  g: FaceGeometry,
  style: FaceStyle,
  values: Record<string, string>,
  time: Date,
  dotted: boolean,
): (Box | null)[] {
  return face.elements.map((element) => {
    switch (element.kind) {
      case "rect":
      case "image":
        return rectBox(g, element);
      case "symbol":
        return symbolBox(g, element);
      case "text":
        return textLayout(ctx, element, g, style, values)?.box ?? null;
      default:
        return clockLayout(ctx, element, g, style, time, dotted)?.box ?? null;
    }
  });
}

/** Hodiny cela v case time - kreslia sa pri kazdom prekresleni aj s pozadim, ktore prekryje predosly cas. */
export function drawFaceClocks(ctx: CanvasRenderingContext2D, face: BoardFaceDto, g: FaceGeometry, style: FaceStyle, time: Date, display?: DisplayClock) {
  ctx.save();
  ctx.beginPath();
  ctx.rect(g.face.x, g.face.y, g.face.w, g.face.h);
  ctx.clip();
  ctx.textBaseline = "top";
  ctx.textAlign = "left";
  for (const element of face.elements) {
    if (element.kind !== "clock") continue;
    const layout = clockLayout(ctx, element, g, style, time, !!display);
    if (!layout) continue;
    const { box, text, dots } = layout;
    const background = resolveColor(style, element.fill, style.clockBackground);
    if (dots && display) {
      if (background) {
        ctx.fillStyle = background;
        ctx.fillRect(box.x - g.unit, box.y - g.unit / 2, box.w + 2 * g.unit, box.h + g.unit);
      }
      display(box, dots, resolveColor(style, element.color, null));
      continue;
    }
    setTextStyle(ctx, element.size * g.unit, true, style.clockFont, element.spacing * g.unit);
    if (background) {
      ctx.fillStyle = background;
      ctx.fillRect(box.x - g.unit, box.y - g.unit / 2, box.w + 2 * g.unit, box.h + g.unit);
    }
    ctx.fillStyle = resolveColor(style, element.color, style.clockColor) ?? "#fff";
    ctx.fillText(text, box.x, box.y);
  }
  ctx.letterSpacing = "0px";
  ctx.restore();
}
