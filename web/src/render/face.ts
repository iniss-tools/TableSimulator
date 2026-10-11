// Celo tabule (predloha BoardFaceDto): plochy, texty a hodiny okolo matice. Rozlozenie je ciste (testy), kreslenie ide
// na platno v px pri mierke 1 (volajuci nastavi posun a mierku). Maticu - LED, LCD aj listy - kresli jej renderer do
// okna cela; geometriu (matica a riadky v px) mu dava on. Polohy a rozmery prvkov su v bodoch, unit = px na bod.
import type { BoardDto, BoardFaceDto, BoardHeaderDto, FaceClockFormat, FaceElementDto } from "../api/generated/types.gen";

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

/** Ako cela vyzeraju: pismo a farby podla nazvov (text, band, panel, plate, accent) zo vzhladu tabule. */
export interface FaceStyle {
  font: string;
  bold: boolean;
  colors: Record<string, string>;
  clockFont: string;
  clockColor: string;
  clockBackground: string;
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
  color: "",
  fill: "",
  border: 0,
  radius: 0,
  maxWidth: 0,
  spacing: 0,
  format: "HH:mm",
};

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

function setTextStyle(ctx: CanvasRenderingContext2D, size: number, bold: boolean, font: string, spacing: number) {
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

function drawText(ctx: CanvasRenderingContext2D, element: FaceElementDto, g: FaceGeometry, style: FaceStyle, values: Record<string, string>) {
  const anchor = anchorBox(g, element);
  if (!anchor) return;
  const lines = textLines(element, values, g.unit);
  const bold = element.bold ?? style.bold;
  const spacing = element.spacing * g.unit;
  const limit = element.maxWidth > 0 ? element.maxWidth * g.unit : Number.POSITIVE_INFINITY;
  const widths = lines.map((line) => {
    setTextStyle(ctx, line.size, bold, style.font, spacing);
    return Math.min(limit, ctx.measureText(line.text).width);
  });
  const box = place(anchor, element, Math.max(0, ...widths), textHeight(lines), g.unit);
  ctx.fillStyle = resolveColor(style, element.color, style.colors.text) ?? "#fff";
  let y = box.y;
  lines.forEach((line, i) => {
    setTextStyle(ctx, line.size, bold, style.font, spacing);
    const w = widths[i];
    const x = element.align === "right" ? box.x + box.w - w : element.align === "center" ? box.x + (box.w - w) / 2 : box.x;
    if (Number.isFinite(limit)) ctx.fillText(line.text, x, y, limit);
    else ctx.fillText(line.text, x, y);
    y += line.size * LINE_SPACING;
  });
  ctx.letterSpacing = "0px";
}

/** Hodiny cela v case time - kreslia sa pri kazdom prekresleni aj s pozadim, ktore prekryje predosly cas. */
export function drawFaceClocks(ctx: CanvasRenderingContext2D, face: BoardFaceDto, g: FaceGeometry, style: FaceStyle, time: Date) {
  ctx.save();
  ctx.beginPath();
  ctx.rect(g.face.x, g.face.y, g.face.w, g.face.h);
  ctx.clip();
  ctx.textBaseline = "top";
  ctx.textAlign = "left";
  for (const element of face.elements) {
    if (element.kind !== "clock") continue;
    const anchor = anchorBox(g, element);
    if (!anchor) continue;
    const size = element.size * g.unit;
    const text = clockText(time, element.format);
    setTextStyle(ctx, size, true, style.clockFont, element.spacing * g.unit);
    const box = place(anchor, element, ctx.measureText(text).width, size, g.unit);
    const background = resolveColor(style, element.fill, style.clockBackground);
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
