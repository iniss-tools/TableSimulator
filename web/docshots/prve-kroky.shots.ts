// Snimky clankov Prve kroky: hlavne okno s tabulami a stranka Linky s hodnotami TablePort.
import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { expect, test } from "@playwright/test";
import { API_HEADERS } from "../e2e/helpers";
import type { SettingsDto } from "../src/api/generated/types.gen";
import { writeIniss2Install } from "./iniss2-install";
import { mask, open, shot, THEMES } from "./shoot";

for (const theme of THEMES) {
  test(`rozhranie (${theme})`, async ({ page }) => {
    await open(page, "/", theme);
    await expect(page.getByText("Odchody – hala")).toBeVisible();
    await shot(page, "rozhranie/hlavne-okno", theme);
  });

  test(`pripojenie-iniss (${theme})`, async ({ page }) => {
    await open(page, "/lines", theme);
    await expect(page.getByRole("row", { name: /Hala/ })).toBeVisible();
    await shot(page, "pripojenie-iniss/linky", theme);
  });

  test(`nacitanie-stanice (${theme})`, async ({ page }) => {
    await open(page, "/lines", theme);
    await page.getByRole("button", { name: "Načítať z INISSu" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("v simulátore TCP 47001")).toBeVisible();
    await mask(page);
    await shot(page, "nacitanie-stanice/nacitat-z-inissu", theme, { target: dialog, height: 1400 });
  });

  test(`nacitanie-stanice-iniss2 (${theme})`, async ({ page }) => {
    await open(page, "/lines", theme);
    const settings = (await (await page.request.get("/api/settings")).json()) as SettingsDto;
    const folder = mkdtempSync(join(tmpdir(), "docshots-iniss2-"));
    const save = (inissProgram: string) => page.request.put("/api/settings", { headers: API_HEADERS, data: { ...settings, inissProgram } });
    await save(writeIniss2Install(folder).program);
    try {
      await page.getByRole("button", { name: "Načítať z INISSu" }).click();
      const dialog = page.getByRole("dialog");
      await expect(dialog.getByText("v simulátore UDP 47001")).toBeVisible();
      // docasna cesta je v kazdom behu ina
      await page.evaluate((path) => {
        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        for (let node = walker.nextNode(); node; node = walker.nextNode())
          if (node.textContent?.includes(path)) node.textContent = node.textContent.replace(path, String.raw`C:\INISS2`);
      }, folder);
      await mask(page);
      await shot(page, "nacitanie-stanice/nacitat-z-iniss2", theme, { target: dialog, height: 1400 });
    } finally {
      await save(settings.inissProgram);
      rmSync(folder, { recursive: true, force: true });
    }
  });
}
