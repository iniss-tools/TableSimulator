// Typy a volania API servera simulatora (zrkadli projekt TableSimulator.Contracts).
import { hasText, t } from "./i18n";
import { authHeaders } from "./auth";

export { accessKey, authHeaders, deviceLink, forgetAccessKey, socketUrl } from "./auth";

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

/** Spravy od INISSu patria inemu vyrobcovi, nez ma tabula nastaveneho. */
export interface ManufacturerMismatch {
  /** Co na to ukazuje: elen.extendedFont, lcd.lcd1Commands, lcd.lcdCommands. */
  evidence: string;
  suggested: string[];
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
  mismatch: ManufacturerMismatch | null;
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
  /** Vyzadovat prihlasenie aj z tohto pocitaca (zo siete sa vyzaduje vzdy). */
  requireLogin: boolean;
  /** Priecinky logov INISSu s XML tabul pre virtualne tabule. */
  virtualFolders: string[];
}

/** Virtualna tabula - co z XML tabule INISSu ukazat na jednej obrazovke (ako definicia v INISSView). */
export interface VirtualBoard {
  id: number;
  name: string;
  description: string;
  /** Subor XML bez pripony (SAVE_XML). */
  source: string;
  /** Pocet zaznamov; null = vsetky. */
  rowCount: number | null;
  /** Poradie prveho zaznamu (od 1). */
  rowStart: number;
  /** Text namiesto # v stanici a smere; prazdny = casti sa striedaju. */
  separator: string;
  /** Sablona XSL z INISSView (zatial sa nepouziva). */
  template: string;
  /** Atribut x pre sablonu (zatial sa nepouziva). */
  parameter: string;
  /** Rozlozenie; prazdne = predvolene. */
  layout: string;
}

export type VirtualClock = "none" | "minutes" | "seconds";
export type VirtualAlign = "left" | "center" | "right";

/** Farby virtualnej tabule (#rrggbb); rowAlternate prazdne = bez striedania riadkov. */
export interface VirtualColors {
  background: string;
  text: string;
  headBackground: string;
  headText: string;
  accent: string;
  muted: string;
  delay: string;
  rowLine: string;
  rowAlternate: string;
}

/** Stlpec rozlozenia: prvky XML, popis (prazdny = predvoleny), sirka ("", "fill", "12%", "6em"), farba ("", accent, muted, delay, #rrggbb). */
export interface VirtualColumn {
  cells: string[];
  label: string;
  width: string;
  align: VirtualAlign;
  color: string;
  bold: boolean;
  /** Velkost pisma v percentach riadku. */
  size: number;
  /** Text medzi prvkami stlpca. */
  separator: string;
}

/** Rozlozenie virtualnej tabule - data (vstavane alebo vlastne v databaze). */
export interface VirtualLayout {
  id: string;
  name: string;
  builtIn: boolean;
  /** Nadpis; {name} = nazov tabule, {station} = stanica. */
  title: string;
  showStation: boolean;
  clock: VirtualClock;
  showDate: boolean;
  columnHeaders: boolean;
  /** Pocet riadkov pre velkost pisma, ak tabula nema pevny pocet zaznamov. */
  rows: number;
  font: string;
  colors: VirtualColors;
  /** Prazdne = stlpce podla XML. */
  columns: VirtualColumn[];
  alternationMs: number;
  blinkMs: number;
  blinkPhases: number;
}

/** Subor XML tabule v priecinkoch logov INISSu; casy su miestne casy pocitaca s INISSom. */
export interface VirtualSource {
  name: string;
  path: string;
  modified: string;
  station: string | null;
  key: string | null;
  written: string | null;
  validTo: string | null;
  rows: number;
  /** Platnost uplynula - INISS subor neobnovuje. */
  stale: boolean;
  error: string | null;
}

export interface VirtualFolder {
  path: string;
  exists: boolean;
}

export interface VirtualOverview {
  boards: VirtualBoard[];
  sources: VirtualSource[];
  folders: VirtualFolder[];
  layouts: VirtualLayout[];
  /** Rozlozenie tabul bez vlastneho rozlozenia. */
  defaultLayout: string;
}

