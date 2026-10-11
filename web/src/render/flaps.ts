// Listova tabula (ERS, FERS): moduly s listami. INISS posiela kody listov, text listu urcuju zoznamy profilu
// (z TKatalog a TabTab stanice) - zodpoveda FlapLabels v jadre. Farbu listu urcuje profil (nastavenie simulatora),
// inak text zacinajuci * je na cervenom liste a ostatne na ciernom.
import type { BoardDto, BoardFaceDto, BoardProfileDto, FlapModuleDto } from "../api/generated/types.gen";
import { drawFace, drawFaceClocks, hasFaceClock, type FaceGeometry, type FaceStyle } from "./face";

/** Vyska modulu (riadku) v bodoch platna pri mierke 1. */
export const FLAP_ROW = 34;
/** Sirka jedneho znaku textu listu. */
const CHAR = 15;
/** Vnutorny okraj modulu. */
const INNER = 7;
/** Medzera medzi modulmi a riadkami, okraj tabule. */
const GAP = 4;
const ROW_GAP = 6;
const PAD = 8;
/** Najdlhsi text, podla ktoreho sa meria sirka stlpca. */
const MAX_CHARS = 24;
/** Bod cela listovej tabule v px - pozicia (8 bodov) ma sirku znaku listu. */
export const FLAP_UNIT = CHAR / 8;

/** Farba listu (kluce ako FlapColors v jadre). */
export type FlapColor = "black" | "red" | "yellow" | "white";

/** Farby listu: horna a dolna polovica a text. */
export const FLAP_COLORS: Record<FlapColor, { top: string; bottom: string; text: string }> = {
  black: { top: "#262626", bottom: "#1d1d1d", text: "#f4f4f2" },
  red: { top: "#c83838", bottom: "#ba3333", text: "#ffffff" },
  yellow: { top: "#f6dc3c", bottom: "#efcf1f", text: "#151515" },
  white: { top: "#f3f3ef", bottom: "#e3e3de", text: "#1b1b1b" },
};

const isColor = (value: string | undefined): value is FlapColor => value !== undefined && value in FLAP_COLORS;

export interface FlapCell {
  /** Kluc bunky (riadok a pozicia) - na zistenie zmeny a animaciu otacania. */
  key: string;
  x: number;
  width: number;
  label: string;
  color: FlapColor;
  /** Modul ma zoznam listov, ale kod v nom nie je - ukazuje sa kod. */
  unknown: boolean;
  /** Jednoznakovy modul - text v strede. */
  narrow: boolean;
}

export interface FlapRow {
  y: number;
  cells: FlapCell[];
}

export interface FlapLayout {
  width: number;
  height: number;
  rows: FlapRow[];
  /** Plocha tabule, moduly a riadky pre celo. */
  geometry: FaceGeometry;
}

/** Kod prazdneho listu okrem medzery: ERS posiela namiesto medzier 0. */
export function blankCode(manufacturer: string): string | null {
  return manufacturer.toUpperCase() === "ERS" ? "0" : null;
}

/** Moduly riadku zlava; pozicie bez modulu profilu su samostatne moduly bez zoznamu. */
export function flapModules(profile: BoardProfileDto, row: number, positions: number): FlapModuleDto[] {
  const flaps = profile.flaps;
  const line = flaps ? row % Math.max(1, flaps.linesPerRecord) : 0;
  const modules = (flaps?.modules ?? []).filter((m) => m.line === line).sort((a, b) => a.position - b.position);
  const width = Math.max(positions, ...modules.map((m) => m.position + m.span));
  const result: FlapModuleDto[] = [];
  let next = 0;
  for (const module of modules) {
    for (; next < module.position; next++) result.push({ line, position: next, span: 1, list: null });
    if (module.position < next) continue;
    result.push(module);
    next = module.position + module.span;
  }
  for (; next < width; next++) result.push({ line, position: next, span: 1, list: null });
  return result;
}

