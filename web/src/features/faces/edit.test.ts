import { describe, expect, test } from "bun:test";
import type { FaceElementDto } from "@/api";
import { NEW_ELEMENT } from "@/render/face";
import { FINE, moveElement, moveItem, referencePoint, resizeElement, snap, stepOrder } from "./edit";

const el = (patch: Partial<FaceElementDto>): FaceElementDto => ({ ...NEW_ELEMENT, ...patch });

describe("posun prvku", () => {
  test("vlavo hore zarovnany: X a Y rastu s posunom doprava a dole, prichytenie na pol bodu", () => {
    const moved = moveElement(el({ x: 2, y: 3 }), 4.3, -1.2);
    expect(moved.x).toBe(6.5);
    expect(moved.y).toBe(2);
  });

  test("vpravo a dole zarovnany: X a Y sa meraju od praveho a spodneho okraja", () => {
    const moved = moveElement(el({ align: "right", vAlign: "bottom", x: 4, y: 1 }), 3, 2);
    expect(moved.x).toBe(1);
    expect(moved.y).toBe(-1);
  });

  test("jemny krok a desatinne miesta bez chyb", () => {
    expect(moveElement(el({ x: 0.1 }), 0.2, 0, FINE).x).toBe(0.3);
    expect(snap(2.675, 0.5)).toBe(2.5);
  });
});

describe("zmena velkosti", () => {
  const box = { x: 0, y: 0, w: 20, h: 10 };

  test("plocha s pevnym rozmerom: volny okraj vpravo dole, na stred dvojnasobne", () => {
    const rect = el({ kind: "rect", width: 20, height: 10 });
    expect(resizeElement(rect, "xy", 5, 2, box)).toMatchObject({ width: 25, height: 12 });
    expect(resizeElement(rect, "x", 5, 2, box)).toMatchObject({ width: 25, height: 10 });
    expect(resizeElement({ ...rect, align: "center" }, "x", 3, 0, box).width).toBe(26);
    // vpravo zarovnana: volny je lavy okraj - tahanie dolava zvacsuje
    expect(resizeElement({ ...rect, align: "right" }, "x", -4, 0, box).width).toBe(24);
    expect(resizeElement(rect, "xy", -40, 0, box).width).toBe(1);
  });

  test("plocha po okraj ukotvenia ostane po okraj a neklesne pod 1 bod", () => {
    const rect = el({ kind: "rect", width: -2, height: 0 });
    expect(resizeElement(rect, "xy", -3, 5, box)).toMatchObject({ width: -5, height: 0 });
    expect(resizeElement(rect, "x", -50, 0, box).width).toBe(-21);
  });

  test("text meni velkost pisma podla vysky bloku, aj druhych riadkov", () => {
    const text = el({ kind: "text", size: 4, size2: 3 });
    const bigger = resizeElement(text, "xy", 0, 5, box);
    expect(bigger.size).toBe(6);
    expect(bigger.size2).toBe(4.5);
    expect(resizeElement(text, "xy", 0, -50, box).size).toBe(1);
  });

  test("hodiny bodmi maju cele riadky bodov", () => {
    const clock = el({ kind: "clock", size: 9, render: "display" });
    expect(resizeElement(clock, "xy", 0, 1.3, { ...box, h: 9 }).size).toBe(10);
  });
});

test("bod merania X a Y podla zarovnania", () => {
  const anchor = { x: 10, y: 20, w: 100, h: 40 };
  expect(referencePoint(el({}), anchor)).toEqual({ x: 10, y: 20 });
  expect(referencePoint(el({ align: "right", vAlign: "middle" }), anchor)).toEqual({ x: 110, y: 40 });
  expect(referencePoint(el({ align: "center", vAlign: "bottom" }), anchor)).toEqual({ x: 60, y: 60 });
});

test("presun polozky v zozname", () => {
  expect(moveItem(["a", "b", "c"], 0, 2)).toEqual(["b", "c", "a"]);
  expect(moveItem(["a", "b", "c"], 2, 0)).toEqual(["c", "a", "b"]);
});

test("vybrane prvky dopredu a dozadu o jedno miesto, susedne spolu", () => {
  expect(stepOrder(4, [1], 1)).toEqual([0, 2, 1, 3]);
  expect(stepOrder(4, [0, 1], 1)).toEqual([2, 0, 1, 3]);
  // vrchny prvok ostane, prvok pod nim sa nepreskoci
  expect(stepOrder(3, [1, 2], 1)).toEqual([0, 1, 2]);
  expect(stepOrder(4, [0, 2], -1)).toEqual([0, 2, 1, 3]);
  expect(stepOrder(4, [3, 1], -1)).toEqual([1, 0, 3, 2]);
});
