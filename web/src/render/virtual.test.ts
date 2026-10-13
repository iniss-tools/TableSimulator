import { describe, expect, test } from "bun:test";
import type { VirtualLayoutDto as VirtualLayout, VirtualRowDto as VirtualRow } from "../api/generated/types.gen";
import { cellText, columnColor, columnText, columnTexts, defaultColumn, DEFAULT_TIMING, labelKey, layoutColumns, layoutTitle, virtualColumns } from "./virtual";

const row = (cells: Record<string, string>): VirtualRow => ({ cells: Object.entries(cells).map(([name, text]) => ({ name, text })) });

const layout = (columns: VirtualLayout["columns"]): VirtualLayout => ({
  id: "test",
  name: "Test",
  builtIn: false,
  title: "{name}",
  showStation: true,
  clock: "seconds",
  showDate: false,
  columnHeaders: true,
  rows: 8,
  font: "",
  colors: {
    background: "#000000",
    text: "#ffffff",
    headBackground: "#000000",
    headText: "#ffffff",
    accent: "#ffcc00",
    muted: "#999999",
    delay: "#ff0000",
    rowLine: "#222222",
    rowAlternate: "",
  },
  columns,
  ...DEFAULT_TIMING,
});

describe("cellText", () => {
  test("text bez # sa nemeni", () => {
    expect(cellText("Bratislava", 0)).toBe("Bratislava");
  });

  test("casti sa striedaju po intervale a prazdne sa vynechaju", () => {
    expect(cellText("Bratislava##Wien", 0)).toBe("Bratislava");
    expect(cellText("Bratislava##Wien", DEFAULT_TIMING.alternationMs)).toBe("Wien");
    expect(cellText("Bratislava##Wien", 2 * DEFAULT_TIMING.alternationMs)).toBe("Bratislava");
  });

  test("jedina cast s # blika - v poslednej faze nesvieti", () => {
    expect(cellText("Kosice#", 0)).toBe("Kosice");
    expect(cellText("Kosice#", DEFAULT_TIMING.blinkMs)).toBe("Kosice");
    expect(cellText("Kosice#", 2 * DEFAULT_TIMING.blinkMs)).toBe("");
    expect(cellText("Kosice#", 3 * DEFAULT_TIMING.blinkMs)).toBe("Kosice");
  });

  test("casovanie z rozlozenia", () => {
    const timing = { alternationMs: 1000, blinkMs: 500, blinkPhases: 2 };
    expect(cellText("A#B", 1000, timing)).toBe("B");
    expect(cellText("A#", 500, timing)).toBe("");
  });
});

describe("virtualColumns", () => {
  test("poradie z XML, vlak spolu, prazdne a typehex vynechane", () => {
    const rows = [
      row({ type: "Os", number: "7813", time: "11:35", delay: "", destination: "Horne Mesto", typehex: "0A", track: "" }),
      row({ type: "R", number: "601", time: "11:52", delay: "5", destination: "Velka Ves", typehex: "0B", track: "" }),
    ];

    const columns = virtualColumns(rows);

    expect(columns.map((c) => c.key)).toEqual(["train", "time", "delay", "destination"]);
    expect(columnText(rows[0], { cells: columns[0].cells, separator: " " }, 0)).toBe("Os 7813");
  });

  test("bez druhu je cislo samostatny stlpec", () => {
    expect(virtualColumns([row({ number: "601", time: "11:52" })]).map((c) => c.key)).toEqual(["number", "time"]);
  });
});

describe("layoutColumns", () => {
  const rows = [row({ time: "11:35", destination: "Horne Mesto" })];

  test("bez stlpcov v rozlozeni podla XML s predvolenym vzhladom", () => {
    const columns = layoutColumns(layout([]), rows);
    expect(columns.map((c) => [c.key, c.color, c.width])).toEqual([
      ["time", "accent", ""],
      ["destination", "", "fill"],
    ]);
  });

  test("stlpce z rozlozenia v jeho poradi", () => {
    const columns = layoutColumns(layout([{ ...defaultColumn(["destination", "direction"]), separator: " – " }, defaultColumn(["time"])]), rows);
    expect(columns.map((c) => c.cells.join("+"))).toEqual(["destination+direction", "time"]);
    expect(columnText(row({ destination: "Kosice", direction: "Presov" }), columns[0], 0)).toBe("Kosice – Presov");
  });
});

describe("columnTexts", () => {
  test("bez striedania prazdne", () => {
    expect(columnTexts(row({ destination: "Kosice" }), defaultColumn(["destination"]))).toEqual([]);
  });

  test("vsetky faze striedania prvkov s inym poctom casti, blikanie rozsvietene", () => {
    const column = { cells: ["destination", "direction", "track"], separator: " " };
    const r = row({ destination: "A#B", direction: "x#y#z", track: "1#" });
    const texts = columnTexts(r, column);
    expect(texts).toEqual(["A x 1", "B y 1", "A z 1", "B x 1", "A y 1", "B z 1"]);
    for (let k = 0; k < 6; k++) expect(texts).toContain(columnText(r, column, k * DEFAULT_TIMING.alternationMs, { ...DEFAULT_TIMING, blinkMs: 1e9 }));
  });

  test("opakovane a prazdne texty raz", () => {
    expect(columnTexts(row({ destination: "Sered#Galanta#Sered", direction: "##" }), defaultColumn(["destination", "direction"]))).toEqual([
      "Sered",
      "Galanta",
    ]);
  });
});

describe("pomocne", () => {
  test("popis a farba stlpca, nadpis", () => {
    expect(labelKey(["type", "number"])).toBe("train");
    expect(labelKey(["delay"])).toBe("delay");
    const colors = layout([]).colors;
    expect(columnColor("accent", colors)).toBe("#ffcc00");
    expect(columnColor("", colors)).toBeUndefined();
    expect(columnColor("#123456", colors)).toBe("#123456");
    expect(layoutTitle("Odchody – {station}", "Odchody web", "Kosice")).toBe("Odchody – Kosice");
  });
});