/** Text listu modulu pre kody riadku; key = kod, pod ktorym je list v zozname (pre farbu). */
export function flapLabel(
  profile: BoardProfileDto,
  module: FlapModuleDto,
  codes: string,
  blank: string | null,
): { label: string; unknown: boolean; key: string | null } {
  const code = codes.slice(module.position, module.position + module.span).padEnd(module.span);
  const list = module.list ? profile.flaps?.lists[module.list] : undefined;
  if (list) {
    const key = code in list ? code : code.trim() in list ? code.trim() : null;
    if (key !== null) return { label: list[key], unknown: false, key };
  }
  const trimmed = code.trim();
  const empty = trimmed.length === 0 || (blank !== null && [...trimmed].every((c) => c === blank));
  return { label: empty ? "" : trimmed, unknown: !!list && !empty, key: null };
}

/** Farba listu: nastavena v profile, inak text zacinajuci * cerveny, ostatne cierne. */
export function flapColor(profile: BoardProfileDto, module: FlapModuleDto, key: string | null, label: string): FlapColor {
  const set = module.list && key !== null ? profile.flaps?.colors[module.list]?.[key] : undefined;
  if (isColor(set)) return set;
  return label.startsWith("*") ? "red" : "black";
}

/** Sirka modulu v znakoch: najdlhsi text jeho zoznamu (bez *), aspon pocet pozicii. */
function moduleChars(profile: BoardProfileDto, module: FlapModuleDto): number {
  const list = module.list ? profile.flaps?.lists[module.list] : undefined;
  const longest = list ? Math.max(0, ...Object.values(list).map((v) => v.replace(/^\*/, "").length)) : 0;
  return Math.min(MAX_CHARS, Math.max(module.span, longest));
}

/** Rozlozenie tabule: moduly kazdeho riadku s polohou, textom a farbou listu; okolo modulov miesto pre celo. */
export function layoutFlaps(board: BoardDto, face: BoardFaceDto | null = null): FlapLayout {
  const margin = face?.margin ?? { left: 0, top: 0, right: 0, bottom: 0 };
  const left = margin.left * FLAP_UNIT;
  const top = margin.top * FLAP_UNIT;
  const profile = board.profile;
  const codes = new Map((board.display.flapRows ?? []).map((r) => [r.row, r.codes]));
  const rowCount = Math.max(profile.rows, ...[...codes.keys()].map((r) => r + 1));
  const positions = Math.max(Math.floor(profile.width / 8), ...[...codes.values()].map((c) => c.length));
  const blank = blankCode(board.manufacturer);

  // stlpce sa zarovnaju v ramci riadku zaznamu - sirky modulov podla zoznamov
  const rows: FlapRow[] = [];
  let width = 0;
  for (let row = 0; row < rowCount; row++) {
    const text = codes.get(row) ?? "";
    let x = left + PAD;
    const cells = flapModules(profile, row, positions).map((module) => {
      const chars = moduleChars(profile, module);
      const cellWidth = chars * CHAR + 2 * INNER;
      const { label, unknown, key } = flapLabel(profile, module, text, blank);
      const color = flapColor(profile, module, key, label);
      const shown = label.startsWith("*") ? label.slice(1) : label;
      const cell: FlapCell = { key: `${row}:${module.position}`, x, width: cellWidth, label: shown, color, unknown, narrow: chars === 1 };
      x += cellWidth + GAP;
      return cell;
    });
    width = Math.max(width, x - GAP + PAD - left);
    rows.push({ y: top + PAD + row * (FLAP_ROW + ROW_GAP), cells });
  }
  const inner = { w: Math.max(width, 2 * PAD + CHAR), h: 2 * PAD + rowCount * FLAP_ROW + Math.max(0, rowCount - 1) * ROW_GAP };
  const total = { w: left + inner.w + margin.right * FLAP_UNIT, h: top + inner.h + margin.bottom * FLAP_UNIT };
  const matrix = { x: left + PAD, y: top + PAD, w: inner.w - 2 * PAD, h: inner.h - 2 * PAD };
  return {
    width: total.w,
    height: total.h,
    rows,
    geometry: {
      unit: FLAP_UNIT,
      face: { x: 0, y: 0, w: total.w, h: total.h },
      matrix,
      rows: rows.map((r) => ({ x: matrix.x, y: r.y, w: matrix.w, h: FLAP_ROW })),
    },
  };
}

