// Zaloha a obnova celeho simulatora: stiahnutie zalohy (ZIP), jej nahratie na stranke Nastavenia - obnova sa pripravi
// na najblizsi start servera - a zrusenie pripravenej obnovy.
import { expect, test } from "@playwright/test";
import { collectErrors, openPage } from "./helpers";

test("zaloha a obnova simulatora", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  const backup = await page.request.get("/api/backup/file");
  expect(backup.ok()).toBe(true);
  expect(backup.headers()["content-type"]).toContain("application/zip");
  const zip = await backup.body();

  await openPage(page, "Nastavenia");
  await page.getByRole("button", { name: "Záloha a obnova" }).click();
  await expect(page.getByRole("link", { name: "Stiahnuť zálohu" })).toHaveAttribute("href", "/api/backup/file?recordings=false");

  await page.locator('input[type=file][accept=".zip"]').setInputFiles({ name: "zaloha.zip", mimeType: "application/zip", buffer: zip });
  await page.getByRole("alertdialog").getByRole("button", { name: "Obnoviť zo zálohy…" }).click();
  await expect(page.getByText("Pripravená obnova zálohy")).toBeVisible();
  await expect(page.getByText("Použije sa, keď server zastavíte a znova spustíte.")).toBeVisible();

  await page.getByRole("button", { name: "Zrušiť obnovu" }).click();
  await expect(page.getByText("Pripravená obnova zálohy")).toBeHidden();
  expect(errors).toEqual([]);
});
