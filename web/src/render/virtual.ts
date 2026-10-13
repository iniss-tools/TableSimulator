// Virtualna tabula: stlpce podla rozlozenia alebo podla XML INISSu, striedanie a blikanie textov ako v INISSView.
import type {
  VirtualColorsDto as VirtualColors,
  VirtualColumnDto as VirtualColumn,
  VirtualLayoutDto as VirtualLayout,
  VirtualRowDto as VirtualRow,
} from "../api/generated/types.gen";

/** Casovanie textov - z rozlozenia (INISSView Jscript.iNextString, iBlinkPause, iMaxBlinkPhase). */
export interface Timing {
  alternationMs: number;
  blinkMs: number;
  blinkPhases: number;
}

export const DEFAULT_TIMING: Timing = { alternationMs: 1500, blinkMs: 600, blinkPhases: 3 };

/**
 * Co z textu ukazat v case now (ms): casti oddelene # sa striedaju po alternationMs (vsetky polozky naraz),
 * text s jedinou neprazdnou castou a znakom # (napr. "Kosice#") blika - v poslednej faze nesvieti; prazdne casti
 * sa vynechaju.
 */
export function cellText(text: string, now: number, timing: Timing = DEFAULT_TIMING): string {
  if (!text.includes("#")) return text;
  const parts = text.split("#").filter((p) => p !== "");
  if (parts.length === 0) return "";
  if (parts.length === 1) return Math.floor(now / timing.blinkMs) % timing.blinkPhases === timing.blinkPhases - 1 ? "" : parts[0];
  return parts[Math.floor(now / timing.alternationMs) % parts.length];
}

/** Stlpec podla XML: kluc (popis) a prvky, ktore ukazuje (vlak = druh a cislo spolu). */
export interface AutoColumn {
  key: string;
  cells: string[];
}

// druh vlaku v hexadecimalnom kode je pre stroje, nie pre cestujucich
const HIDDEN = new Set(["typehex"]);

/** Prvky XML INISSu (navrhy v editore rozlozenia). */
export const CELL_NAMES = ["time", "type", "number", "name", "destination", "direction", "platform", "track", "delay", "text", "company", "line", "typehex"];

/** Text prvej polozky s menom; prazdny, ak v zazname nie je. */
export function cellOf(row: VirtualRow, name: string): string {
  return row.cells.find((c) => c.name === name)?.text ?? "";
}

/**
 * Stlpce v poradi stlpcov katalogovej tabule INISSu (podla prveho vyskytu prvku). Druh a cislo vlaku su jeden
 * stlpec; stlpec, ktory je vo vsetkych zaznamoch prazdny, sa vynecha.
 */
export function virtualColumns(rows: VirtualRow[]): AutoColumn[] {
  const names: string[] = [];
  for (const row of rows) for (const cell of row.cells) if (!names.includes(cell.name) && !HIDDEN.has(cell.name)) names.push(cell.name);
  const used = names.filter((name) => rows.some((row) => cellOf(row, name) !== ""));
  const train = used.includes("type") && used.includes("number");
  const columns: AutoColumn[] = [];
  for (const name of used) {
    if (train && (name === "type" || name === "number")) {
      if (!columns.some((c) => c.key === "train")) columns.push({ key: "train", cells: ["type", "number"] });
    } else columns.push({ key: name, cells: [name] });
  }
  return columns;
}

/** Stlpec s predvolenymi vlastnostami. */
export function defaultColumn(cells: string[]): VirtualColumn {
  return { cells, label: "", width: "", align: "left", color: "", bold: false, size: 100, separator: " " };
}

// vzhlad stlpcov podla XML: cas zvyrazneny, stanica zaberie zvysok riadku, dopravca a nazov mensie
const AUTO_STYLE: Record<string, Partial<VirtualColumn>> = {
  time: { color: "accent", bold: true },
  destination: { width: "fill", bold: true },
  delay: { color: "delay" },
  company: { color: "muted", size: 70 },
  name: { color: "muted", size: 70 },
};

/** Stlpec na vykreslenie (kluc pre React). */
export interface ColumnSpec extends VirtualColumn {
  key: string;
}

/** Stlpce tabule: z rozlozenia, alebo (bez stlpcov v rozlozeni) podla XML. */
export function layoutColumns(layout: VirtualLayout, rows: VirtualRow[]): ColumnSpec[] {
  if (layout.columns.length > 0) return layout.columns.map((c, i) => ({ ...c, key: `${i}:${c.cells.join("+")}` }));
  return virtualColumns(rows).map((c) => ({ ...defaultColumn(c.cells), ...AUTO_STYLE[c.key], key: c.key }));
}

/** Kluc predvoleneho popisu stlpca: vlak pre druh a cislo, inak prvy prvok. */
export function labelKey(cells: string[]): string {
  return cells.includes("type") && cells.includes("number") ? "train" : (cells[0] ?? "");
}

// po kolkych fazach striedania sa texty bunky stlpca uz len opakuju (prvky s inym poctom casti); strop pre istotu
const MAX_PHASES = 64;

const gcd = (a: number, b: number): number => (b === 0 ? a : gcd(b, a % b));

/**
 * Vsetky texty, ktore bunka stlpca pri striedani ukaze (blikajuci prvok rozsvieteny), bez opakovania - najdlhsi urci
 * sirku stlpca, aby sa stlpce pri striedani nehybali. Prazdne pole, ked sa text bunky nemeni (ziadny prvok s #).
 */
export function columnTexts(row: VirtualRow, column: Pick<VirtualColumn, "cells" | "separator">): string[] {
  const texts = column.cells.map((name) => cellOf(row, name));
  if (!texts.some((text) => text.includes("#"))) return [];
  const parts = texts.map((text) => (text.includes("#") ? text.split("#").filter((p) => p !== "") : [text]));
  let phases = 1;
  for (const p of parts) if (p.length > 0) phases = Math.min(MAX_PHASES, (phases * p.length) / gcd(phases, p.length));
  const result = new Set<string>();
  for (let k = 0; k < phases; k++)
    result.add(
      parts
        .map((p) => (p.length === 0 ? "" : p[k % p.length]))
        .filter((text) => text !== "")
        .join(column.separator),
    );
  return [...result];
}

/** Text bunky stlpca v case now - neprazdne prvky oddelene oddelovacom stlpca. */
export function columnText(row: VirtualRow, column: Pick<VirtualColumn, "cells" | "separator">, now: number, timing: Timing = DEFAULT_TIMING): string {
  return column.cells
    .map((name) => cellText(cellOf(row, name), now, timing))
    .filter((text) => text !== "")
    .join(column.separator);
}

/** Farba stlpca: prazdna = text tabule, accent/muted/delay z farieb rozlozenia, inak #rrggbb. */
export function columnColor(color: string, colors: VirtualColors): string | undefined {
  switch (color) {
    case "":
      return undefined;
    case "accent":
      return colors.accent;
    case "muted":
      return colors.muted;
    case "delay":
      return colors.delay;
    default:
      return color;
  }
}

/** Sirka stlpca v CSS: fill = zvysok riadku. */
export function columnWidth(width: string): string | undefined {
  if (width === "") return undefined;
  return width === "fill" ? "100%" : width;
}

/** Nadpis z rozlozenia s nazvom tabule a stanicou. */
export function layoutTitle(title: string, name: string, station: string | null | undefined): string {
  return title
    .replaceAll("{name}", name)
    .replaceAll("{station}", station ?? "")
    .trim();
}
