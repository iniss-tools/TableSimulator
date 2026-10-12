// Zarovnanie a rozmiestnenie vybranych prvkov cela ako vo Figme: viac prvkov voci ploche, ktoru spolu zaberaju, jeden
// prvok voci svojmu ukotveniu; rozmiestnenie (aspon 3 prvky) s rovnakymi medzerami, krajne ostanu. Vysledkom su posuny
// v bodoch na obrazovke - prvok si ich prepocita na X a Y podla svojho zarovnania. Ciste funkcie - testy.
import type { Box } from "@/render/face";

export type AlignOp = "left" | "hcenter" | "right" | "top" | "vcenter" | "bottom" | "hdistribute" | "vdistribute";

/** Zarovnania v poradi panela. */
export const ALIGN_OPS: AlignOp[] = ["left", "hcenter", "right", "top", "vcenter", "bottom", "hdistribute", "vdistribute"];

/** Plocha, ktoru prvky spolu zaberaju. */
export function union(boxes: Box[]): Box {
  const x = Math.min(...boxes.map((b) => b.x));
  const y = Math.min(...boxes.map((b) => b.y));
  const right = Math.max(...boxes.map((b) => b.x + b.w));
  const bottom = Math.max(...boxes.map((b) => b.y + b.h));
  return { x, y, w: right - x, h: bottom - y };
}

/** Da sa zarovnanie pouzit: rozmiestnenie az od 3 prvkov, ostatne od jedneho (voci ukotveniu). */
export function canAlign(op: AlignOp, count: number): boolean {
  return op === "hdistribute" || op === "vdistribute" ? count >= 3 : count >= 1;
}

/**
 * Posuny prvkov (v poradi plochy) pri zarovnani: frame = plocha, voci ktorej sa zarovnava (pri jednom prvku jeho
 * ukotvenie, inak spolocna plocha).
 */
export function alignDeltas(boxes: Box[], op: AlignOp, frame: Box = union(boxes)): { dx: number; dy: number }[] {
  const none = boxes.map(() => ({ dx: 0, dy: 0 }));
  if (!canAlign(op, boxes.length)) return none;
  if (op === "hdistribute" || op === "vdistribute") {
    const horizontal = op === "hdistribute";
    const start = (b: Box) => (horizontal ? b.x : b.y);
    const size = (b: Box) => (horizontal ? b.w : b.h);
    const order = boxes.map((_, i) => i).sort((a, b) => start(boxes[a]) - start(boxes[b]));
    const first = boxes[order[0]];
    const last = boxes[order[order.length - 1]];
    const span = start(last) + size(last) - start(first);
    const gap = (span - boxes.reduce((sum, b) => sum + size(b), 0)) / (boxes.length - 1);
    const deltas = [...none];
    let at = start(first);
    for (const i of order) {
      const d = at - start(boxes[i]);
      deltas[i] = horizontal ? { dx: d, dy: 0 } : { dx: 0, dy: d };
      at += size(boxes[i]) + gap;
    }
    return deltas;
  }
  return boxes.map((b) => {
    switch (op) {
      case "left":
        return { dx: frame.x - b.x, dy: 0 };
      case "hcenter":
        return { dx: frame.x + frame.w / 2 - (b.x + b.w / 2), dy: 0 };
      case "right":
        return { dx: frame.x + frame.w - (b.x + b.w), dy: 0 };
      case "top":
        return { dx: 0, dy: frame.y - b.y };
      case "vcenter":
        return { dx: 0, dy: frame.y + frame.h / 2 - (b.y + b.h / 2) };
      default:
        return { dx: 0, dy: frame.y + frame.h - (b.y + b.h) };
    }
  });
}

/** Plochy, ktore pretina obdlznik (vyber tahanim) - indexy v poradi ploch. */
export function intersecting(boxes: (Box | null)[], rect: Box): number[] {
  return boxes.flatMap((b, i) => (b && b.x < rect.x + rect.w && b.x + b.w > rect.x && b.y < rect.y + rect.h && b.y + b.h > rect.y ? [i] : []));
}
