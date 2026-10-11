// Uprava prvku cela tahanim v nahlade: posun a zmena velkosti v bodoch podla zarovnania prvku (X vpravo zarovnaneho
// prvku sa meria od praveho okraja ukotvenia, Y dole zarovnaneho od spodneho). Ciste funkcie - testy.
import type { FaceElementDto } from "@/api";
import type { Box } from "@/render/face";

/** Co sa taha: cely prvok, alebo uchyt na volnom okraji (x = zvisly okraj, y = vodorovny, xy = roh). */
export type DragMode = "move" | "x" | "y" | "xy";

/** Krok prichytenia v bodoch: predvolene pol bodu, s Alt desatina, s Shift pri sipkach 5 bodov. */
export const SNAP = 0.5;
export const FINE = 0.1;
export const COARSE = 5;

/** Hodnota prichytena na krok (bez chyb desatinnych miest). */
export function snap(value: number, step: number): number {
  const snapped = Math.round(value / step) * step;
  return Math.round(snapped * 1000) / 1000;
}

/** Smer osi: +1, ak hodnota rastie s posunom doprava/dole, -1 pri zarovnani k pravemu/spodnemu okraju. */
const signX = (e: FaceElementDto) => (e.align === "right" ? -1 : 1);
const signY = (e: FaceElementDto) => (e.vAlign === "bottom" ? -1 : 1);

/** Prvok posunuty o (dx, dy) bodov od povodnej polohy. */
export function moveElement(element: FaceElementDto, dx: number, dy: number, step = SNAP): FaceElementDto {
  return { ...element, x: snap(element.x + signX(element) * dx, step), y: snap(element.y + signY(element) * dy, step) };
}

/** Volny okraj prvku - ten, ktorym sa pri zmene velkosti hybe (opacny k okraju, od ktoreho sa meria). */
export function freeEdges(element: FaceElementDto): { left: boolean; top: boolean } {
  return { left: element.align === "right", top: element.vAlign === "bottom" };
}

/**
 * Nova velkost plochy pri posune volneho okraja o delta bodov (v smere osi obrazovky): kladna velkost = tolko bodov,
 * nulova a zaporna = po okraj ukotvenia (ostava tak, len sa posunie odstup). Rozmer neklesne pod 1 bod.
 */
function resizeSide(size: number, extent: number, delta: number, centered: boolean, step: number): number {
  const grow = (centered ? 2 : 1) * delta;
  if (size > 0) return Math.max(1, snap(size + grow, step));
  // po okraj: velkost je zaporny odstup od okraja; rozmer = extent + zmena
  const next = Math.min(0, snap(size + grow, step));
  return Math.max(next, snap(size - (extent - 1), step));
}

/**
 * Prvok po potiahnuti uchytu o (dx, dy) bodov. Plocha meni sirku a vysku (box = jej povodny rozmer v bodoch), text
 * a hodiny velkost pisma podla zvisleho posunu rohu (box = povodna vyska bloku textu v bodoch).
 */
export function resizeElement(element: FaceElementDto, mode: Exclude<DragMode, "move">, dx: number, dy: number, box: Box, step = SNAP): FaceElementDto {
  const edges = freeEdges(element);
  const ddx = edges.left ? -dx : dx;
  const ddy = edges.top ? -dy : dy;
  if (element.kind === "rect") {
    const next = { ...element };
    if (mode !== "y") next.width = resizeSide(element.width, box.w, ddx, element.align === "center", step);
    if (mode !== "x") next.height = resizeSide(element.height, box.h, ddy, element.vAlign === "middle", step);
    return next;
  }
  // pismo: pomer novej a povodnej vysky bloku; hodiny bodmi maju cele riadky bodov
  const grow = (element.vAlign === "middle" ? 2 : 1) * ddy;
  const factor = box.h > 0 ? Math.max(0.05, (box.h + grow) / box.h) : 1;
  const sizeStep = element.kind === "clock" && element.render === "display" ? 1 : step;
  const size = Math.max(1, snap(element.size * factor, sizeStep));
  const ratio = size / element.size;
  return { ...element, size, size2: element.size2 > 0 ? Math.max(0.5, snap(element.size2 * ratio, FINE)) : 0 };
}

/** Bod ukotvenia, od ktoreho sa meraju X a Y prvku (podla zarovnania). */
export function referencePoint(element: FaceElementDto, anchor: Box): { x: number; y: number } {
  const x = element.align === "right" ? anchor.x + anchor.w : element.align === "center" ? anchor.x + anchor.w / 2 : anchor.x;
  const y = element.vAlign === "bottom" ? anchor.y + anchor.h : element.vAlign === "middle" ? anchor.y + anchor.h / 2 : anchor.y;
  return { x, y };
}

/** Polozka zoznamu presunuta na iny index. */
export function moveItem<T>(items: readonly T[], from: number, to: number): T[] {
  const moved = [...items];
  const [item] = moved.splice(from, 1);
  moved.splice(to, 0, item);
  return moved;
}
