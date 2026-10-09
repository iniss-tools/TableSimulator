import { describe, expect, test } from "bun:test";
import type { BoardDto, BoardProfileDto, FlapProfileDto } from "../api/generated/types.gen";
import { defaultProfile } from "../lib/boards";
import { flapLabel, flapModules, layoutFlaps } from "./flaps";

const flaps: FlapProfileDto = {
  linesPerRecord: 1,
  modules: [
    { line: 0, position: 0, span: 1, list: "Ciel" },
    { line: 0, position: 2, span: 1, list: "Druh" },
  ],
  lists: {
    Ciel: { A: "BRATISLAVA", B: "*BRATISLAVA", "0": "" },
    Druh: { R: "R", O: "Os" },
  },
  colors: {},
};

function board(manufacturer: string, rows: { row: number; codes: string }[], profile: BoardProfileDto = { ...defaultProfile(manufacturer), flaps }): BoardDto {
  return {
    id: "3-7",
    line: 3,
    address: 7,
    name: "",
    manufacturer,
    profile,
    design: null,
    fault: { kind: "none", delayMs: 0, status: 0 },
    autoCreated: false,
    version: 1,
    stats: { frames: 0, acks: 0, naks: 0, unanswered: 0, lastFrameAt: null },
    secondAddress: null,
    mismatch: null,
    display: {
      family: "flaps",
      brightness: 100,
      brightnessGreen: 100,
      flashOnMs: null,
      flashOffMs: null,
      clock: null,
      clockSetAt: null,
      rows: [],
      movingRow: 0,
      movingCodes: null,
      movingText: null,
      charRows: null,
      flapRows: rows,
      rotatingUntil: null,
      amplifier: null,
    },
  };
}

describe("listova tabula", () => {
  test("pozicie bez modulu profilu su samostatne moduly", () => {
    const modules = flapModules({ ...defaultProfile("FERS"), flaps }, 0, 4);
    expect(modules.map((m) => [m.position, m.list])).toEqual([
      [0, "Ciel"],
      [1, null],
      [2, "Druh"],
      [3, null],
    ]);
  });

  test("text listu, cerveny list, kod bez textu a prazdny list", () => {
    const layout = layoutFlaps(
      board(
        "FERS",
        [
          { row: 0, codes: "B R5" },
          { row: 1, codes: "AXO " },
        ],
        { ...defaultProfile("FERS"), width: 4 * 8, rows: 2, flaps },
      ),
    );
    const [first, second] = layout.rows;
    expect(first.cells.map((c) => [c.label, c.color])).toEqual([
      ["BRATISLAVA", "red"],
      ["", "black"],
      ["R", "black"],
      ["5", "black"],
    ]);
    expect(second.cells.map((c) => c.label)).toEqual(["BRATISLAVA", "X", "Os", ""]);
    // stlpce zarovnane: modul cielov siroky podla najdlhsieho textu zoznamu, rovnako v kazdom riadku
    expect(first.cells.map((c) => c.x)).toEqual(second.cells.map((c) => c.x));
    expect(first.cells[0].width).toBeGreaterThan(first.cells[1].width * 5);
    expect(first.cells[1].narrow).toBe(true);
  });

  test("farba listu z profilu prebije * z textu", () => {
    const colored: FlapProfileDto = { ...flaps, colors: { Druh: { O: "yellow" }, Ciel: { B: "white" } } };
    const layout = layoutFlaps(board("FERS", [{ row: 0, codes: "BXO " }], { ...defaultProfile("FERS"), width: 4 * 8, rows: 1, flaps: colored }));
    expect(layout.rows[0].cells.map((c) => [c.label, c.color])).toEqual([
      ["BRATISLAVA", "white"],
      ["X", "black"],
      ["Os", "yellow"],
      ["", "black"],
    ]);
  });

  test("ERS: kod 0 je prazdny list, ak ho zoznam nema", () => {
    const profile = { ...defaultProfile("ERS"), flaps: null };
    expect(flapLabel(profile, { line: 0, position: 0, span: 1, list: null }, "0", "0")).toEqual({ label: "", unknown: false, key: null });
    expect(flapLabel(profile, { line: 0, position: 0, span: 1, list: null }, "0", null)).toEqual({ label: "0", unknown: false, key: null });
    const layout = layoutFlaps(board("ERS", [{ row: 2, codes: "12A000" }], profile));
    expect(layout.rows).toHaveLength(4);
    expect(layout.rows[2].cells.map((c) => c.label).slice(0, 4)).toEqual(["1", "2", "A", ""]);
  });
});