/** Polozka zaznamu: meno prvku z XML (time, type, number, destination…) a text s castami oddelenymi #. */
export interface VirtualCell {
  name: string;
  text: string;
}

export interface VirtualRow {
  cells: VirtualCell[];
}

/** Co ukazuje virtualna tabula. */
export interface VirtualView {
  board: VirtualBoard;
  source: VirtualSource | null;
  rows: VirtualRow[];
  layout: VirtualLayout;
}

export interface InissViewImported {
  added: number;
  replaced: number;
  skipped: number;
  duplicates: number[];
  folders: string[];
}

/** Prihlaseny pouzivatel a rezim prihlasenia. */
export interface Me {
  loginRequired: boolean;
  setupRequired: boolean;
  authenticated: boolean;
  userName: string | null;
  displayName: string | null;
  role: "admin" | "operator" | "viewer" | null;
  permissions: string[];
  /** Poziadavka prisla s pristupovym klucom (zariadenie, program) - bez hesla a odhlasenia. */
  accessKey?: boolean;
}

/** Opravnenia (rovnake nazvy ako politiky servera). */
export const PERMISSIONS = {
  view: "simulator.view",
  log: "log.view",
  edit: "simulator.edit",
  recordings: "recordings.manage",
  settings: "settings.manage",
  users: "users.manage",
  audit: "audit.view",
} as const;

export type Role = "admin" | "operator" | "viewer";

export type AccessKeyKind = "device" | "api";

/** Pristupovy kluc bez tajnej hodnoty. */
export interface AccessKeyInfo {
  id: string;
  name: string;
  kind: AccessKeyKind;
  role: Role;
  /** Zaciatok kluca na rozlisenie. */
  prefix: string;
  createdAt: string;
  createdBy: string;
  lastUsedAt: string | null;
}

/** Novy kluc - tajna hodnota sa ukaze len raz. */
export interface AccessKeyCreated {
  key: AccessKeyInfo;
  secret: string;
}

/** Roly od najvyssej. */
export const ROLES: Role[] = ["admin", "operator", "viewer"];

/** Pouzivatel pre spravu pouzivatelov (casy UTC ako ISO text). */
export interface UserInfo {
  userName: string;
  displayName: string;
  role: Role;
  disabled: boolean;
  /** Zablokovany po neuspesnych pokusoch do; null = nie. */
  lockedUntil: string | null;
  createdAt: string;
  lastLoginAt: string | null;
}

/** Zaznam auditu - kto co urobil; user "-" = neprihlaseny. */
export interface AuditEntry {
  id: number;
  time: string;
  user: string;
  action: string;
  target: string;
  success: boolean;
  error: string | null;
  details: string;
  address: string | null;
}

export interface AuditPage {
  entries: AuditEntry[];
  hasMore: boolean;
}

