import { describe, expect, test } from "bun:test";
import type { VirtualRow } from "../api";
import { ALTERNATION_MS, BLINK_MS, cellText, columnText, virtualColumns } from "./virtual";

const row = (cells: Record<string, string>): VirtualRow => ({ cells: Object.entries(cells).map(([name, text]) => ({ name, text })) });

describe("cellText", () => {
  test("text bez # sa nemeni", () => {
    expect(cellText("Bratislava", 0)).toBe("Bratislava");
  });

  test("casti sa striedaju po intervale a prazdne sa vynechaju", () => {
    expect(cellText("Bratislava##Wien", 0)).toBe("Bratislava");
    expect(cellText("Bratislava##Wien", ALTERNATION_MS)).toBe("Wien");
    expect(cellText("Bratislava##Wien", 2 * ALTERNATION_MS)).toBe("Bratislava");
  });

  test("jedina cast s # blika - v poslednej faze nesvieti", () => {
    expect(cellText("Kosice#", 0)).toBe("Kosice");
    expect(cellText("Kosice#", BLINK_MS)).toBe("Kosice");
    expect(cellText("Kosice#", 2 * BLINK_MS)).toBe("");
    expect(cellText("Kosice#", 3 * BLINK_MS)).toBe("Kosice");
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
    expect(columnText(rows[0], columns[0], 0)).toBe("Os 7813");
  });

  test("bez druhu je cislo samostatny stlpec", () => {
    expect(virtualColumns([row({ number: "601", time: "11:52" })]).map((c) => c.key)).toEqual(["number", "time"]);
  });
});
