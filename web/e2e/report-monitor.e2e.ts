// Monitor hlaseni: priecinok logov s dnesnym archivom REP (ako ho pise INISS), zoznam hlaseni a nove hlasenie, ktore
// pribudne bez obnovenia stranky; vyber podla cisla vlaku. Nastavenie priecinkov sa na konci vrati.
import { appendFileSync, mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { expect, test } from "@playwright/test";
import { API_HEADERS, collectErrors, openPage } from "./helpers";

/** Riadok archivu hlaseni v kodovani windows-1250 - texty len ASCII, aby stacil latin1. */
const report = (time: string, name: string, train: string) =>
  `<H d="2026-10-10" c="${time}" u="hlasenie" n="${name}" s="Dolne Mesto" t="Os" v="${train}" kp="1" x="Vlak ${train}" e="OK"/>\r\n`;

test("monitor hlaseni nazivo", async ({ page }) => {
  const errors = collectErrors(page);
  const folder = mkdtempSync(join(tmpdir(), "e2e-rep-"));
  const day = new Date().toISOString().slice(0, 10);
  const file = join(folder, `REP${day}.XML`);
  appendFileSync(file, report("06:00:00", "Prichod", "7813") + report("06:01:00", "Odchod", "601"), "latin1");

  await page.goto("/#/");
  const settings = (await (await page.request.get("/api/settings")).json()) as Record<string, unknown> & { virtualFolders: string[] };
  const put = (virtualFolders: string[]) => page.request.put("/api/settings", { headers: API_HEADERS, data: { ...settings, virtualFolders } });
  expect((await put([...settings.virtualFolders, folder])).ok()).toBe(true);
  try {
    await openPage(page, "Monitor hlásení");
    await expect(page.getByText(`REP${day}.XML`)).toBeVisible();
    await expect(page.getByRole("cell", { name: "Vlak 601" })).toBeVisible();

    // INISS pripise hlasenie - pribudne do par sekund bez obnovenia stranky
    appendFileSync(file, report("06:02:00", "Meskanie", "833"), "latin1");
    await expect(page.getByRole("cell", { name: "Vlak 833" })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText("zobrazených 3 z 3 hlásení dňa")).toBeVisible();

    await page.getByLabel("Číslo vlaku").fill("78");
    await expect(page.getByText("zobrazených 1 z 3 hlásení dňa")).toBeVisible();
  } finally {
    await put(settings.virtualFolders);
    rmSync(folder, { recursive: true, force: true });
  }
  expect(errors).toEqual([]);
});
