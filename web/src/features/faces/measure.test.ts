import { expect, test } from "bun:test";
import { measure } from "./measure";

test("medzera medzi plochami vedla seba", () => {
  // b vpravo od a, prekryvaju sa zvislo v 2..4
  expect(measure({ x: 0, y: 0, w: 10, h: 4 }, { x: 15, y: 2, w: 5, h: 6 })).toEqual([
    { axis: "x", from: 10, to: 15, at: 3 },
    { axis: "y", from: 0, to: 2, at: 5 },
    { axis: "y", from: 4, to: 8, at: 5 },
  ]);
});

test("plocha v ploche: vzdialenosti od vsetkych okrajov", () => {
  const gaps = measure({ x: 2, y: 3, w: 4, h: 2 }, { x: 0, y: 0, w: 10, h: 10 });
  expect(gaps.map((g) => [g.axis, g.from, g.to])).toEqual([
    ["x", 0, 2],
    ["x", 6, 10],
    ["y", 0, 3],
    ["y", 5, 10],
  ]);
});

test("zarovnane okraje sa nemeraju", () => {
  expect(measure({ x: 0, y: 0, w: 10, h: 4 }, { x: 0, y: 6, w: 10, h: 4 })).toEqual([{ axis: "y", from: 4, to: 6, at: 5 }]);
});
