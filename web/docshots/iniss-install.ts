// Ukazkova instalacia INISSu pre okno Nacitat z INISSu: data stanice Dolne Mesto z docshots/iniss/DATA (vygenerovane
// zapisovacmi ToolsCore.Iniss - grafikon, TPhysic, TKatalog), INISS.INI so sekciami Driver a prazdny INISS.exe
// (simulator cita len subory, program nespusta).
import { cpSync, mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { cp1250 } from "./frames";

/**
 * Linky stanice: ELEN v hale a na nastupistiach, Elektrocas LCD1 v podchode, APEL vo vestibule a tercikova Adon/BUSE,
 * ktoru simulator nepozna. Tabule s cislom linky bez sekcie Driver ostanu bez linky.
 */
const DRIVERS = [
  ["Driver", 4, "COM1"],
  ["Driver0", 4, "COM2"],
  ["Driver1", 5, "COM3"],
  ["Driver2", 10, "COM5"],
  ["Driver3", 0, "COM8"],
] as const;

/** Zapise instalaciu do `folder` a vrati cestu k programu INISSu. */
export function writeInissInstall(folder: string, logFolder: string): string {
  mkdirSync(folder, { recursive: true });
  cpSync(join(import.meta.dirname, "iniss", "DATA"), join(folder, "DATA"), { recursive: true });
  const ini = [
    ...DRIVERS.flatMap(([section, tableClass, port]) => [`[${section}]`, `TableClass=${tableClass}`, `TablePort=${port}`]),
    "[PathNames]",
    `LogPath=${logFolder}\\`,
    "",
  ].join("\r\n");
  writeFileSync(join(folder, "INISS.INI"), Buffer.from(cp1250(ini)));
  const program = join(folder, "INISS.exe");
  writeFileSync(program, "");
  return program;
}
