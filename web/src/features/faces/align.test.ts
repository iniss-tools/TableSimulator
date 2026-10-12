import { expect, test } from "bun:test";
import { alignDeltas, canAlign, intersecting, union } from "./align";

const a = { x: 0, y: 0, w: 10, h: 4 };
const b = { x: 20, y: 10, w: 6, h: 6 };
const c = { x: 40, y: 2, w: 4, h: 2 };

test("viac prvkov sa zarovna voci spolocnej ploche", () => {
  expect(union([a, b, c])).toEqual({ x: 0, y: 0, w: 44, h: 16 });
  expect(alignDeltas([a, b, c], "left")).toEqual([
    { dx: 0, dy: 0 },
    { dx: -20, dy: 0 },
    { dx: -40, dy: 0 },
  ]);
  expect(alignDeltas([a, b], "right").map((d) => d.dx)).toEqual([16, 0]);
  expect(alignDeltas([a, b], "vcenter").map((d) => d.dy)).toEqual([6, -5]);
  expect(alignDeltas([a, b], "bottom").map((d) => d.dy)).toEqual([12, 0]);
});

test("jeden prvok sa zarovna voci ukotveniu", () => {
  const anchor = { x: 0, y: 0, w: 100, h: 20 };
  expect(alignDeltas([b], "hcenter", anchor)).toEqual([{ dx: 27, dy: 0 }]);
  expect(alignDeltas([b], "top", anchor)).toEqual([{ dx: 0, dy: -10 }]);
});

test("rozmiestnenie s rovnakymi medzerami, krajne prvky ostanu", () => {
  // medzery: 0-10, 20-26, 40-44 → spolu 44, prvky 20 → medzera 12
  const d = alignDeltas([c, a, b], "hdistribute");
  expect(d[1]).toEqual({ dx: 0, dy: 0 });
  expect(d[2].dx).toBe(2);
  expect(d[0].dx).toBe(0);
  expect(canAlign("vdistribute", 2)).toBe(false);
  expect(alignDeltas([a, b], "hdistribute")).toEqual([
    { dx: 0, dy: 0 },
    { dx: 0, dy: 0 },
  ]);
});

test("vyber obdlznikom pretina plochy", () => {
  expect(intersecting([a, null, b, c], { x: 5, y: 1, w: 18, h: 10 })).toEqual([0, 2]);
});
