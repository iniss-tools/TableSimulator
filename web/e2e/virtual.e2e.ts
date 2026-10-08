// Virtualne tabule: zalozenie tabule zo suboru XML a kiosk, ktory ohlasi chybajuci subor.
import { expect, test } from "@playwright/test";

test("virtualna tabula bez suboru XML", async ({ page }) => {
  await page.goto("/#/virtual");
  await page.getByRole("button", { name: "Pridať tabuľu" }).first().click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Číslo").fill("77");
  await dialog.getByLabel("Súbor XML").fill("E2ETAB");
  await dialog.getByLabel("Názov", { exact: true }).fill("Odchody E2E");
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole("cell", { name: "Odchody E2E" })).toBeVisible();

  await page.goto("/#/virtual/77");
  await expect(page.getByText("Súbor E2ETAB.xml sa v sledovaných priečinkoch nenašiel.")).toBeVisible();
});
