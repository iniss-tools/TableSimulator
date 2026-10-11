// Vzhlad tabule (designs/<id>/design.json) - vsetko, co urcuje, ako tabula vyzera, nie co ukazuje. Material podla
// technologie: body (LED aj LCD, renderer led-matrix) alebo listy (flaps); spolocne su ram, plech a celo.
import type { FaceStyle } from "./face";

/** Kreslenie: body (LED, LCD) alebo listove tabule. */
export type Renderer = "led-matrix" | "flaps";

/** Pomenovany styl textu cela; null = ako ostatne texty cela. */
export interface TextStyle {
  color: string | null;
  font: string | null;
  bold: boolean | null;
}

/** Pismo a farby cela tabule (kluc header z casov, ked celom bola len hlavicka nad bodmi). */
export interface HeaderStyle {
  /** Pozadie pasov (a panelov, ak nemaju vlastne). */
  background: string;
  /** Farba textov. */
  color: string;
  font: string;
  bold: boolean;
  clockColor: string;
  clockBackground: string;
  clockFont: string;
  /** Pozadie panelov; null = ako pasy. */
  panel: string | null;
  /** Styly textov podla nazvu (title, label, number); null = ziadne. */
  styles: Record<string, TextStyle> | null;
}

/** Farby jedneho listu. */
export interface FlapColorStyle {
  top: string;
  bottom: string;
  text: string;
}

/** Material listovej tabule. */
export interface FlapMaterial {
  /** Farby listov podla farby listu (black, red, yellow, white). */
  colors: Record<string, FlapColorStyle>;
  /** Pozadie riadku listov. */
  row: string;
  /** Deliaca ciara v strede listu. */
  split: string;
  /** Text listu, ktory zoznam nepozna. */
  unknown: string;
  font: string;
}

export interface BoardDesign {
  name: string;
  renderer: Renderer;
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
  /** Pozadie pod bodmi (modul). */
  background: string;
  /** Plech cela okolo modulov; null = ako pozadie. */
  plate: string | null;
  /** Okraj modulu nad a pod riadkom bodov v bodoch. */
  moduleMargin: number;
  /** Medzera medzi riadkami tabule v bodoch. */
  rowGap: number;
  /** Okraj medzi ramom a bodmi v bodoch. */
  padding: number;
  frame: { width: number; color: string; radius: number; style: "flat" | "metal"; badge: string };
  /** Vypln medzi riadkami; null = pozadie. */
  gapColor: string | null;
  /** Svy medzi modulmi (rozostup urcuje celo); farba null = bez svov, sirka ako podiel pitch. */
  seam: { color: string | null; width: number };
  header: HeaderStyle;
  /** Material listovej tabule; null pri vzhlade bodov. */
  flaps: FlapMaterial | null;
}

/** Pismo a farby cela tabule zo vzhladu: pasy a panely, texty, plech a pomenovane styly. */
export function faceStyle(design: BoardDesign): FaceStyle {
  const h = design.header;
  return {
    font: h.font,
    bold: h.bold,
    colors: { text: h.color, band: h.background, panel: h.panel ?? h.background, plate: design.plate ?? design.background, accent: h.clockColor },
    styles: h.styles ?? {},
    clockFont: h.clockFont,
    clockColor: h.clockColor,
    clockBackground: h.clockBackground,
  };
}

export const DEFAULT_DESIGN_ID = "elen-led";
export const DEFAULT_FLAP_DESIGN_ID = "listova";

/** Vyrobcovia s cervenymi LED tabulami APEL. */
const APEL_MANUFACTURERS = ["APEL", "APEL_AN"];

/** Predvoleny vzhlad podla vyrobcu a rodiny tabule (znakove tabule Elektrocas vyzeraju ako LCD, APEL cervene LED). */
export function defaultDesignId(family: string, manufacturer = ""): string {
  if (family === "flaps") return DEFAULT_FLAP_DESIGN_ID;
  if (APEL_MANUFACTURERS.includes(manufacturer.toUpperCase())) return "apel-led";
  return family === "chars" ? "lcd-dark" : DEFAULT_DESIGN_ID;
}

/** Kreslenie, ktore tabula danej rodiny potrebuje. */
export function rendererOf(family: string): Renderer {
  return family === "flaps" ? "flaps" : "led-matrix";
}

