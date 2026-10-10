// Ukazkova stanica pre snimky dokumentacie (fiktivne Dolne Mesto ako v dokumentacii GVDEditora): linky a tabule cez
// API ako pri presmerovani z GVDEditora a obsah tabul ako od INISSu. Spojenia ostanu otvorene az do konca behu - linky
// tak ukazuju pripojeny INISS, nie vypadok. Navyse dva zaznamy (pred a po zmene grafikonu), chybne spravy v logu
// a hodina statistiky.
import { spawnSync } from "node:child_process";
import { connect, createServer, type Socket } from "node:net";
import { join } from "node:path";
import { request } from "@playwright/test";
import { elenFrame, elenRow, API_HEADERS } from "../e2e/helpers";
import type {
  FaultScenarioRequest,
  FlapProfileDto,
  SetBoardRequest,
  SettingsDto,
  SetVirtualBoardRequest,
  StateDto,
  StationBoardDto,
} from "../src/api/generated/types.gen";
import { apelFrame, cp1250, elsvoFrame, fersRow, lcd1Row } from "./frames";
import { writeInissLog } from "./iniss-log";
import { DOCSHOTS_SERVER } from "./server";

/** Linky ukazkovej stanice: dve ELEN, Elektrocas LCD1, listova FERS, APEL a spinanie zosilnovacov ELSVO. */
const LINES = [
  { number: 1, tableClass: 4, name: "Hala" },
  { number: 2, tableClass: 4, name: "Nástupištia" },
  { number: 3, tableClass: 5, name: "Podchod" },
  { number: 4, tableClass: 3, name: "Listová tabuľa" },
  { number: 5, tableClass: 10, name: "Vestibul" },
  { number: 6, tableClass: 129, name: "Rozhlas" },
];

/** Port je volny (nepocuva na nom napr. simulator spusteny na tomto pocitaci). */
function free(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const probe = createServer();
    probe.once("error", () => resolve(false));
    probe.listen(port, "0.0.0.0", () => probe.close(() => resolve(true)));
  });
}

/** Port linky: predvoleny 47000 + cislo ako v dokumentacii, ak je obsadeny, 48000 + cislo. */
async function portOf(line: number): Promise<number> {
  if (await free(47000 + line)) return 47000 + line;
  console.warn(`Port ${47000 + line} je obsadeny - linka ${line} pocuva na ${48000 + line} (snimky ukazu iny port).`);
  return 48000 + line;
}

/** Hodiny na listoch: 0-9 cislicou, 10-23 pismenom a-n (ako v TabTab stanic). */
const HOURS = Object.fromEntries(Array.from({ length: 24 }, (_, h) => [h < 10 ? String(h) : String.fromCharCode(97 + h - 10), String(h)]));

/** Listova tabula FERS: ciel, druh, hodiny, desiatky a jednotky minut, nastupiste (ako zo zoznamov TabTab stanice). */
const FLAPS: FlapProfileDto = {
  linesPerRecord: 1,
  modules: ["Ciel", "Druh", "Hodiny", "Minuty", "Minuty", "Nastupiste"].map((list, position) => ({ line: 0, position, span: 1, list })),
  lists: {
    Ciel: { "0": "", A: "TRNAVA", B: "*BRATISLAVA", C: "SENICA", D: "LIPOVAN" },
    Druh: { "0": "", O: "Os", R: "*R", E: "*Ex", B: "BUS" },
    Hodiny: HOURS,
    Minuty: Object.fromEntries(Array.from({ length: 10 }, (_, m) => [String(m), String(m)])),
    Nastupiste: { "0": "", "1": "1", "2": "2", "3": "3" },
  },
  // farbu listu data stanice nemaju - nastavuje sa v simulatore (autobus na zltom liste)
  colors: { Druh: { B: "yellow" } },
};

