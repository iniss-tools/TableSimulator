// Ukazkova stanica pre snimky dokumentacie (fiktivne Dolne Mesto ako v dokumentacii GVDEditora): linky a tabule cez
// API ako pri presmerovani z GVDEditora a obsah tabul ako od INISSu. Spojenia ostanu otvorene az do konca behu - linky
// tak ukazuju pripojeny INISS, nie vypadok.
import { connect, createServer, type Socket } from "node:net";
import { request } from "@playwright/test";
import { elenRow, API_HEADERS } from "../e2e/helpers";
import type { FaultScenarioRequest, FlapProfileDto, SetBoardRequest, StateDto, StationBoardDto } from "../src/api/generated/types.gen";
import { apelFrame, cp1250, elsvoFrame, fersRow, lcd1Row } from "./frames";
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

/** Posle spravy na linku ako INISS (dalsia az po odpovedi tabule) a spojenie necha otvorene. */
async function feed(port: number, frames: number[][]): Promise<Socket> {
  const socket = connect(port, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  let replies = 0;
  socket.on("data", (data: Buffer) => (replies += data.length));
  for (const frame of frames) {
    const before = replies;
    socket.write(Buffer.from(frame));
    for (let i = 0; i < 50 && replies === before; i++) await new Promise((r) => setTimeout(r, 20));
  }
  return socket;
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
  await api.dispose();

  const sockets: Socket[] = [];
  for (const line of lines)
    sockets.push(
      await feed(
        line.port,
        BOARDS.filter((b) => b.line === line.number).flatMap((b) => b.frames),
      ),
    );
  // linky chvilu potrebuju na stav "pripojeny", listy na dotocenie a tabule na prekreslenie
  await new Promise((r) => setTimeout(r, 4000));
  return () => sockets.forEach((s) => s.destroy());
}
