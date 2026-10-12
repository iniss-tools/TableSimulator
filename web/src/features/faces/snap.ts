// Prichytavanie pri tahani prvku cela: k mriezke v bodoch tabule (od rohu bodov) a k okrajom a stredom inych prvkov,
// bodov, riadkov a plochy cela; vodiace ciary a vzdialenosti k susedom. Vsetko v bodoch tabule od rohu platna.
// Ciste funkcie - testy.
import type { FaceAlign, FaceVAlign } from "@/api";
import type { Box } from "@/render/face";

/** Ciel prichytenia na jednej osi: hodnota a plocha, ktorej je okrajom alebo stredom (na ciaru a vzdialenost). */
export interface SnapTarget {
  value: number;
  box: Box;
}

/** Ciele na oboch osiach: x = zvisle ciary (lavy, stred, pravy okraj), y = vodorovne. */
export interface SnapTargets {
  x: SnapTarget[];
  y: SnapTarget[];
}

/** Vodiaca ciara: zvisla (x) alebo vodorovna (y) na hodnote at, od from po to (body). */
export interface Guide {
  axis: "x" | "y";
  at: number;
  from: number;
  to: number;
}

/** Vzdialenost medzi presuvanym prvkom a susedom: usek na osi a jeho dlzka v bodoch. */
export interface Gap {
  axis: "x" | "y";
  from: number;
  to: number;
  /** Poloha useku na druhej osi. */
  at: number;
}

/** Mriezka: krok a pociatok (roh bodov tabule) v bodoch. */
export interface Grid {
  step: number;
  originX: number;
  originY: number;
}

export interface SnapOptions {
  /** Najvacsia vzdialenost k cielu v bodoch, ked sa este prichyti. */
  threshold: number;
  /** Prichytenie k mriezke; null = bez mriezky. */
  grid: Grid | null;
  /** Prichytenie k prvkom a strukture tabule. */
  objects: boolean;
}

export interface SnapResult {
  dx: number;
  dy: number;
  guides: Guide[];
  gaps: Gap[];
}

/** Okraje a stred plochy na osi: [zaciatok, stred, koniec]. */
const edges = (b: Box, axis: "x" | "y"): [number, number, number] => (axis === "x" ? [b.x, b.x + b.w / 2, b.x + b.w] : [b.y, b.y + b.h / 2, b.y + b.h]);

/** Ciele z ploch: okraje a stred kazdej na oboch osiach. */
export function targetsOf(boxes: Box[]): SnapTargets {
  const x: SnapTarget[] = [];
  const y: SnapTarget[] = [];
  for (const box of boxes) {
    for (const value of edges(box, "x")) x.push({ value, box });
    for (const value of edges(box, "y")) y.push({ value, box });
  }
  return { x, y };
}

/** Najblizsi ciel k niektoremu z okrajov (v poradi zaciatok, stred, koniec); null = ziadny v dosahu. */
function nearest(values: number[], targets: SnapTarget[], threshold: number): { delta: number; target: SnapTarget } | null {
  let best: { delta: number; target: SnapTarget } | null = null;
  for (const v of values)
    for (const target of targets) {
      const delta = target.value - v;
      if (Math.abs(delta) <= threshold && (!best || Math.abs(delta) < Math.abs(best.delta) - 1e-9)) best = { delta, target };
    }
  return best;
}

/** Okraj, od ktoreho sa meria poloha prvku na osi (podla zarovnania) - ten sa prichyti k mriezke. */
function reference(box: Box, axis: "x" | "y", align: FaceAlign | FaceVAlign): number {
  const [start, middle, end] = edges(box, axis);
  return align === "center" || align === "middle" ? middle : align === "right" || align === "bottom" ? end : start;
}

/** Posun hodnoty na najblizsiu ciaru mriezky. */
function toGrid(value: number, step: number, origin: number): number {
  return origin + Math.round((value - origin) / step) * step - value;
}

/** Ciary a vzdialenosti k cielu, ku ktoremu sa plocha prichytila na osi. */
function guideFor(axis: "x" | "y", moved: Box, target: SnapTarget): { guide: Guide; gap: Gap | null } {
  // ciara ide cez obe plochy na druhej osi
  const [a0, , a1] = edges(moved, axis === "x" ? "y" : "x");
  const [b0, , b1] = edges(target.box, axis === "x" ? "y" : "x");
  const guide: Guide = { axis, at: target.value, from: Math.min(a0, b0), to: Math.max(a1, b1) };
  // medzera medzi plochami na druhej osi (ak sa neprekryvaju)
  const gapAxis = axis === "x" ? "y" : "x";
  const gap = a0 >= b1 ? { from: b1, to: a0 } : b0 >= a1 ? { from: a1, to: b0 } : null;
  return { guide, gap: gap && gap.to - gap.from > 1e-6 ? { axis: gapAxis, ...gap, at: target.value } : null };
}

/**
 * Posun plochy box o (dx, dy) bodov s prichytenim: k cielu v dosahu (okraj alebo stred plochy k okraju alebo stredu
 * ciela), inak k mriezke podla okraja, od ktoreho sa prvok meria; bez nich posun ostane.
 */
export function snapMove(box: Box, dx: number, dy: number, targets: SnapTargets, align: FaceAlign, vAlign: FaceVAlign, options: SnapOptions): SnapResult {
  const moved = { ...box, x: box.x + dx, y: box.y + dy };
  const result: SnapResult = { dx, dy, guides: [], gaps: [] };
  for (const axis of ["x", "y"] as const) {
    const hit = options.objects ? nearest(edges(moved, axis), targets[axis], options.threshold) : null;
    let delta = 0;
    if (hit) delta = hit.delta;
    else if (options.grid)
      delta = toGrid(reference(moved, axis, axis === "x" ? align : vAlign), options.grid.step, axis === "x" ? options.grid.originX : options.grid.originY);
    if (axis === "x") {
      result.dx += delta;
      moved.x += delta;
    } else {
      result.dy += delta;
      moved.y += delta;
    }
  }
  // ciary az pre konecnu polohu (prichytenie na jednej osi meni ciaru na druhej)
  for (const axis of ["x", "y"] as const) {
    if (!options.objects) continue;
    const exact = nearest(edges(moved, axis), targets[axis], 1e-6);
    if (!exact) continue;
    const { guide, gap } = guideFor(axis, moved, exact.target);
    result.guides.push(guide);
    if (gap) result.gaps.push(gap);
  }
  return result;
}

/**
 * Posun volneho okraja plochy pri zmene rozmeru: value = povodna poloha okraja, delta = posun v smere osi. Prichyti sa
 * k cielu v dosahu, inak k mriezke. Vrati novy posun a vodiacu ciaru.
 */
export function snapEdge(
  axis: "x" | "y",
  value: number,
  delta: number,
  box: Box,
  targets: SnapTargets,
  options: SnapOptions,
): { delta: number; guide: Guide | null } {
  const at = value + delta;
  const hit = options.objects ? nearest([at], targets[axis], options.threshold) : null;
  if (hit) {
    const [a0, , a1] = edges(box, axis === "x" ? "y" : "x");
    const [b0, , b1] = edges(hit.target.box, axis === "x" ? "y" : "x");
    return { delta: delta + hit.delta, guide: { axis, at: hit.target.value, from: Math.min(a0, b0), to: Math.max(a1, b1) } };
  }
  if (options.grid) return { delta: delta + toGrid(at, options.grid.step, axis === "x" ? options.grid.originX : options.grid.originY), guide: null };
  return { delta, guide: null };
}