const HEADER: HeaderStyle = {
  background: "#0b1f4b",
  color: "#ffffff",
  font: "Arial, Helvetica, sans-serif",
  bold: true,
  clockColor: "#ffd000",
  clockBackground: "#000000",
  clockFont: "Tahoma, Verdana, sans-serif",
  panel: null,
  styles: null,
};

export const DEFAULT_DESIGN: BoardDesign = {
  name: "LED",
  renderer: "led-matrix",
  pitch: 5,
  dot: 0.78,
  shape: "circle",
  off: "#2b1a14",
  colors: { "0": "#ffb000", "1": "#ff3b22", "2": "#3dff4a", "3": "#ffc21a", mono: "#ffb000" },
  glow: 0.6,
  background: "#0c0c0c",
  plate: null,
  moduleMargin: 1,
  rowGap: 1,
  padding: 3,
  frame: { width: 10, color: "#24272b", radius: 8, style: "flat", badge: "" },
  gapColor: null,
  seam: { color: null, width: 0.5 },
  header: HEADER,
  flaps: null,
};

/** Listy ako doteraz natvrdo: cierne s bielym textom, cervene, zlte a biele. */
export const DEFAULT_FLAPS: FlapMaterial = {
  colors: {
    black: { top: "#262626", bottom: "#1d1d1d", text: "#f4f4f2" },
    red: { top: "#c83838", bottom: "#ba3333", text: "#ffffff" },
    yellow: { top: "#f6dc3c", bottom: "#efcf1f", text: "#151515" },
    white: { top: "#f3f3ef", bottom: "#e3e3de", text: "#1b1b1b" },
  },
  row: "#0d0d0d",
  split: "#000000",
  unknown: "#f2c14e",
  font: '"Arial Narrow", "Roboto Condensed", "Liberation Sans Narrow", Arial, sans-serif',
};

/** Predvoleny vzhlad listovej tabule: svetly plech, tmave texty cela. */
export const DEFAULT_FLAP_DESIGN: BoardDesign = {
  ...DEFAULT_DESIGN,
  name: "Listová",
  renderer: "flaps",
  background: "#dcdcd8",
  plate: null,
  frame: { width: 0, color: "#a9adb1", radius: 0, style: "flat", badge: "" },
  header: { ...HEADER, background: "#c6c7c2", color: "#1d1f22", clockColor: "#f4f4f2", clockBackground: "#1d1d1d", clockFont: HEADER.font },
  flaps: DEFAULT_FLAPS,
};

/** Vzhlad zo suboru doplneny predvolenymi hodnotami (aj vnorene casti - starsie subory ich nemaju). */
export function completeDesign(data: Partial<BoardDesign>): BoardDesign {
  const base = data.renderer === "flaps" ? DEFAULT_FLAP_DESIGN : DEFAULT_DESIGN;
  const flaps = data.flaps ?? base.flaps;
  return {
    ...base,
    ...data,
    colors: { ...base.colors, ...(data.colors ?? {}) },
    frame: { ...base.frame, ...(data.frame ?? {}) },
    seam: { ...base.seam, ...(data.seam ?? {}) },
    header: { ...base.header, ...(data.header ?? {}) },
    flaps: flaps && { ...DEFAULT_FLAPS, ...flaps, colors: { ...DEFAULT_FLAPS.colors, ...flaps.colors } },
  };
}

const cache = new Map<string, Promise<BoardDesign>>();

/** Nacita vzhlad; chybajuce hodnoty doplni predvolenymi. Verzia vynuti nove nacitanie po zmene suboru. */
export function loadDesign(id: string, version: number): Promise<BoardDesign> {
  const key = `${id}@${version}`;
  let promise = cache.get(key);
  if (!promise) {
    promise = fetch(`/designs/${encodeURIComponent(id)}/design.json?v=${version}`)
      .then((r) => (r.ok ? r.json() : {}))
      .then((data: Partial<BoardDesign>) => completeDesign(data))
      .catch(() => DEFAULT_DESIGN);
    cache.set(key, promise);
  }
  return promise;
}

/** Vzhlad pre tabulu rodiny family: nacitany, ak ma spravne kreslenie, inak predvoleny (napr. LED vzhlad listovej tabule). */
export function designForFamily(design: BoardDesign, family: string): BoardDesign {
  const wanted = rendererOf(family);
  if (design.renderer === wanted) return design;
  return wanted === "flaps" ? DEFAULT_FLAP_DESIGN : DEFAULT_DESIGN;
}
