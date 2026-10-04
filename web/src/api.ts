// Typy a volania API servera simulatora (zrkadli TableSimulator/Api/Dtos.cs).

export type LineMode = "display" | "monitor";
export type LineStatus = "stopped" | "listening" | "connected" | "error";
export type FaultKind = "none" | "noReply" | "nak" | "delay" | "garbage" | "status";

export interface LineStats {
  bytesIn: number;
  bytesOut: number;
  frames: number;
  badFrames: number;
  discarded: number;
  connections: number;
}

export interface Line {
  number: number;
  name: string;
  tableClass: number;
  endpoint: string;
  mode: LineMode;
  autoLearn: boolean;
  manufacturer: string;
  status: LineStatus;
  statusDetail: string | null;
  inissTablePort: string;
  stats: LineStats;
  version: number;
  mirror: string | null;
  mirrorError: string | null;
}

export interface HeaderText {
  x: number;
  y: number;
  height: number;
  text: string;
}

export interface HeaderClock {
  x: number;
  y: number;
  height: number;
}

/** Hlavicka tabule - polohy a vysky v bodoch matice. */
export interface BoardHeader {
  height: number;
  texts: HeaderText[];
  clock: HeaderClock | null;
}

export interface Profile {
  width: number;
  rows: number;
  rowHeight: number;
  fontFile: string | null;
  fontMap: Record<string, number> | null;
  alternationMs: number;
  blinkOnMs: number;
  blinkOffMs: number;
  scrollSpeed: number;
  movingFont: number;
  autoSize: boolean;
  monochrome: boolean;
  swapRedGreen: boolean;
  header: BoardHeader | null;
}

export interface Run {
  x: number | null;
  font: number;
  codes: number[];
  text: string;
}

export interface RowElement {
  type: "run" | "alt";
  run: Run | null;
  parts: Run[][] | null;
}

export interface Row {
  row: number;
  elements: RowElement[];
}

/** Vrstva riadku znakovej tabule; kod -1 = znak vo vrstve nie je (plati vrstva 0). */
export interface CharLayer {
  layer: number;
  codes: number[];
  fonts: number[];
  text: string;
}

/** Riadok znakovej tabule (Elektrocas) zhora od 0. */
export interface CharRow {
  row: number;
  layers: CharLayer[];
}

export interface Display {
  /** elen = maticova tabula ELEN (rows), chars = znakova tabula Elektrocas (charRows). */
  family: "elen" | "chars" | "unknown";
  brightness: number;
  clock: string | null;
  clockSetAt: string | null;
  rows: Row[];
  movingRow: number;
  movingCodes: number[] | null;
  movingText: string | null;
  brightnessGreen: number;
  flashOnMs: number | null;
  flashOffMs: number | null;
  charRows: CharRow[] | null;
}

export interface Fault {
  kind: FaultKind;
  delayMs: number;
  /** Stavove bity, ktore tabula hlasi pri poruche "status". */
  status: number;
}

export interface BoardStats {
  frames: number;
  acks: number;
  naks: number;
  unanswered: number;
  lastFrameAt: string | null;
}

export interface Board {
  id: string;
  line: number;
  address: number;
  name: string;
  manufacturer: string;
  profile: Profile;
  design: string | null;
  fault: Fault;
  autoCreated: boolean;
  stats: BoardStats;
  display: Display;
  version: number;
  secondAddress: number | null;
}

export interface Frame {
  seq: number;
  time: string;
  line: number;
  direction: "rx" | "tx";
  address: number | null;
  hex: string;
  status: "ok" | "badChecksum" | "noBoard" | "fault" | "noise";
  kind: string;
  args: Record<string, string> | null;
}

export interface Settings {
  httpPort: number;
  allowNetwork: boolean;
  fontsFolder: string;
  designsFolder: string;
  connectHost: string;
  configPath: string;
  monitorFolder: string;
}

export interface State {
  lines: Line[];
  boards: Board[];
  supportedClasses: number[];
  /** Vyrobcovia tabul podla triedy linky (kluc = TableClass); prvy je predvoleny. */
  manufacturers: Record<string, string[]>;
  settings: Settings;
}

export interface RecordingInfo {
  name: string;
  size: number;
  created: string;
  duration: number;
  lines: number[];
  frames: number;
  note: string | null;
}

export interface RecorderStatus {
  recording: boolean;
  name: string | null;
  started: string | null;
  entries: number;
  bytes: number;
}

export interface ReplayStatus {
  name: string;
  state: "running" | "finished" | "stopped";
  position: number;
  duration: number;
  speed: number;
  loop: boolean;
  pass: number;
}

export interface Recordings {
  items: RecordingInfo[];
  recorder: RecorderStatus;
  replay: ReplayStatus | null;
}

export interface DesignInfo {
  id: string;
  name: string;
  renderer: string;
  builtIn: boolean;
}