interface DemoBoard {
  line: number;
  address: number;
  manufacturer: string;
  name: string;
  flaps?: FlapProfileDto;
  /** Spravy, ktore tabuli posle INISS. */
  frames: number[][];
}

const elenRows = (address: number, rows: string[]) => rows.map((text, i) => elenRow(address, rows.length - i, text)); // ELEN odspodu

const BOARDS: DemoBoard[] = [
  {
    line: 1,
    address: 1,
    manufacturer: "ELEN",
    name: "Odchody – hala",
    frames: elenRows(1, [
      "Os 4201  Trnava       10:15  1",
      "R  612   Bratislava   10:32  2",
      "Ex 521   Lipovan      10:47  1",
      "Os 4205  Senica       11:05  3",
    ]),
  },
  {
    line: 1,
    address: 2,
    manufacturer: "ELEN",
    name: "Príchody – hala",
    frames: elenRows(2, ["Os 4202  Trnava       10:08  1", "R  613   Bratislava   10:29  2", "Os 4206  Senica       10:55  3"]),
  },
  { line: 2, address: 3, manufacturer: "ELEN", name: "Nástupište 1", frames: elenRows(3, ["Os 4201  Trnava  10:15", "Galanta, Sered, Trnava"]) },
  { line: 2, address: 4, manufacturer: "ELEN", name: "Nástupište 2", frames: elenRows(4, ["R  612   Bratislava  10:32", "Nitra, Leopoldov, Trnava"]) },
  {
    line: 3,
    address: 10,
    manufacturer: "LCD1",
    name: "Podchod – odchody",
    frames: [
      lcd1Row(10, 0, "Os 4201  Trnava            10:15   1"),
      lcd1Row(10, 1, "R  612   Bratislava hl.st. 10:32   2"),
      lcd1Row(10, 2, "Ex 521   Lipovan           10:47   1"),
    ],
  },
  {
    line: 4,
    address: 20,
    manufacturer: "FERS",
    name: "Odchody – listová",
    flaps: FLAPS,
    frames: [fersRow(20, 0, "AOa151"), fersRow(20, 1, "BRa322"), fersRow(20, 2, "DEa471"), fersRow(20, 3, "CBb050")],
  },
  {
    line: 5,
    address: 0x81,
    manufacturer: "APEL_AN",
    name: "Vestibul – odchody",
    frames: [
      apelFrame(0x81, [
        [0, cp1250("Os  4201  Trnava            10:15  1")],
        [1, cp1250("R   612   Bratislava hl.st. 10:32  2")],
        [2, cp1250("Ex  521   Lipovan           10:47  1")],
      ]),
    ],
  },
  { line: 6, address: 5, manufacturer: "ELSVO", name: "Okruh Hala", frames: [elsvoFrame(5, 0x1f), elsvoFrame(5, 0x80)] },
  { line: 6, address: 6, manufacturer: "ELSVO", name: "Okruh Nástupištia", frames: [elsvoFrame(6, 0x80)] },
];

/** Ukazkovy scenar poruch (nespusteny). */
const SCENARIO: FaultScenarioRequest = {
  name: "Výpadky na nástupištiach",
  loop: true,
  cycleMs: 60_000,
  steps: [
    { atMs: 5_000, durationMs: 20_000, line: 2, address: 3, action: "fault", fault: { kind: "noReply", delayMs: 0, status: 0 } },
    { atMs: 15_000, durationMs: 10_000, line: 2, address: 4, action: "powerLoss", fault: null },
    { atMs: 35_000, durationMs: 15_000, line: 2, address: null, action: "lineOutage", fault: null },
  ],
};

/** Spojenie linky ako INISS: spravy posiela po jednej (dalsiu az po odpovedi tabule), spojenie ostava otvorene. */
interface InissLink {
  socket: Socket;
  send(frames: number[][]): Promise<void>;
}

