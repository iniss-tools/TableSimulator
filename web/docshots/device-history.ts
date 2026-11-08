// Ukazkove 2 hodiny historie ustredni AK Signal a rozhlasu vzdialeneho INISSu pre snimku casovej osi - server ju zapisuje
// az za chodu, cisty server by nemal co ukazat. Spusta ho osvetlenie-a-rozhlas.shots.ts cez bun (bun:sqlite):
//   bun docshots/device-history.ts <tablesimulator.db>
// Stavy svetla su bity LightFlags (automatika 1, rucne 2, tma 4, svieti 8, porucha 16), casy v ms od 1970 UTC.
import { Database } from "bun:sqlite";

const path = process.argv[2];
if (!path) throw new Error("Chyba cesta k databaze servera.");

const AUTO = 1;
const MANUAL = 2;
const DARK = 4;
const ON = 8;
const FAULT = 16;

const now = Date.now();
const at = (minutes: number) => now - minutes * 60_000;

/** Stavy svetla: [linka, adresa, minut pred terajskom, bity]. */
const LIGHTS: [number, number, number, number][] = [
  // nastupiste 1: vecer v tme, server zapina automatiku podla vlakov, chvilu porucha svetiel
  [7, 3, 125, DARK],
  [7, 3, 112, AUTO | DARK | ON],
  [7, 3, 98, DARK],
  [7, 3, 81, AUTO | DARK | ON],
  [7, 3, 72, DARK],
  [7, 3, 55, DARK | FAULT],
  [7, 3, 44, DARK],
  [7, 3, 30, AUTO | DARK | ON],
  [7, 3, 18, DARK],
  [7, 3, 6, AUTO | DARK | ON],
  // nastupiste 2: cez den, raz rucne svietenie pri udrzbe
  [7, 4, 125, 0],
  [7, 4, 64, MANUAL | ON],
  [7, 4, 52, 0],
  // rozhlas UH svetlo neriadi
  [8, 0, 125, 0],
];

/** Hlasenia: [linka, adresa, kanal, zaciatok (min pred), trvanie (s), vysledok, zvuky, titulok]. */
const ANNOUNCEMENTS: [number, number, number, number, number, string, string, string | null][] = [
  [8, 0, 1, 110, 21, "completed", "SK/Gong/gong1*SK/Vlak/os*SK/VlakNum/3412*SK/Vlak/prich", null],
  [8, 0, 1, 82, 24, "completed", "SK/Gong/gong1*SK/Vlak/r*SK/VlakNum/612*SK/Vlak/odch", null],
  [8, 0, 2, 61, 9, "cancelled", "SK/Gong/gong1*SK/Vlak/mesk", null],
  [8, 0, 1, 31, 22, "completed", "SK/Gong/gong1*SK/Vlak/os*SK/VlakNum/3414*SK/Vlak/prich", null],
  [9, 0, 0, 105, 18, "completed", "SK/Gong/gong1*SK/Vlak/os*SK/VlakNum/3412", "Os 3412 Lipovec"],
  [9, 0, 0, 76, 20, "completed", "SK/Gong/gong1*SK/Vlak/r*SK/VlakNum/612", "R 612 Bratislava"],
  [9, 0, 0, 47, 7, "cancelled", "SK/Gong/gong1*SK/Vlak/ex*SK/VlakNum/521", "Ex 521 Lipovan"],
  [9, 0, 0, 22, 19, "completed", "SK/Gong/gong1*SK/Vlak/os*SK/VlakNum/3414", "Os 3414 Lipovec"],
];

const db = new Database(path);
const light = db.prepare(`INSERT INTO DeviceLightStates (Line, Address, TimeMs, Flags) VALUES (?, ?, ?, ?)`);
const announcement = db.prepare(
  `INSERT INTO DeviceAnnouncements (Line, Address, Channel, StartMs, EndMs, Result, Sounds, Value, Title, Queue, Circuit)
   VALUES (?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, NULL)`,
);
db.transaction(() => {
  for (const [line, address, minutes, flags] of LIGHTS) light.run(line, address, at(minutes), flags);
  for (const [line, address, channel, minutes, seconds, result, sounds, title] of ANNOUNCEMENTS)
    announcement.run(line, address, channel, at(minutes), at(minutes) + seconds * 1000, result, sounds, title, line === 9 ? "" : null);
})();
db.close();
