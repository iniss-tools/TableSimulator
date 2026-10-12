// Meranie s Alt ako vo Figme: vzdialenosti vybranej plochy od plochy pod mysou (alebo od ukotvenia) v bodoch. Pri
// medzere medzi plochami jej dlzka, inak (prekrytie, plocha v ploche) rozdiely zodpovedajucich okrajov. Ciste funkcie.
import type { Box } from "@/render/face";
import type { Gap } from "./snap";

/** Mensie rozdiely sa neukazuju. */
const EPS = 0.005;

/** Useky vzdialenosti plochy a od plochy b na oboch osiach. */
export function measure(a: Box, b: Box): Gap[] {
  const gaps: Gap[] = [];
  for (const axis of ["x", "y"] as const) {
    const [a0, a1] = axis === "x" ? [a.x, a.x + a.w] : [a.y, a.y + a.h];
    const [b0, b1] = axis === "x" ? [b.x, b.x + b.w] : [b.y, b.y + b.h];
    // usek ide stredom prekrytia na druhej osi, inak stredom plochy a
    const [c0, c1] = axis === "x" ? [a.y, a.y + a.h] : [a.x, a.x + a.w];
    const [d0, d1] = axis === "x" ? [b.y, b.y + b.h] : [b.x, b.x + b.w];
    const low = Math.max(c0, d0);
    const high = Math.min(c1, d1);
    const at = low <= high ? (low + high) / 2 : (c0 + c1) / 2;
    const push = (from: number, to: number) => {
      if (to - from > EPS) gaps.push({ axis, from, to, at });
    };
    if (a1 <= b0) push(a1, b0);
    else if (b1 <= a0) push(b1, a0);
    else {
      push(Math.min(a0, b0), Math.max(a0, b0));
      push(Math.min(a1, b1), Math.max(a1, b1));
    }
  }
  return gaps;
}
