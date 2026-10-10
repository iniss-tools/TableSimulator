// Spravy WebSocketu /ws (TableSimulator.Contracts.Live.LiveMessage) - druh vo vlastnosti "type", obsah su DTO API.
import type { BoardDto, FrameDto, LineDto, RecorderStatusDto, ReplayStatusDto, ReportArchiveEntryDto } from "./generated/types.gen";

export type LiveMessage =
  /** Pribudli alebo ubudli linky ci tabule, zmenilo sa nastavenie - cely stav znova. */
  | { type: "structure" }
  | { type: "board"; board: BoardDto }
  | { type: "line"; line: LineDto }
  /** Len pre /ws?log=1. */
  | { type: "frames"; frames: FrameDto[] }
  /** Zmenil sa subor vzhladu. */
  | { type: "design"; id: string }
  /** Pribudol alebo ubudol zaznam komunikacie. */
  | { type: "recordings" }
  | { type: "recorder"; status: RecorderStatusDto }
  | { type: "replay"; status: ReplayStatusDto }
  /** Zmenili sa XML tabul (mena suborov) alebo definicie virtualnych tabul. */
  | { type: "virtual"; sources: string[]; definitions: boolean }
  /** Zmenili sa spravy, playlisty, skupiny alebo panely informacnych panelov. */
  | { type: "signage" }
  /** Zmenili sa scenare porucha alebo ich beh. */
  | { type: "scenarios" }
  /** Pribudli hlasenia v sledovanom archive hlaseni (monitor); reset = sleduje sa iny subor, nacitat znova. */
  | { type: "reports"; date: string | null; added: ReportArchiveEntryDto[]; reset: boolean; total: number; skipped: number };
