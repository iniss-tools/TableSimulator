// Snimka stranky alebo jej casti do dokumentacie: <docs>/static/img/tablesimulator/<nazov>-light.png a -dark.png
// (komponent Screenshot v docs vyberie podla temy webu). Rozhranie je vzdy slovenske.
import { mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import type { Locator, Page } from "@playwright/test";

const here = dirname(fileURLToPath(import.meta.url));
const OUT = process.env.DOCSHOTS_OUT ?? resolve(here, "..", "..", "..", "iniss-tools-docs", "static", "img", "tablesimulator");

export const THEMES = ["light", "dark"] as const;

export type Theme = (typeof THEMES)[number];

/** Nastavi temu a jazyk (pred nacitanim stranky) a otvori adresu za #. */
export async function open(page: Page, hash: string, theme: Theme) {
  await page.addInitScript((value) => {
    localStorage.setItem("theme", value);
    localStorage.setItem("lang", "sk");
  }, theme);
  await page.goto(`/#${hash}`);
  await page.locator("main").waitFor();
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
