// Snimky clanku Osvetlenie a rozhlas: ustredne AK Signal servera INISS2 (AKSIGNAL so svetlami nastupist, AKSIGNALUH
// s hlasenim), rozhlas vzdialeneho INISSu s frontou a historia na casovej osi; okno scenara s krokom ustredne.
// Linky si subor zalozi sam a na konci ich odstrani - ostatne snimky ukazkovej stanice sa nemenia.
import { spawnSync } from "node:child_process";
import { connect, createServer, type Socket } from "node:net";
import { join } from "node:path";
import { expect, request, test } from "@playwright/test";
import { akSignalFrame, API_HEADERS, remoteQueueMessage } from "../e2e/helpers";
import { defaultProfile } from "../src/lib/boards";
import type { AkSignalProfileDto } from "../src/api/generated/types.gen";
import { open, shot, THEMES } from "./shoot";
import { DOCSHOTS_SERVER } from "./server";

/** Linky zariadeni: svetla (AKSIGNAL), rozhlas UH a vzdialeny INISS v podchode. */
const LINES = [
  { number: 7, tableClass: 131, name: "Svetlá nástupíšť", manufacturer: "AKSIGNAL" },
  { number: 8, tableClass: 132, name: "Rozhlas nástupíšť", manufacturer: "AKSIGNALUH" },
  { number: 9, tableClass: 128, name: "Vzdialený INISS – podchod", manufacturer: "ELEN" },
];

const light = (name: string) => ({ name, station: 5400010, beforeArrival: 10, beforeDeparture: 15, afterDeparture: 5, afterArrival: 5, minOff: 10 });

const AKSIGNAL: AkSignalProfileDto = {
  lights: [],
  audio: [],
  sensor: { mode: "sun", offsetMinutes: 0 },
  conditions: { lightsFault: false, highPriority: false, outputPowerError: false, ampSupplyMissing: false, ampCommError: false, bankNotSynced: false },
  soundSeconds: 1.5,
  syncSeconds: 2,
  silenceSeconds: 10,
};

/** Ustredne: dve nastupistia (jedno v tme so zapnutou automatikou), rozhlas UH s audio linkou. */
const DEVICES = [
  {
    line: 7,
    address: 3,
    manufacturer: "AKSIGNAL",
    name: "Nástupište 1",
    akSignal: { ...AKSIGNAL, lights: [light("Nástupište 1")], sensor: { mode: "dark", offsetMinutes: 0 } },
  },
  {
    line: 7,
    address: 4,
    manufacturer: "AKSIGNAL",
    name: "Nástupište 2",
    akSignal: { ...AKSIGNAL, lights: [light("Nástupište 2")], sensor: { mode: "light", offsetMinutes: 0 } },
  },
  {
    line: 8,
    address: 0,
    manufacturer: "AKSIGNALUH",
    name: "Rozhlas nástupíšť",
    akSignal: { ...AKSIGNAL, audio: [{ name: "Dolné Mesto – nástupištia", station: 5400010, channel: 1 }], sensor: { mode: "light", offsetMinutes: 0 } },
  },
];

/** Hlasenie UH s viacerymi zvukmi - hra pocas snimok. */
const UH_SOUNDS = [
  "SK/Gong/gong1",
  "SK/Vlak/os",
  "SK/VlakNum/3412",
  "SK/Smer/z",
  "SK/Stanice/9900100",
  "SK/Vlak/prich",
  "SK/Kolaj/2",
  ...Array.from({ length: 12 }, () => "SK/Vlak/pozor"),
];

let sockets: Socket[] = [];
let polling: ReturnType<typeof setInterval> | undefined;

/** Ukazkove 2 hodiny historie v databaze servera (docshots/device-history.ts cez bun). */
function seedHistory() {
  const db = join(import.meta.dirname, "..", "e2e", ".run", DOCSHOTS_SERVER.name, "tablesimulator.db");
  const result = spawnSync("bun", [join(import.meta.dirname, "device-history.ts"), db], { stdio: "inherit", shell: process.platform === "win32" });
  if (result.status !== 0) throw new Error(`Historia: bun skoncil s kodom ${result.status}`);
}

function free(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const probe = createServer();
    probe.once("error", () => resolve(false));
    probe.listen(port, "0.0.0.0", () => probe.close(() => resolve(true)));
  });
}