export interface AuditFilter {
  before?: number;
  user?: string;
  action?: string;
  limit?: number;
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

/** Chyba volania API s textom pre obsluhu. */
export class ApiError extends Error {}

/** Chyba v odpovedi servera (ProblemDetails): kod je kluc textu, popis je anglicky. */
interface ProblemError {
  code: string;
  description: string;
}

interface Problem {
  code?: string;
  detail?: string;
  errors?: ProblemError[];
}

let unauthorized: (() => void) | null = null;

/** Co urobit, ked server odpovie 401 (prihlasenie vyprsalo alebo pouzivatela vypli). */
export function onUnauthorized(handler: () => void) {
  unauthorized = handler;
}

/** Text chyby v jazyku obsluhy podla kodu; neznamy kod = popis zo servera. */
function describe(code: string | undefined, description: string | undefined, fallback: string): string {
  const key = `error.${code}`;
  return code && hasText(key) ? t(key) : description || fallback;
}

/** Chyba z odpovede servera - pri neplatnych vstupoch vsetky chyby pod sebou. */
async function apiError(response: Response): Promise<ApiError> {
  const fallback =
    response.status === 401 ? t("error.auth.required") : response.status === 403 ? t("error.auth.forbidden") : `${response.status} ${response.statusText}`;
  try {
    const data = (await response.json()) as Problem;
    if (data.errors?.length) return new ApiError(data.errors.map((e) => describe(e.code, e.description, fallback)).join("\n"));
    return new ApiError(describe(data.code, data.detail, fallback));
  } catch {
    // bez tela
    return new ApiError(fallback);
  }
}

async function call<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: body === undefined ? authHeaders() : { ...authHeaders(), "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 401 && !url.startsWith("/api/auth/")) unauthorized?.();
  if (!response.ok) throw await apiError(response);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  me: () => call<Me>("GET", "/api/auth/me"),
  login: (userName: string, password: string, remember: boolean) => call<Me>("POST", "/api/auth/login", { userName, password, remember }),
  logout: () => call<void>("POST", "/api/auth/logout"),
  setup: (userName: string, displayName: string, password: string) => call<Me>("POST", "/api/auth/setup", { userName, displayName, password }),
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
    const response = await fetch(`/api/recordings/upload?name=${encodeURIComponent(file.name)}`, { method: "POST", body: file, headers: authHeaders() });
    if (!response.ok) throw await apiError(response);
    return (await response.json()) as RecordingInfo;
  },
  play: (name: string, speed: number, loop: boolean) => call<ReplayStatus>("POST", "/api/replay", { name, speed, loop }),
  monitor: () => call<MonitorInfo>("GET", "/api/monitor"),
  importMonitor: (project: string, line: number) => call<{ count: number }>("POST", "/api/monitor/import", { project, line }),
  stopReplay: () => call<void>("POST", "/api/replay/stop"),
  virtual: () => call<VirtualOverview>("GET", "/api/virtual"),
  virtualView: (id: number) => call<VirtualView>("GET", `/api/virtual/${id}`),
  putVirtual: (board: VirtualBoard) => call<void>("PUT", `/api/virtual/${board.id}`, board),
  deleteVirtual: (id: number) => call<void>("DELETE", `/api/virtual/${id}`),
  importInissView: (content: string, replace: boolean) => call<InissViewImported>("POST", "/api/virtual/import", { content, replace }),
  putLayout: (layout: VirtualLayout) => call<void>("PUT", `/api/virtual/layouts/${encodeURIComponent(layout.id)}`, layout),
  deleteLayout: (id: string) => call<void>("DELETE", `/api/virtual/layouts/${encodeURIComponent(id)}`),
  users: () => call<UserInfo[]>("GET", "/api/users"),
  createUser: (user: { userName: string; displayName: string; role: Role; password: string }) => call<void>("POST", "/api/users", user),
  updateUser: (userName: string, user: { displayName: string; role: Role; disabled: boolean }) =>
    call<void>("PUT", `/api/users/${encodeURIComponent(userName)}`, user),
  setUserPassword: (userName: string, password: string) => call<void>("POST", `/api/users/${encodeURIComponent(userName)}/password`, { password }),
  unlockUser: (userName: string) => call<void>("POST", `/api/users/${encodeURIComponent(userName)}/unlock`),
  deleteUser: (userName: string) => call<void>("DELETE", `/api/users/${encodeURIComponent(userName)}`),
  changePassword: (currentPassword: string, newPassword: string) => call<void>("POST", "/api/auth/password", { currentPassword, newPassword }),
  keys: () => call<AccessKeyInfo[]>("GET", "/api/keys"),
  createKey: (name: string, kind: AccessKeyKind, role: Role) => call<AccessKeyCreated>("POST", "/api/keys", { name, kind, role }),
  deleteKey: (id: string) => call<void>("DELETE", `/api/keys/${encodeURIComponent(id)}`),
  audit: (filter: AuditFilter) => {
    const query = new URLSearchParams();
    for (const [key, value] of Object.entries(filter)) if (value !== undefined) query.set(key, String(value));
    return call<AuditPage>("GET", `/api/audit?${query}`);
  },
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