const COLORS = {
  frame: "#dcdcd8",
  row: "#0d0d0d",
  unknown: "#f2c14e",
  split: "rgba(0, 0, 0, 0.55)",
};

/** Potlac cela listovej tabule: tmave texty na svetlom plechu, pasy a panely o odtien tmavsie. */
const FACE_STYLE: FaceStyle = {
  font: "Arial, Helvetica, sans-serif",
  bold: true,
  colors: { text: "#1d1f22", band: "#c6c7c2", panel: "#c6c7c2", plate: COLORS.frame, accent: "#c83838" },
  clockFont: "Arial, Helvetica, sans-serif",
  clockColor: "#f4f4f2",
  clockBackground: "#1d1d1d",
};

/** Celo listovej tabule s hodnotami premennych a casom hodin. */
export interface FlapFace {
  face: BoardFaceDto;
  values: Record<string, string>;
  time: Date;
}

/** Pismo listu v danej vyske. */
const flapFont = (size: number) => `600 ${size}px "Arial Narrow", "Roboto Condensed", "Liberation Sans Narrow", Arial, sans-serif`;
const FONT_SIZE = Math.round(FLAP_ROW * 0.6);

/**
 * Nakresli tabulu v mierke; flip(kluc) vrati 0-1 pre modul, ktory sa prave otaca (vyska textu), null = stoji.
 */
export function drawFlaps(
  ctx: CanvasRenderingContext2D,
  layout: FlapLayout,
  scale: number,
  flip: (key: string) => number | null = () => null,
  face: FlapFace | null = null,
) {
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  ctx.fillStyle = COLORS.frame;
  ctx.fillRect(0, 0, layout.width, layout.height);
  if (face) drawFace(ctx, face.face, layout.geometry, FACE_STYLE, face.values);
  ctx.textBaseline = "middle";
  ctx.textAlign = "left";

  const { matrix } = layout.geometry;
  for (const row of layout.rows) {
    ctx.fillStyle = COLORS.row;
    ctx.fillRect(matrix.x - PAD / 2, row.y - 2, matrix.w + PAD, FLAP_ROW + 4);
    for (const cell of row.cells) {
      const half = FLAP_ROW / 2;
      const palette = FLAP_COLORS[cell.color];
      ctx.fillStyle = palette.top;
      ctx.fillRect(cell.x, row.y, cell.width, half);
      ctx.fillStyle = palette.bottom;
      ctx.fillRect(cell.x, row.y + half, cell.width, half);

      if (cell.label) {
        const squeeze = flip(cell.key);
        ctx.save();
        ctx.beginPath();
        ctx.rect(cell.x + 2, row.y, cell.width - 4, FLAP_ROW);
        ctx.clip();
        ctx.translate(0, row.y + half);
        if (squeeze !== null) ctx.scale(1, Math.max(0.05, squeeze));
        // dlhy text je na liste mensim pismom (ako ODRIEKNUTY na skutocnej tabuli)
        ctx.font = flapFont(FONT_SIZE);
        const room = cell.width - (cell.narrow ? 4 : 2 * INNER);
        const measured = ctx.measureText(cell.label).width;
        if (measured > room) ctx.font = flapFont(Math.max(Math.round(FONT_SIZE * 0.4), Math.floor((FONT_SIZE * room) / measured)));
        ctx.fillStyle = cell.unknown ? COLORS.unknown : palette.text;
        ctx.textAlign = cell.narrow ? "center" : "left";
        ctx.fillText(cell.label, cell.narrow ? cell.x + cell.width / 2 : cell.x + INNER, 1);
        ctx.restore();
      }

      // stred listu - hrana, kde sa list preklapa
      ctx.fillStyle = COLORS.split;
      ctx.fillRect(cell.x, row.y + half - 0.5, cell.width, 1);
    }
  }
  if (face && hasFaceClock(face.face)) drawFaceClocks(ctx, face.face, layout.geometry, FACE_STYLE, face.time);
}