async function connectLine(port: number): Promise<InissLink> {
  const socket = connect(port, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  let replies = 0;
  socket.on("data", (data: Buffer) => (replies += data.length));
  return {
    socket,
    async send(frames) {
      for (const frame of frames) {
        const before = replies;
        socket.write(Buffer.from(frame));
        for (let i = 0; i < 50 && replies === before; i++) await new Promise((r) => setTimeout(r, 20));
      }
    },
  };
}

/** Zmena grafikonu medzi zaznamami A a B: vlak R 612 ide z inej kolaje (tabula v hale a v podchode). */
const CHANGED: { line: number; frames: number[][] }[] = [
  { line: 1, frames: [elenRow(1, 3, "R  612   Bratislava   10:32  3")] },
  { line: 3, frames: [lcd1Row(10, 1, "R  612   Bratislava hl.st. 10:32   3")] },
];

/** Chybne spravy pre log komunikacie: sum, zly sucet a 04 v datach (pismo 04 namiesto 44); tabula ich odmietne. */
function faultyFrames(): number[][] {
  const bad = elenRow(2, 1, "Os 4202  Trnava       10:08  1");
  bad[bad.length - 1] ^= 0x01;
  const eot = elenFrame(2, 1, [0x80 | 2, 0x10, 0x80, 0x1b, 0x04, ...Array.from("R  613", (ch) => ch.charCodeAt(0))]);
  return [[0x55, 0xaa, 0x55], bad, eot];
}

/** Priecinok dat servera (e2e/server.mjs). */
const DATA = join(import.meta.dirname, "..", "e2e", ".run", DOCSHOTS_SERVER.name);

/** Virtualne tabule z XML ukazkoveho priecinka logov INISSu: hala cela, podchod na dvoch obrazovkach po 4 zaznamoch. */
const VIRTUAL: (SetVirtualBoardRequest & { id: number })[] = [
  { id: 1, name: "Odchody – hala", description: "Monitor nad pokladňami", source: "Odchody", rowCount: null, rowStart: 1, layout: "odchody" },
  { id: 2, name: "Príchody – hala", description: "Monitor pri vchode", source: "Prichody", rowCount: null, rowStart: 1, layout: "prichody" },
  { id: 3, name: "Podchod – ľavý monitor", description: "", source: "Odchody", rowCount: 4, rowStart: 1, layout: "cierna", separator: ", " },
  { id: 4, name: "Podchod – pravý monitor", description: "", source: "Odchody", rowCount: 4, rowStart: 5, layout: "cierna", separator: ", " },
].map((v) => ({ separator: "", template: "", parameter: "", ...v }));

/** Ukazkova hodina statistiky v databaze servera (docshots/statistics.ts cez bun). */
function seedStatistics() {
  const db = join(DATA, "tablesimulator.db");
  const result = spawnSync("bun", [join(import.meta.dirname, "statistics.ts"), db], { stdio: "inherit", shell: process.platform === "win32" });
  if (result.status !== 0) throw new Error(`Statistika: bun skoncil s kodom ${result.status}`);
}

export default async function demo() {
  const api = await request.newContext({ baseURL: DOCSHOTS_SERVER.url, extraHTTPHeaders: API_HEADERS });
  const lines = await Promise.all(LINES.map(async (l) => ({ ...l, port: await portOf(l.number) })));
  const station = await api.post("/api/station", {
    data: {
      lines,
      boards: BOARDS.map((b): StationBoardDto => ({
        line: b.line,
        address: b.address,
        manufacturer: b.manufacturer,
        name: b.name,
        flaps: b.flaps ?? null,
        flapRecords: 4,
      })),
      logFolder: null,
      xmlTables: [],
    },
  });
  if (!station.ok()) throw new Error(`Ukazkova stanica: ${station.status()} ${await station.text()}`);

  // pismo ELEN tabuliam ELEN, ak server pozna priecinok pisiem (inak nahradne bodove pismo s upozornenim na karte)
  const fonts = (await (await api.get("/api/fonts")).json()) as string[];
  const font = fonts.find((f) => f.toLowerCase() === "10_1250_rwp_s05.bin") ?? fonts[0];
  if (font) {
    const state = (await (await api.get("/api/state")).json()) as StateDto;
    for (const b of state.boards.filter((x) => x.manufacturer === "ELEN")) {
      const response = await api.put(`/api/boards/${b.line}/${b.address}`, {
        data: {
          line: b.line,
          address: b.address,
          manufacturer: b.manufacturer,
          name: b.name,
          profile: { ...b.profile, fontFile: font },
          design: b.design,
          fault: b.fault,
          secondAddress: b.secondAddress,
        } satisfies SetBoardRequest,
      });
      if (!response.ok()) throw new Error(`Pismo tabule ${b.id}: ${await response.text()}`);
    }
  } else console.warn("Priecinok pisiem ELEN nie je (DOCSHOTS_FONTS) - tabule ukazu nahradne pismo.");

  const scenario = await api.post("/api/scenarios", { data: SCENARIO });
  if (!scenario.ok()) throw new Error(`Scenar: ${await scenario.text()}`);

  // priecinok logov INISSu s XML tabul a dennymi archivmi, virtualne tabule
  const folder = writeInissLog(join(DATA, "iniss-log"));
  const settings = (await (await api.get("/api/settings")).json()) as SettingsDto;
  const saved = await api.put("/api/settings", { data: { ...settings, virtualFolders: [folder] } });
  if (!saved.ok()) throw new Error(`Nastavenie: ${await saved.text()}`);
  for (const { id, ...board } of VIRTUAL) {
    const response = await api.put(`/api/virtual/${id}`, { data: board });
    if (!response.ok()) throw new Error(`Virtualna tabula ${id}: ${await response.text()}`);
  }
  await api.dispose();

  const recorder = await request.newContext({ baseURL: DOCSHOTS_SERVER.url, extraHTTPHeaders: API_HEADERS });
  const record = async (note: string, run: () => Promise<void>) => {
    const started = await recorder.post("/api/recordings/start", { data: { note } });
    if (!started.ok()) throw new Error(`Zaznam: ${await started.text()}`);
    await run();
    await new Promise((r) => setTimeout(r, 1500));
    await recorder.post("/api/recordings/stop");
  };
  const framesOf = (line: number) => BOARDS.filter((b) => b.line === line).flatMap((b) => b.frames);

  // zaznam A = stanica, ako ju INISS poslal; zaznam B = to iste po zmene grafikonu (porovnanie zaznamov)
  const links = new Map<number, InissLink>();
  // linky po chvilkach, aby casova os zaznamu mala co ukazat
  const pause = () => new Promise((r) => setTimeout(r, 1500));
  await record("Pred zmenou grafikonu", async () => {
    for (const line of lines) {
      const iniss = await connectLine(line.port);
      links.set(line.number, iniss);
      await iniss.send(framesOf(line.number));
      await pause();
    }
  });
  await record("Po zmene grafikonu", async () => {
    // zmena hned za obsahom linky - do 1 s je to jeden stav tabule, porovnanie ho ukaze ako zmeneny
    for (const line of lines) {
      await links.get(line.number)!.send([...framesOf(line.number), ...CHANGED.filter((c) => c.line === line.number).flatMap((c) => c.frames)]);
      await pause();
    }
  });
  // tabule spat ako v zazname A, potom chybne spravy (navrchu logu komunikacie)
  for (const change of CHANGED) await links.get(change.line)!.send(BOARDS.filter((b) => b.line === change.line).flatMap((b) => b.frames));
  await links.get(1)!.send(faultyFrames());
  await recorder.dispose();

  seedStatistics();
  // linky chvilu potrebuju na stav "pripojeny", listy na dotocenie a tabule na prekreslenie
  await new Promise((r) => setTimeout(r, 4000));
  return () => links.forEach((i) => i.socket.destroy());
}