export interface LineConfig {
  number: number;
  tableClass: number;
  endpoint: string;
  name: string;
  mode: LineMode;
  autoLearn: boolean;
  manufacturer: string;
  mirror: string | null;
}

export interface BoardConfig {
  line: number;
  address: number;
  manufacturer: string;
  name: string;
  profile: Profile;
  design: string | null;
  fault: Fault;
  secondAddress: number | null;
}

/** Predloha tabule z programu ELEN Monitor. */
export interface BoardTemplate {
  file: string;
  name: string;
  profile: Profile;
}

export interface MonitorInfo {
  available: boolean;
  folder: string;
  panels: BoardTemplate[];
  projects: string[];
}

/** Chyba volania API s textom zo servera. */
export class ApiError extends Error {}

async function call<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const data = await response.json();
      if (data?.error) message = data.error;
    } catch {
      // bez tela
    }
    throw new ApiError(message);
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  state: () => call<State>("GET", "/api/state"),
  putLine: (config: LineConfig) => call<void>("PUT", `/api/lines/${config.number}`, config),
  deleteLine: (number: number) => call<void>("DELETE", `/api/lines/${number}`),
  putBoard: (config: BoardConfig) => call<void>("PUT", `/api/boards/${config.line}/${config.address}`, config),
  deleteBoard: (id: string) => call<void>("DELETE", `/api/boards/${id}`),
  setFault: (id: string, fault: Fault) => call<void>("POST", `/api/boards/${id}/fault`, fault),
  clearBoard: (id: string) => call<void>("POST", `/api/boards/${id}/clear`),
  log: (after = 0) => call<Frame[]>("GET", `/api/log?after=${after}`),
  clearLog: () => call<void>("DELETE", "/api/log"),
  fonts: () => call<string[]>("GET", "/api/fonts"),
  designs: () => call<DesignInfo[]>("GET", "/api/designs"),
  putSettings: (settings: Settings) => call<{ restart: boolean }>("PUT", "/api/settings", settings),
  recordings: () => call<Recordings>("GET", "/api/recordings"),
  startRecording: (note: string) => call<RecorderStatus>("POST", "/api/recordings/start", { note }),
  stopRecording: () => call<RecorderStatus>("POST", "/api/recordings/stop"),
  deleteRecording: (name: string) => call<void>("DELETE", `/api/recordings/${encodeURIComponent(name)}`),
  recordingUrl: (name: string) => `/api/recordings/${encodeURIComponent(name)}/file`,
  uploadRecording: async (file: File) => {
    const response = await fetch(`/api/recordings/upload?name=${encodeURIComponent(file.name)}`, { method: "POST", body: file });
    if (!response.ok) throw new ApiError(String(response.status));
    return (await response.json()) as RecordingInfo;
  },
  play: (name: string, speed: number, loop: boolean) => call<ReplayStatus>("POST", "/api/replay", { name, speed, loop }),
  monitor: () => call<MonitorInfo>("GET", "/api/monitor"),
  importMonitor: (project: string, line: number) => call<{ count: number }>("POST", "/api/monitor/import", { project, line }),
  stopReplay: () => call<void>("POST", "/api/replay/stop"),
};

/** Nastavenie tabule z jej aktualneho stavu (na upravu). */
export function boardConfig(board: Board): BoardConfig {
  return {
    line: board.line,
    address: board.address,
    manufacturer: board.manufacturer,
    name: board.name,
    profile: { ...board.profile },
    design: board.design,
    fault: board.fault,
    secondAddress: board.secondAddress,
  };
}

/** Vyrobcovia znakovych tabul Elektrocas (rodina chars). */
export const CHAR_MANUFACTURERS = ["LCD", "LCD1", "ERP"];

/** Profil novej tabule podla vyrobcu (zodpoveda BoardProfile.DefaultFor). */
export function defaultProfile(manufacturer: string): Profile {
  const upper = manufacturer.toUpperCase();
  const size =
    upper === "ELEN16" || upper === "ELEN16KAM" || upper === "ELEKON"
      ? { width: 384, rows: 4, rowHeight: 16 }
      : CHAR_MANUFACTURERS.includes(upper)
        ? { width: 320, rows: 4, rowHeight: 16 }
        : { width: 192, rows: 2, rowHeight: 10 };
  return {
    ...size,
    fontFile: null,
    fontMap: null,
    alternationMs: 2000,
    blinkOnMs: 1000,
    blinkOffMs: 500,
    scrollSpeed: 60,
    movingFont: 0x52,
    autoSize: false,
    monochrome: false,
    swapRedGreen: false,
    header: null,
  };
}

/** Nastavenie linky z jej aktualneho stavu (na upravu). */
export function lineConfig(line: Line): LineConfig {
  return {
    number: line.number,
    tableClass: line.tableClass,
    endpoint: line.endpoint,
    name: line.name,
    mode: line.mode,
    autoLearn: line.autoLearn,
    manufacturer: line.manufacturer,
    mirror: line.mirror,
  };
}
