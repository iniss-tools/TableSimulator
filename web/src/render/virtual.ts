// Virtualna tabula: stlpce zo zaznamov XML INISSu, striedanie a blikanie textov ako v INISSView.
import type { VirtualRow } from "../api";

/** Interval striedania casti textu oddelenych # (INISSView Jscript.iNextString). */
export const ALTERNATION_MS = 1500;
/** Dlzka jednej fazy blikania (Jscript.iBlinkPause). */
export const BLINK_MS = 600;
/** Pocet faz blikania - v poslednej text nesvieti (Jscript.iMaxBlinkPhase). */
export const BLINK_PHASES = 3;

/**
 * Co z textu ukazat v case now (ms): casti oddelene # sa striedaju po ALTERNATION_MS (vsetky polozky naraz),
 * text s jedinou neprazdnou castou a znakom # (napr. "Kosice#") blika; prazdne casti sa vynechaju.
 */
export function cellText(text: string, now: number): string {
  if (!text.includes("#")) return text;
  const parts = text.split("#").filter((p) => p !== "");
  if (parts.length === 0) return "";
  if (parts.length === 1) return Math.floor(now / BLINK_MS) % BLINK_PHASES === BLINK_PHASES - 1 ? "" : parts[0];
  return parts[Math.floor(now / ALTERNATION_MS) % parts.length];
}

/** Stlpec tabule: kluc (popis) a prvky XML, ktore ukazuje (vlak = druh a cislo spolu). */
export interface VirtualColumn {
  key: string;
  cells: string[];
}

// druh vlaku v hexadecimalnom kode je pre stroje, nie pre cestujucich
const HIDDEN = new Set(["typehex"]);

/** Text prvej polozky s menom; prazdny, ak v zazname nie je. */
export function cellOf(row: VirtualRow, name: string): string {
  return row.cells.find((c) => c.name === name)?.text ?? "";
}

/**
 * Stlpce v poradi stlpcov katalogovej tabule INISSu (podla prveho vyskytu prvku). Druh a cislo vlaku su jeden
 * stlpec; stlpec, ktory je vo vsetkych zaznamoch prazdny, sa vynecha.
 */
export function virtualColumns(rows: VirtualRow[]): VirtualColumn[] {
  const names: string[] = [];
  for (const row of rows)
    for (const cell of row.cells) if (!names.includes(cell.name) && !HIDDEN.has(cell.name)) names.push(cell.name);
  const used = names.filter((name) => rows.some((row) => cellOf(row, name) !== ""));
  const train = used.includes("type") && used.includes("number");
  const columns: VirtualColumn[] = [];
  for (const name of used) {
    if (train && (name === "type" || name === "number")) {
      if (!columns.some((c) => c.key === "train")) columns.push({ key: "train", cells: ["type", "number"] });
    } else columns.push({ key: name, cells: [name] });
  }
  return columns;
}

/** Text bunky stlpca v case now - casti vlaku oddelene medzerou. */
export function columnText(row: VirtualRow, column: VirtualColumn, now: number): string {
  return column.cells
    .map((name) => cellText(cellOf(row, name), now))
    .filter((text) => text !== "")
    .join(" ");
}