async function link(port: number) {
  const socket = connect(port, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  sockets.push(socket);
  let replies = 0;
  socket.on("data", (data: Buffer) => (replies += data.length));
  return async (frame: number[]) => {
    const before = replies;
    socket.write(Buffer.from(frame));
    for (let i = 0; i < 50 && replies === before; i++) await new Promise((r) => setTimeout(r, 20));
  };
}

const text = (value: string) => Array.from(value, (ch) => ch.charCodeAt(0));
const pause = (ms: number) => new Promise((r) => setTimeout(r, ms));

test.beforeAll(async () => {
  test.setTimeout(60_000);
  const api = await request.newContext({ baseURL: DOCSHOTS_SERVER.url, extraHTTPHeaders: API_HEADERS });
  const ports = new Map<number, number>();
  for (const l of LINES) {
    const port = (await free(47000 + l.number)) ? 47000 + l.number : 48000 + l.number;
    ports.set(l.number, port);
    const response = await api.put(`/api/lines/${l.number}`, {
      data: {
        number: l.number,
        tableClass: l.tableClass,
        endpoint: `TCP://0.0.0.0:${port}`,
        name: l.name,
        mode: "display",
        autoLearn: true,
        manufacturer: l.manufacturer,
        mirror: null,
      },
    });
    if (!response.ok()) throw new Error(`Linka ${l.number}: ${await response.text()}`);
  }
  for (const d of DEVICES) {
    const response = await api.put(`/api/boards/${d.line}/${d.address}`, {
      data: {
        line: d.line,
        address: d.address,
        manufacturer: d.manufacturer,
        name: d.name,
        profile: { ...defaultProfile(d.manufacturer), akSignal: d.akSignal },
        design: null,
        fault: { kind: "none", delayMs: 0, status: 0 },
        secondAddress: null,
      },
    });
    if (!response.ok()) throw new Error(`Ustredna ${d.line}-${d.address}: ${await response.text()}`);
  }
  await api.dispose();
  // linky zacnu pocuvat
  await pause(1500);

  // server INISS2: automatika na nastupisti 1 (v tme svieti), 2 vypnute; rozhlas UH hra hlasenie na kanali 1
  const lights = await link(ports.get(7)!);
  await lights(akSignalFrame(3, 0x22));
  await lights(akSignalFrame(4, 0x20));
  const uh = await link(ports.get(8)!);
  await uh(text("<light>"));
  await uh(text(`<aud|ch=1|${UH_SOUNDS.join("*")}>`));

  // riadiaci INISS: kratke hlasenie dohra, dlhe (ticho po nahravke) hra pocas snimok
  const remote = await link(ports.get(9)!);
  await remote(remoteQueueMessage(1, 0, "SK/R1/9900100,0,0", "Os 3412 Lipovec"));
  await remote(remoteQueueMessage(2, 1));
  await pause(2500);
  await remote(remoteQueueMessage(3, 1));
  await remote(remoteQueueMessage(4, 0));
  await remote(remoteQueueMessage(5, 0, "SK/Gong/gong1,0,0,SK/Vlak/r,0,0,SK/VlakNum/612,0,600000", "R 612 Bratislava"));
  await remote(remoteQueueMessage(6, 1));
  seedHistory();

  // server a riadiaci INISS sa pytaju dalej - ustredne nehlasia, ze sa server nepyta
  let seq = 7;
  polling = setInterval(() => {
    void (async () => {
      await lights(akSignalFrame(3, 0x20));
      await lights(akSignalFrame(4, 0x20));
      await uh(text("<light>"));
      await remote(remoteQueueMessage(seq++, 1));
    })();
  }, 2000);
  // historia sa zapisuje kazdych 5 s
  await pause(6000);
});

test.afterAll(async () => {
  clearInterval(polling);
  const api = await request.newContext({ baseURL: DOCSHOTS_SERVER.url, extraHTTPHeaders: API_HEADERS });
  for (const l of LINES) await api.delete(`/api/lines/${l.number}`);
  await api.dispose();
  sockets.forEach((s) => s.destroy());
  sockets = [];
});

for (const theme of THEMES) {
  test(`osvetlenie a rozhlas (${theme})`, async ({ page }) => {
    await open(page, "/lights", theme);
    const first = page.locator("[data-slot=card]").filter({ hasText: "Nástupište 1" }).first();
    await expect(first.getByText("Svieti", { exact: true })).toBeVisible();
    await shot(page, "osvetlenie-a-rozhlas/stranka", theme, { height: 1500 });

    const remote = page.locator("[data-slot=card]").filter({ hasText: "vzdialený INISS" });
    await expect(remote.getByText("R 612 Bratislava").first()).toBeVisible();
    await shot(page, "osvetlenie-a-rozhlas/vzdialeny-iniss", theme, { target: remote });

    const uh = page.locator("[data-slot=card]").filter({ hasText: "AK Signal UH" });
    await expect(uh.getByText("Kanál 1").first()).toBeVisible();
    await shot(page, "osvetlenie-a-rozhlas/rozhlas-uh", theme, { target: uh });

    const history = page.locator("[data-slot=card]").filter({ hasText: "Stav svetiel a hlásenia ústrední v čase" });
    await expect(history.getByText("Rozhlas vzdialeného INISSu")).toBeVisible();
    await shot(page, "osvetlenie-a-rozhlas/historia", theme, { target: history });
  });

  test(`krok scenara pre ustrednu (${theme})`, async ({ page }) => {
    await open(page, "/scenarios", theme);
    await page.getByRole("button", { name: "Nový scenár" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Názov").fill("Porucha svetiel na nástupišti 1");
    await dialog.getByRole("button", { name: "Pridať krok" }).click();
    await dialog.getByRole("combobox", { name: "Linka" }).click();
    await page.getByRole("option", { name: "7 – Svetlá nástupíšť" }).click();
    await dialog.getByRole("combobox", { name: "Na čom" }).click();
    await page.getByRole("option", { name: "3 – Nástupište 1" }).click();
    await dialog.getByRole("combobox", { name: "Čo sa stane" }).click();
    await page.getByRole("option", { name: "Stav ústredne AK Signal" }).click();
    await dialog.getByLabel("Od [s]").fill("10");
    await dialog.getByLabel("Trvá [s]").fill("300");
    await dialog.getByRole("combobox", { name: "Snímač tmy" }).click();
    await page.getByRole("option", { name: "Stále tma" }).click();
    await expect(dialog.getByText("Linka 7 · Nástupište 1 (3) – Ústredňa: porucha svetiel, stále tma")).toBeVisible();
    await shot(page, "osvetlenie-a-rozhlas/krok-scenara", theme, { target: dialog });
    await page.keyboard.press("Escape");
  });
}
