// Ukazkova hodina statistiky komunikacie pre snimku dokumentacie - server statistiku zapisuje az po skonceni minuty,
// cisty server by teda nemal co ukazat. Spusta ho demo.ts cez bun (bun:sqlite) s cestou k databaze servera:
//   bun docshots/statistics.ts <tablesimulator.db>
// Zapise 60 minut pred aktualnou: prevadzku liniek a tabul ukazkovej stanice, chyby, poruchu zo scenara a vypadok
// linky 2 (INISS sa odpojil a o 3 minuty sa vratil).
import { Database } from "bun:sqlite";

const MINUTES = 60;
const path = process.argv[2];
if (!path) throw new Error("Chyba cesta k databaze servera.");

/** Linky ukazkovej stanice: adresy tabul, sprav za minutu (zaklad) a bajtov na spravu. */
const LINES = [
  { line: 1, boards: [1, 2], frames: 3, bytes: 42 },
  { line: 2, boards: [3, 4], frames: 2, bytes: 38 },
  { line: 3, boards: [10], frames: 2, bytes: 90 },
  { line: 4, boards: [20], frames: 1, bytes: 12 },
  { line: 5, boards: [0x81], frames: 1, bytes: 120 },
  { line: 6, boards: [5, 6], frames: 1, bytes: 5 },
];

/** Pseudonahodne cisla s pevnym zaciatkom - kazdy beh da rovnaky graf. */
let seed = 7;
const random = () => (seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648;

const now = Math.floor(Date.now() / 60000);
const first = now - MINUTES;
/** Vypadok linky 2: minuty od zaciatku obdobia. */
const LOST = [34, 37];
/** Porucha "neodpoveda" tabule 3 zo scenara. */
const FAULT = [15, 20];

const db = new Database(path);
const line = db.prepare(
  `INSERT OR REPLACE INTO LineTraffic (Line, Minute, Ok, BadChecksum, NoBoard, Noise, Other, Replies, Faults, BytesIn, BytesOut, Connections, Disconnections)
   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
);
const board = db.prepare(
  `INSERT OR REPLACE INTO BoardTraffic (Line, Address, Minute, Frames, Acks, Naks, Unanswered, MaxGapMs) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
);
const status = db.prepare(`INSERT INTO LineStatusChanges (Line, TimeMs, Status, Detail) VALUES (?, ?, ?, ?)`);

db.transaction(() => {
  for (const l of LINES) status.run(l.line, (first - 5) * 60000, "connected", "127.0.0.1");
  status.run(2, (first + LOST[0]) * 60000 + 21000, "lost", null);
  status.run(2, (first + LOST[1]) * 60000 + 8000, "connected", "127.0.0.1");

  for (let i = 0; i < MINUTES; i++) {
    const minute = first + i;
    // INISS posiela najma pri zmene obsahu - kazdych 5 minut prichod alebo odchod vlaku
    const burst = i % 5 === 0 ? 3 : 1;
    for (const l of LINES) {
      const lost = l.line === 2 && i > LOST[0] && i < LOST[1];
      if (lost) continue;
      let ok = 0;
      let faults = 0;
      for (const address of l.boards) {
        const frames = Math.max(1, Math.round(l.frames * burst * (0.6 + random() * 0.8)));
        const faulty = l.line === 2 && address === 3 && i >= FAULT[0] && i < FAULT[1];
        const naks = l.line === 1 && address === 2 && (i === 12 || i === 47) ? 1 : 0;
        ok += frames;
        if (faulty) faults += frames;
        board.run(l.line, address, minute, frames, faulty ? 0 : frames - naks, naks, faulty ? frames : 0, Math.round((60 / frames) * 1000 * (1 + random())));
      }
      const bad = l.line === 1 && (i === 12 || i === 47) ? 1 : 0;
      const noise = l.line === 3 && i === 28 ? 2 : 0;
      const reconnect = l.line === 2 && i === LOST[1];
      line.run(
        l.line,
        minute,
        ok - bad,
        bad,
        0,
        noise,
        0,
        ok - faults,
        faults,
        ok * l.bytes,
        (ok - faults) * 1,
        reconnect ? 1 : 0,
        l.line === 2 && i === LOST[0] ? 1 : 0,
      );
    }
  }
})();
db.close();
