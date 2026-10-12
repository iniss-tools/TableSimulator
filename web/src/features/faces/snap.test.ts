import { describe, expect, test } from "bun:test";
import { snapEdge, snapMove, targetsOf, type SnapOptions } from "./snap";

// matica od (10, 20) 100 × 40 bodov, iny prvok 60-80 × 0-10
const matrix = { x: 10, y: 20, w: 100, h: 40 };
const other = { x: 60, y: 0, w: 20, h: 10 };
const targets = targetsOf([matrix, other]);
const objects: SnapOptions = { threshold: 2, grid: null, objects: true };

describe("posun s prichytenim", () => {
  test("lavy okraj sa prichyti k lavemu okraju bodov a ukaze ciaru", () => {
    const box = { x: 30, y: 13, w: 10, h: 4 };
    const r = snapMove(box, -18.6, 0, targets, "left", "top", objects);
    expect(r.dx).toBeCloseTo(-20);
    expect(r.dy).toBe(0);
    expect(r.guides).toEqual([{ axis: "x", at: 10, from: 13, to: 60 }]);
    // medzera od prvku nad maticou po maticu
    expect(r.gaps).toEqual([{ axis: "y", from: 17, to: 20, at: 10 }]);
  });

  test("stred sa prichyti k stredu ineho prvku, mimo dosahu ostane posun", () => {
    const box = { x: 0, y: 30, w: 10, h: 4 };
    expect(snapMove(box, 64.4, 0, targets, "left", "top", objects).dx).toBeCloseTo(65);
    expect(snapMove(box, 40, 0, targets, "left", "top", objects).dx).toBe(40);
  });

  test("mriezka od rohu bodov podla okraja, od ktoreho sa meria", () => {
    const grid: SnapOptions = { threshold: 0, grid: { step: 8, originX: 10, originY: 20 }, objects: false };
    const box = { x: 13, y: 21, w: 5, h: 3 };
    // lavy okraj 13 + 4 = 17 → 18 (10 + 8)
    expect(snapMove(box, 4, 0, targets, "left", "top", grid).dx).toBe(5);
    // pravy okraj 18 + 4 = 22 → 26
    expect(snapMove(box, 4, 0, targets, "right", "top", grid).dx).toBe(8);
    // y: horny okraj 21 + 2 = 23 → 20 (pociatok mriezky)
    expect(snapMove(box, 0, 2, targets, "left", "top", grid).dy).toBe(-1);
  });

  test("prvky maju prednost pred mriezkou, bez prichytenia ostane posun", () => {
    const both: SnapOptions = { threshold: 2, grid: { step: 8, originX: 10, originY: 20 }, objects: true };
    const box = { x: 0, y: 30, w: 10, h: 4 };
    expect(snapMove(box, 59, 0, targets, "left", "top", both).dx).toBe(60);
    const none: SnapOptions = { threshold: 2, grid: null, objects: false };
    expect(snapMove(box, 59.3, 0, targets, "left", "top", none).dx).toBe(59.3);
  });
});

test("okraj pri zmene rozmeru sa prichyti k cielu alebo mriezke", () => {
  const box = { x: 20, y: 30, w: 30, h: 5 };
  const r = snapEdge("x", 50, 9, box, targets, objects);
  expect(r.delta).toBe(10);
  expect(r.guide?.at).toBe(60);
  const grid: SnapOptions = { threshold: 0, grid: { step: 4, originX: 10, originY: 20 }, objects: false };
  expect(snapEdge("x", 50, 2.5, box, targets, grid).delta).toBe(4);
});
