// Ukazkova tabula ELEN pre nahlad vzhladu, ked v simulatore este ziadna tabula nie je: hlavicka s hodinami
// a riadky vo vsetkych farbach pisma (bez farby, cervena, zelena, zlta).
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
    header: {
      height: 14,
      texts: [{ x: 4, y: 2, height: 10, text: "Odchody / Departures" }],
      clock: { x: 160, y: 2, height: 10 },
    },
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
  },
  version: 0,
  secondAddress: null,
  mismatch: null,
};
