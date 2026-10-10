// Priecinok logov INISSu ukazkovej stanice pre virtualne tabule a archiv: XML tabul (SAVE_XML) a denne archivy TAB/REP
// v tvare, ako ich zapisuje INISS 3.39 (windows-1250, archiv po riadkoch s CR LF) - viz docs formaty-suborov/archiv.mdx
// a tutorial INISSView.
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { cp1250 } from "./frames";

const STATION = "Dolné Mesto";

const pad = (n: number) => String(n).padStart(2, "0");
const day = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const stamp = (d: Date) => `${day(d)} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
const write = (file: string, text: string) => writeFileSync(file, Buffer.from(cp1250(text)));

interface Row {
  time: string;
  type: string;
  number: string;
  destination: string;
  direction: string;
  platform: string;
  track: string;
  delay: string;
}

/** Odchody: smer s castami oddelenymi # sa na tabuli striedaju (alebo ukazu s oddelovacom). */
const DEPARTURES: Row[] = [
  { time: "10:15", type: "Os", number: "4201", destination: "Trnava", direction: "Galanta#Sereď", platform: "1", track: "1", delay: "" },
  { time: "10:32", type: "R", number: "612", destination: "Bratislava hl.st.", direction: "Nitra#Leopoldov", platform: "2", track: "3", delay: "10" },
  { time: "10:47", type: "Ex", number: "521", destination: "Lipovan", direction: "Horná Ves", platform: "1", track: "2", delay: "" },
  { time: "11:05", type: "Os", number: "4205", destination: "Senica", direction: "Kúty#Holíč", platform: "3", track: "5", delay: "" },
  { time: "11:20", type: "REX", number: "1741", destination: "Žilina", direction: "Trenčín#Púchov", platform: "2", track: "4", delay: "5" },
  { time: "11:38", type: "Os", number: "4207", destination: "Trnava", direction: "Galanta#Sereď", platform: "1", track: "1", delay: "" },
  { time: "11:52", type: "R", number: "601", destination: "Košice", direction: "Žilina#Poprad-Tatry", platform: "2", track: "3", delay: "" },
  { time: "12:10", type: "Os", number: "4209", destination: "Senica", direction: "Kúty#Holíč", platform: "3", track: "6", delay: "" },
];

const ARRIVALS: Row[] = [
  { time: "10:08", type: "Os", number: "4202", destination: "Trnava", direction: "Sereď#Galanta", platform: "1", track: "1", delay: "" },
  { time: "10:29", type: "R", number: "613", destination: "Bratislava hl.st.", direction: "Leopoldov#Nitra", platform: "2", track: "3", delay: "" },
  { time: "10:55", type: "Os", number: "4206", destination: "Senica", direction: "Holíč#Kúty", platform: "3", track: "5", delay: "15" },
  { time: "11:14", type: "Ex", number: "520", destination: "Lipovan", direction: "Horná Ves", platform: "1", track: "2", delay: "" },
  { time: "11:40", type: "REX", number: "1740", destination: "Žilina", direction: "Púchov#Trenčín", platform: "2", track: "4", delay: "" },
];

/** XML tabule ako ho zapisuje INISS: koren station, kazdy zaznam jeden row, prvky v poradi stlpcov tabule. */
function tableXml(key: string, rows: Row[], now: Date): string {
  const validTo = new Date(now.getTime() + 60 * 60_000);
  const body = rows
    .map(
      (r) =>
        `<row><time>${r.time}</time><type>${r.type}</type><number>${r.number}</number><destination>${r.destination}</destination>` +
        `<direction>${r.direction}</direction><platform>${r.platform}</platform><track>${r.track}</track><delay>${r.delay}</delay></row>`,
    )
    .join("\r\n");
  return `<?xml version="1.0" encoding="windows-1250"?>\r\n<station validto="${stamp(validTo)}" name="${STATION}" date="${stamp(now)}" n="${key}" sw="INISS v. 3.39">\r\n${body}\r\n</station>\r\n`;
}

/** Denny archiv tabul: co tabule ukazovali o vlakoch, informacny text a vlak, ktory zmizol z tabul. */
function tableArchive(d: string): string {
  const train = (c: string, t: string, v: string, groups: string) => `<T d="${d}" c="${c}" s="${STATION}" t="${t}" v="${v}" jm="" >${groups}</T>`;
  const group = (tables: string[], time: string, target: string, delay = "") =>
    `<t>${tables.map((n) => `<n>${n}</n>`).join("")}<c n="Odchod" s="${time}" /><c n="Cieľ" s="${target}" />${delay ? `<c n="Meškanie" s="${delay}" />` : `<c n="Meškanie" />`}</t>`;
  return [
    train("09:45:02", "Os", "4201", group(["Odchody hala", "Odchody web"], "10:15", "Trnava")),
    train("09:58:40", "R", "612", group(["Odchody hala", "Odchody web"], "10:32", "Bratislava hl.st.")),
    train("10:05:11", "R", "612", group(["Odchody hala", "Odchody web"], "10:32", "Bratislava hl.st.", "10")),
    `<T d="${d}" c="10:06:00" s="${STATION}"><tx>Vlak R 612 mešká asi 10 minút</tx><t lt="Odchody" r="0"><n r="0">Odchody hala</n></t></T>`,
    train("10:17:30", "Os", "4201", ""),
    train("10:20:15", "Ex", "521", group(["Odchody hala", "Odchody web"], "10:47", "Lipovan")),
    `<T d="${d}" c="10:43:00" s="${STATION}"><tx></tx><t lt="Odchody" r="0"><n r="0">Odchody hala</n></t></T>`,
    "",
  ].join("\r\n");
}

/** Denny archiv hlaseni: hlasenia (aj automatika a prerusene), vyluka s legendou a prihlasenie obsluhy. */
function reportArchive(d: string): string {
  const h = (c: string, attrs: string) => `<H d="${d}" c="${c}" ${attrs} />`;
  return [
    `; ${d.split("-").reverse().join(".")} 06:00:05 Legenda (platná od teraz):`,
    ";  V1     Obecná výluka v stanici",
    ";  V1000001 Výluka Dolné Mesto - Horná Ves",
    h("06:00:12", `u="prihlásenie" n="dispecer"`),
    h(
      "09:44:00",
      `u="hlásenie" n="Pristavenie" a="true" s="${STATION}" t="Os" v="4201" jm="" kp="1" x="Osobný vlak 4201 do stanice Trnava je pristavený na 1. koľaj." e="OK"`,
    ),
    h("10:02:30", `u="stav" n="Meškanie" s="${STATION}" t="R" v="612" jm="" kp="3" zp="10" zcp="10"`),
    h(
      "10:03:10",
      `u="hlásenie" n="Meškanie" s="${STATION}" t="R" v="612" jm="" kp="3" zp="10" zcp="10" x="Rýchlik 612 do stanice Bratislava hlavná stanica mešká asi 10 minút." e="OK"`,
    ),
    h(
      "10:14:00",
      `u="hlásenie" n="Odchod" a="true" s="${STATION}" t="Os" v="4201" jm="" kp="1" x="Osobný vlak 4201 do stanice Trnava odchádza z 1. koľaje." e="OK"`,
    ),
    h("10:25:45", `u="hlásenie" n="Príde" a="true" s="${STATION}" t="R" v="613" jm="" kp="3" e="break"`),
    h(
      "10:26:05",
      `u="hlásenie" n="Príde" s="${STATION}" t="R" v="613" jm="" kp="3" x="Rýchlik 613 zo stanice Bratislava hlavná stanica príde na 3. koľaj." e="OK"`,
    ),
    h("10:40:00", `u="úprava vlaku" n="Koľaj" s="${STATION}" t="Ex" v="521" jm="" kp="2" vyl="1000001"`),
    h("10:44:20", `u="hlásenie" n="Výluka" a="true" s="${STATION}" t="Ex" v="521" jm="" kp="2" vyl="1000001" e="OK"`),
    h("10:46:00", `u="hlásenie" n="Odchod" a="true" s="${STATION}" t="Ex" v="521" jm="" kp="2" e="OK"`),
    "",
  ].join("\r\n");
}

/** Zapise priecinok logov a vrati jeho cestu. */
export function writeInissLog(folder: string): string {
  mkdirSync(folder, { recursive: true });
  const now = new Date();
  write(join(folder, "Odchody.xml"), tableXml("Odchody web", DEPARTURES, now));
  write(join(folder, "Prichody.xml"), tableXml("Prichody web", ARRIVALS, now));
  write(join(folder, `TAB${day(now)}.XML`), tableArchive(day(now)));
  write(join(folder, `REP${day(now)}.XML`), reportArchive(day(now)));
  return folder;
}
