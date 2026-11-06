// Ukazkovy server INISS2 pre okno Nacitat z INISSu: subory stanice Lipovec vo windows-1250 ako ich ma server a prazdny
// IniSrv.exe (simulator cita len subory, program nespusta). Pouzivaju ho snimky dokumentacie aj test v prehliadaci.
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { cp1250 } from "./frames";

const STATION = join("2030", "01 Lipovec");

const HEADER = `<?xml version="1.0" encoding="windows-1250"?>\r\n`;

/** Tabule na linke UDP a na seriovej linke, linka zvuku bez tabul a linka, na ktorej server sam caka na spojenie. */
const COMM_LINES = `${HEADER}<CommLines>
  <line k="Lipovec_Tabule" typ="UDP_CLIENT" protocol="ITP" line="10.0.0.11:10001" />
  <line k="Lipovec_Hala" typ="SERIAL" protocol="LCD" line="COM3" />
  <line k="Lipovec_Rozhlas" typ="TCP_CLIENT" protocol="AKSIGNAL" line="10.0.0.12:2000" />
</CommLines>
`;

const TABLES = `${HEADER}<TABLES>
  <t k="Nástupiště 1" n="Nástupiště 1" lin="Lipovec_Tabule" addr="2">
    <part n="Nástupiště 1" ck="Nástupištní" t="PLATFORM" rec="1"><pos k="1" /></part>
    <part n="Informace" ck="Informace" t="INFO" rec="1" />
  </t>
  <t k="Odjezdy" n="Odjezdy hala" lin="Lipovec_Hala" addr="10">
    <part n="Odjezdy" ck="Odjezdová" t="DEPARTURE" rec="4" />
  </t>
  <t k="Odjezdy Internet" n="Lipovec" save="5400010O.xml">
    <part n="Lipovec - odjezdy" ck="Internet" t="DEPARTURE" rec="10" />
  </t>
</TABLES>
`;

const CATALOG = `${HEADER}<CATALOG>
  <t k="Internet" n="Internet" cls="INTERNET" />
  <t k="Nástupištní" n="Nástupištní" cls="LCD1">
    <div k="Cíl" inf="CilNazev" align="LEFT"><pos c1="0" c2="17" /></div>
    <div k="Odjezd" inf="CasOdjezdu" align="CENTER"><pos c1="17" c2="23" /></div>
    <div k="Směr" inf="PoNazev" align="LEFT"><pos r="1" c1="0" c2="23" /></div>
  </t>
  <t k="Informace" n="Informace" cls="LCD1">
    <div k="Text" inf="TextLine1" align="LEFT"><pos c1="0" c2="123" /></div>
  </t>
  <t k="Odjezdová" n="Odjezdová" cls="LCD1">
    <div k="Čas" inf="CasOdjezdu" align="LEFT"><pos c1="0" c2="6" /></div>
    <div k="Cíl" inf="CilNazev" align="LEFT"><pos c1="6" c2="30" /></div>
    <div k="Kolej" inf="KolejOdjezd" align="RIGHT"><pos c1="30" c2="34" /></div>
  </t>
</CATALOG>
`;

const FILES: Record<string, string> = {
  [join("Data", "Config.xml")]: `${HEADER}<INISSCFG GTNPort="1090" />\r\n`,
  [join("Data", "CommLines.xml")]: COMM_LINES,
  [join("Data", "DirList.xml")]: `${HEADER}<DL><g n="2030"><i s="5400010" d="01 Lipovec" /></g></DL>\r\n`,
  [join("Data", STATION, "SR70.TXT")]: "5400010\r\n",
  [join("Data", STATION, "Tables.xml")]: TABLES,
  [join("Data", STATION, "TCatalog.xml")]: CATALOG,
  [join("Data", STATION, "PositionsDef.xml")]: `${HEADER}<POZICE><pos k="1" n="1" tabkolej="1" tabnast="I" /></POZICE>\r\n`,
};

/** Zapise server do `folder`, vrati cestu k programu a k suboru liniek. */
export function writeIniss2Install(folder: string): { program: string; commLines: string } {
  const server = join(folder, "Server");
  for (const [name, text] of Object.entries(FILES)) {
    const file = join(server, name);
    mkdirSync(dirname(file), { recursive: true });
    writeFileSync(file, Buffer.from(cp1250(text)));
  }

  mkdirSync(join(server, "LOG"), { recursive: true });
  const program = join(server, "IniSrv.exe");
  writeFileSync(program, "");
  return { program, commLines: join(server, "Data", "CommLines.xml") };
}
