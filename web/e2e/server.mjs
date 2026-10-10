// Spusti server simulatora pre testy: cisty priecinok dat v e2e/.run/<meno>, pri "login" so zapnutym prihlasovanim,
// pri "docshots" (snimky dokumentacie) s HTTPS na porte o 1 vyssom a priecinkom pisiem ELEN z premennej DOCSHOTS_FONTS
// (stare settings.json server pri prvom starte prevedie do databazy). Server zostavi "bun run e2e:server".
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const [name, port] = process.argv.slice(2);
const here = dirname(fileURLToPath(import.meta.url));
const data = join(here, ".run", name);
rmSync(data, { recursive: true, force: true });
mkdirSync(data, { recursive: true });
if (name === "login") writeFileSync(join(data, "settings.json"), JSON.stringify({ requireLogin: true }));
if (name === "docshots") {
  // priecinok pisiem ELEN (napr. Sim\FONT programu ELEN Monitor); bez neho tabule ukazu nahradne bodove pismo
  const fonts = process.env.DOCSHOTS_FONTS;
  if (fonts && !existsSync(fonts)) throw new Error(`DOCSHOTS_FONTS: priecinok ${fonts} neexistuje.`);
  // HTTPS na vedlajsom porte - nastavenia ukazu certifikat autority simulatora
  writeFileSync(join(data, "settings.json"), JSON.stringify({ httpsPort: Number(port) + 1, ...(fonts ? { fontsFolder: fonts } : {}) }));
}

const exe = join(here, ".run", "artifacts", "bin", "TableSimulator", "debug", process.platform === "win32" ? "TableSimulator.exe" : "TableSimulator");
const server = spawn(exe, ["--no-browser", "--port", port, "--data", data], { stdio: "inherit" });
const stop = () => server.kill();
process.on("SIGINT", stop);
process.on("SIGTERM", stop);
server.on("exit", (code) => process.exit(code ?? 0));
