// Snimky clankov Prve kroky: hlavne okno s tabulami a stranka Linky s hodnotami TablePort.
import { expect, test } from "@playwright/test";
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
}
