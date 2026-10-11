// Ukazkove tabule pre nahlad vzhladu, ked v simulatore este ziadna tabula nie je: ELEN s celom (nadpis a hodiny)
// a riadkami vo vsetkych farbach pisma (bez farby, cervena, zelena, zlta) a listova s celom a farebnymi listami.
import type { BoardDto, ElementDto } from "@/api";
import { defaultProfile } from "@/lib/boards";

function text(font: number, value: string, x: number | null = null): ElementDto {
  return { type: "run", run: { x, font, codes: Array.from(value, (c) => c.charCodeAt(0)), text: value }, parts: null };
}

export const SAMPLE_BOARD_ID = "sample";

export const SAMPLE_BOARD: BoardDto = {
  id: SAMPLE_BOARD_ID,
  line: 0,
  address: 0,
  name: "",
  manufacturer: "ELEN",
  design: null,
  profile: {
    ...defaultProfile("ELEN"),
    width: 200,
    rows: 4,
    rowHeight: 10,
    face: "odchody",
  },
  fault: { kind: "none", delayMs: 0, status: 0 },
  autoCreated: false,
  stats: { frames: 0, acks: 0, naks: 0, unanswered: 0, lastFrameAt: null },
  display: {
    family: "elen",
    brightness: 100,
    brightnessGreen: 100,
    clock: null,
    clockSetAt: null,
    rows: [
      { row: 4, elements: [text(0x53, "14:35"), text(0x51, "Os 3005", 36), text(0x50, "Zilina", 90), text(0x53, "2", 190)] },
      { row: 3, elements: [text(0x53, "14:52"), text(0x52, "R 615", 36), text(0x50, "Kosice", 90), text(0x53, "1", 190)] },
      { row: 2, elements: [text(0x53, "15:10"), text(0x51, "Ex 141", 36), text(0x50, "Praha hl.n.", 90), text(0x51, "+25", 160)] },
      { row: 1, elements: [text(0x52, "Vlak mešká asi 20 minút")] },
    ],
    movingRow: 0,
    movingCodes: null,
    movingText: null,
    flashOnMs: null,
    flashOffMs: null,
    charRows: null,
    flapRows: null,
    rotatingUntil: null,
    amplifier: null,
  },
  version: 0,
  secondAddress: null,
  mismatch: null,
  poweredOff: false,
};

export const SAMPLE_FLAP_BOARD_ID = "sample-flaps";

/** Listova tabula: ciel a druh vlaku zo zoznamov (cervene listy), cas a nastupiste po jednom liste. */
export const SAMPLE_FLAP_BOARD: BoardDto = {
  ...SAMPLE_BOARD,
  id: SAMPLE_FLAP_BOARD_ID,
  manufacturer: "FERS",
  profile: {
    ...defaultProfile("FERS"),
    width: 7 * 8,
    rows: 3,
    face: "odjezdy",
    flaps: {
      linesPerRecord: 1,
      modules: [
        { line: 0, position: 0, span: 1, list: "Ciel" },
        { line: 0, position: 1, span: 1, list: "Druh" },
      ],
      lists: {
        Ciel: { A: "BRNO HL.N.", B: "*PRAHA HL.N.", C: "KARLŠTEJN" },
        Druh: { R: "*R", O: "Os", S: "Sp" },
      },
      colors: { Druh: { S: "yellow" } },
    },
  },
  display: {
    ...SAMPLE_BOARD.display,
    family: "flaps",
    rows: [],
    flapRows: [
      { row: 0, codes: "AR11342" },
      { row: 1, codes: "BO11392" },
      { row: 2, codes: "CS11503" },
    ],
  },
};
