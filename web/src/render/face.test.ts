import { describe, expect, test } from "bun:test";
import type { BoardFaceDto, FaceElementDto } from "../api/generated/types.gen";
import {
  anchorBox,
  clockText,
  faceVariables,
  headerFace,
  NEW_ELEMENT,
  place,
  rectBox,
  rowOffset,
  substitute,
  textHeight,
  textLines,
  type FaceGeometry,
} from "./face";
import { ledLayout } from "./led";
import { DEFAULT_DESIGN } from "./design";

const el = (patch: Partial<FaceElementDto>): FaceElementDto => ({ ...NEW_ELEMENT, ...patch });

// plocha 100 × 60 px, matica od (20, 10) 70 × 40, dva riadky po 16 px s medzerou 8; bod = 2 px
const g: FaceGeometry = {
  unit: 2,
  face: { x: 0, y: 0, w: 100, h: 60 },
  matrix: { x: 20, y: 10, w: 70, h: 40 },
  rows: [
    { x: 20, y: 10, w: 70, h: 16 },
    { x: 20, y: 34, w: 70, h: 16 },
  ],
};

const face = (elements: FaceElementDto[], patch: Partial<BoardFaceDto> = {}): BoardFaceDto => ({
  id: "f",
  name: "F",
  builtIn: false,
  margin: { left: 0, top: 0, right: 0, bottom: 0 },
  rowGap: null,
  rowGroup: 0,
  groupGap: 0,
  elements,
  ...patch,
});

describe("celo tabule", () => {
  test("ukotvenia: miesto okolo matice a riadok", () => {
    expect(anchorBox(g, el({ anchor: "left" }))).toEqual({ x: 0, y: 0, w: 20, h: 60 });
    expect(anchorBox(g, el({ anchor: "right" }))).toEqual({ x: 90, y: 0, w: 10, h: 60 });
    expect(anchorBox(g, el({ anchor: "top" }))).toEqual({ x: 0, y: 0, w: 100, h: 10 });
    expect(anchorBox(g, el({ anchor: "bottom" }))).toEqual({ x: 0, y: 50, w: 100, h: 10 });
    expect(anchorBox(g, el({ anchor: "row", row: 2 }))).toEqual(g.rows[1]);
    // riadok, ktory tabula nema
    expect(anchorBox(g, el({ anchor: "row", row: 3 }))).toBeNull();
  });

  test("zarovnanie meria polohu od zodpovedajuceho okraja", () => {
    const anchor = { x: 10, y: 20, w: 100, h: 50 };
    expect(place(anchor, el({ x: 3, y: 2 }), 10, 4, 2)).toEqual({ x: 16, y: 24, w: 10, h: 4 });
    expect(place(anchor, el({ align: "right", vAlign: "bottom", x: 3, y: 2 }), 10, 4, 2)).toEqual({ x: 94, y: 62, w: 10, h: 4 });
    expect(place(anchor, el({ align: "center", vAlign: "middle", x: -1 }), 10, 4, 2)).toEqual({ x: 53, y: 43, w: 10, h: 4 });
  });

  test("plocha: kladny rozmer v bodoch, 0 a menej po okraj ukotvenia", () => {
    expect(rectBox(g, el({ kind: "rect", anchor: "left", x: 2, y: 2, width: -3, height: -2 }))).toEqual({ x: 4, y: 4, w: 10, h: 52 });
    expect(rectBox(g, el({ kind: "rect", anchor: "matrix", y: -4, height: 3 }))).toEqual({ x: 20, y: 2, w: 70, h: 6 });
    expect(rectBox(g, el({ kind: "rect", anchor: "right", align: "right", width: 0, x: 1 }))).toEqual({ x: 90, y: 0, w: 8, h: 60 });
  });

  test("texty: premenne, riadky a ich velkost", () => {
    expect(substitute("{track}. {x}", { track: "3" })).toBe("3. ");
    const lines = textLines(el({ text: "NÁSTUPIŠTĚ\n{name}", size: 4, size2: 2.5 }), { name: "Hala" }, 2);
    expect(lines).toEqual([
      { text: "NÁSTUPIŠTĚ", size: 8 },
      { text: "Hala", size: 5 },
    ]);
    expect(textHeight(lines)).toBeCloseTo(8 * 1.15 + 5);
    expect(faceVariables(face([el({ text: "{track} {name}" }), el({ kind: "clock", text: "{x}" }), el({ text: "{platform}{track}" })]))).toEqual([
      "track",
      "platform",
    ]);
  });

  test("hodiny podla formatu", () => {
    const time = new Date(2026, 9, 11, 9, 5, 7);
    expect(clockText(time, "H:mm")).toBe("9:05");
    expect(clockText(time, "HH:mm")).toBe("09:05");
    expect(clockText(time, "HH:mm:ss")).toBe("09:05:07");
  });

  test("hlavicka = celo s pasom, textami a hodinami ako na serveri", () => {
    const converted = headerFace({ height: 14, texts: [{ x: 4, y: 2, height: 10, text: "Odchody" }], clock: { x: 160, y: 2, height: 10 } });
    expect(converted.margin.top).toBe(14);
    expect(converted.elements.map((e) => [e.kind, e.anchor, e.x, e.y, e.width, e.height, e.size, e.fill, e.text])).toEqual([
      ["rect", "face", 0, 0, 0, 14, 8, "band", ""],
      ["text", "face", 4, 2, 0, 0, 10, "", "Odchody"],
      ["clock", "face", 160, 2, 0, 0, 10, "", ""],
    ]);
  });

  test("medzery riadkov a skupin", () => {
    expect([0, 1, 2, 3, 4].map((r) => rowOffset(r, 1, 3, 5))).toEqual([0, 1, 2, 8, 9]);
    expect(rowOffset(3, 2, 0, 9)).toBe(6);
  });

  test("LED: okraje cela zvacsia tabulu, riadky so skupinami", () => {
    const design = { ...DEFAULT_DESIGN, pitch: 2, padding: 1, rowGap: 1, frame: { ...DEFAULT_DESIGN.frame, width: 4 } };
    const plain = ledLayout({ width: 10, height: 6 }, design, 2, null);
    expect(plain.width).toBe(2 * 4 + (2 + 10) * 2);
    expect(plain.height).toBe(2 * 4 + (2 + 6 + 2) * 2);

    const withFace = ledLayout(
      { width: 10, height: 6 },
      design,
      2,
      face([], { margin: { left: 5, top: 3, right: 0, bottom: 0 }, rowGap: 0, rowGroup: 2, groupGap: 4 }),
    );
    expect(withFace.geometry.matrix).toEqual({ x: (1 + 5) * 2, y: (1 + 3) * 2, w: 20, h: (6 + 4) * 2 });
    expect(withFace.geometry.rows.map((r) => r.y)).toEqual([8, 12, 24]);
    expect(withFace.width).toBe(2 * 4 + (2 + 5 + 10) * 2);
  });
});
