// Vzhlad tabule (designs/<id>/design.json) - vsetko, co urcuje, ako tabula vyzera, nie co ukazuje.
import type { FaceStyle } from "./face";

export interface LedDesign {
  name: string;
  renderer: "led-matrix";
  /** Vzdialenost stredov bodov v CSS px pri mierke 1. */
  pitch: number;
  /** Priemer bodu ako podiel pitch (0-1). */
  dot: number;
  shape: "circle" | "square" | "rounded";
  /** Farba zhasnuteho bodu; null = nekreslit. */
  off: string | null;
  /** Farby podla farby pisma ELEN: 0 bez farby, 1 cervena, 2 zelena, 3 zlta. */
  colors: Record<string, string>;
  /** Ziara okolo rozsvieteneho bodu 0-1. */
  glow: number;
  background: string;
  /** Medzera medzi riadkami tabule v bodoch. */
  rowGap: number;
  /** Okraj medzi ramom a bodmi v bodoch. */
  padding: number;
  frame: { width: number; color: string; radius: number };
  /** Vypln medzi riadkami; null = pozadie. */
  gapColor: string | null;
  /** Pismo a farby cela tabule (pasy, panely, texty a hodiny) - kluc z casov, ked celom bola len hlavicka. */
  header: HeaderStyle;
}

export interface HeaderStyle {
  background: string;
  color: string;
  font: string;
  bold: boolean;
  clockColor: string;
  clockBackground: string;
  clockFont: string;
}

/** Pismo a farby cela tabule zo vzhladu: pas a panel maju pozadie hlavicky, texty jej farbu a pismo. */
export function faceStyle(design: LedDesign): FaceStyle {
  const h = design.header;
  return {
    font: h.font,
    bold: h.bold,
    colors: { text: h.color, band: h.background, panel: h.background, plate: design.background, accent: h.clockColor },
    clockFont: h.clockFont,
    clockColor: h.clockColor,
    clockBackground: h.clockBackground,
  };
}

export const DEFAULT_DESIGN_ID = "elen-led";

/** Vyrobcovia s cervenymi LED tabulami APEL. */
const APEL_MANUFACTURERS = ["APEL", "APEL_AN"];

/** Predvoleny vzhlad podla vyrobcu a rodiny tabule (znakove tabule Elektrocas vyzeraju ako LCD, APEL cervene LED). */
export function defaultDesignId(family: string, manufacturer = ""): string {
  if (APEL_MANUFACTURERS.includes(manufacturer.toUpperCase())) return "apel-led";
  return family === "chars" ? "lcd-dark" : DEFAULT_DESIGN_ID;
}

export const DEFAULT_DESIGN: LedDesign = {
  name: "LED",
  renderer: "led-matrix",
  pitch: 5,
  dot: 0.78,
  shape: "circle",
  off: "#2b1a14",
  colors: { "0": "#ffb000", "1": "#ff3b22", "2": "#3dff4a", "3": "#ffc21a", mono: "#ffb000" },
  glow: 0.6,
  background: "#0c0c0c",
  rowGap: 1,
  padding: 3,
  frame: { width: 10, color: "#24272b", radius: 8 },
  gapColor: null,
  header: {
    background: "#0b1f4b",
    color: "#ffffff",
    font: "Arial, Helvetica, sans-serif",
    bold: true,
    clockColor: "#ffd000",
    clockBackground: "#000000",
    clockFont: "Tahoma, Verdana, sans-serif",
  },
};

const cache = new Map<string, Promise<LedDesign>>();

/** Nacita vzhlad; chybajuce hodnoty doplni predvolenymi. Verzia vynuti nove nacitanie po zmene suboru. */
export function loadDesign(id: string, version: number): Promise<LedDesign> {
  const key = `${id}@${version}`;
  let promise = cache.get(key);
  if (!promise) {
    promise = fetch(`/designs/${encodeURIComponent(id)}/design.json?v=${version}`)
      .then((r) => (r.ok ? r.json() : {}))
      .then((data: Partial<LedDesign>) => ({
        ...DEFAULT_DESIGN,
        ...data,
        colors: { ...DEFAULT_DESIGN.colors, ...(data.colors ?? {}) },
        frame: { ...DEFAULT_DESIGN.frame, ...(data.frame ?? {}) },
        header: { ...DEFAULT_DESIGN.header, ...(data.header ?? {}) },
      }))
      .catch(() => DEFAULT_DESIGN);
    cache.set(key, promise);
  }
  return promise;
}
