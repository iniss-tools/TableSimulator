// Snimka stranky alebo jej casti do dokumentacie: <docs>/static/img/tablesimulator/<nazov>-light.png a -dark.png
// (komponent Screenshot v docs vyberie podla temy webu). Rozhranie je vzdy slovenske.
import { mkdirSync } from "node:fs";
import { hostname, networkInterfaces } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import type { Locator, Page } from "@playwright/test";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = process.env.DOCSHOTS_OUT ?? resolve(here, "..", "..", "..", "iniss-tools-docs", "static", "img", "tablesimulator");

export const THEMES = ["light", "dark"] as const;

export type Theme = (typeof THEMES)[number];

/** Nastavi temu a jazyk (pred nacitanim stranky) a otvori adresu za #; kiosk (bez ramu) caka na `ready` namiesto main. */
export async function open(page: Page, hash: string, theme: Theme, ready = "main") {
  await page.addInitScript((value) => {
    localStorage.setItem("theme", value);
    localStorage.setItem("lang", "sk");
  }, theme);
  await page.goto(`/#${hash}`);
  await page.locator(ready).first().waitFor();
}

interface ShotOptions {
  /** Len cast stranky (napr. dialog); inak cele okno. */
  target?: Locator;
  /** Co sa ma nacitat pred snimkou. */
  ready?: Locator;
  /** Vyssie okno (px) - stranka s mnohymi tabulami; menu ostane na celu vysku. */
  height?: number;
}

/** Ulozi snimku `nazov` (cesta pod tablesimulator bez pripony) v danej teme. */
export async function shot(page: Page, name: string, theme: Theme, options: ShotOptions = {}) {
  if (options.ready) await options.ready.waitFor();
  // animacie a kurzor nech nie su na snimke; nacitane pisma
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(300);
  const file = join(OUT, `${name}-${theme}.png`);
  mkdirSync(dirname(file), { recursive: true });
  const opts = { path: file, animations: "disabled" as const, caret: "hide" as const };
  const size = page.viewportSize();
  if (options.height && size) {
    await page.setViewportSize({ width: size.width, height: options.height });
    await page.waitForTimeout(300);
  }
  if (options.target) await options.target.screenshot(opts);
  else await page.screenshot(opts);
  if (options.height && size) await page.setViewportSize(size);
}

/** Priecinok dat ukazkoveho servera (e2e/server.mjs). */
const DATA = resolve(here, "..", "e2e", ".run", "docshots");

/**
 * Cesty, meno a adresy pocitaca, na ktorom harness bezi, nahradene ukazkovymi ako na stanici - nemaju byt
 * v dokumentacii. Poradie je dolezite (priecinok logov je v priecinku dat).
 */
const MASKS: [string, string][] = [
  [join(DATA, "iniss-log"), String.raw`C:\INISS\LOG`],
  [join(DATA, "iniss"), String.raw`C:\INISS`],
  [DATA, String.raw`C:\ProgramData\TableSimulator`],
  ...(process.env.DOCSHOTS_FONTS ? [[process.env.DOCSHOTS_FONTS, String.raw`C:\ELEN\Monitor\Sim\FONT`] as [string, string]] : []),
  [hostname(), "STANICA-PC"],
  ...Object.values(networkInterfaces())
    .flat()
    .filter((a) => a && a.family === "IPv4" && !a.internal)
    // prva adresa ako v ostatnych snimkach (okno kluca), dalsie rozne, aby zoznam adries vyzeral ako na stanici
    .map((a, i): [string, string] => [a!.address, i === 0 ? "192.168.1.20" : `10.0.${i}.20`]),
];

/** Nahradi na stranke texty a hodnoty poli podla MASKS (len pre snimku - nic sa neuklada). */
export async function mask(page: Page) {
  await page.evaluate((masks) => {
    // bez ohladu na velkost pismen (meno pocitaca, pismeno disku)
    const replace = (text: string, from: string, to: string) => {
      let result = text;
      for (let i = result.toLowerCase().indexOf(from.toLowerCase()); i >= 0; i = result.toLowerCase().indexOf(from.toLowerCase(), i + to.length))
        result = result.slice(0, i) + to + result.slice(i + from.length);
      return result;
    };
    const apply = (text: string) => masks.reduce((t, [from, to]) => replace(t, from, to), text);
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) if (node.textContent) node.textContent = apply(node.textContent);
    for (const input of document.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>("input, textarea")) input.value = apply(input.value);
  }, MASKS);
}
