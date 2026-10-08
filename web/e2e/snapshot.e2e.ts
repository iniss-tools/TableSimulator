// Snimka tabule: karta ulozi obsah tabule ako PNG s nazvom podla tabule a casu.
import { readFile } from "node:fs/promises";
import { expect, test } from "@playwright/test";
import { collectErrors, createLine, deleteLine, elenRow, sendToLine } from "./helpers";

const LINE = 63;

test("snimka tabule ako PNG", async ({ page }) => {
  const errors = collectErrors(page);
  await createLine(page, LINE, "Snímka");
  expect(await sendToLine(47000 + LINE, [elenRow(4, 1, "SNIMKA")])).toContain(0x06);

  await page.goto("/#/");
  const card = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 4` });
  await expect(card.locator("canvas")).toBeVisible();
  await card.getByRole("button", { name: "Snímka tabule (PNG)" }).click();
  const [download] = await Promise.all([page.waitForEvent("download"), page.getByRole("menuitem", { name: "Uložiť ako PNG" }).click()]);

  expect(download.suggestedFilename()).toMatch(/^tabula-63-4-\d{4}-\d\d-\d\d_\d{6}\.png$/);
  const png = await readFile(await download.path());
  // podpis PNG a rozmer z hlavicky IHDR
  expect([...png.subarray(0, 8)]).toEqual([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  expect(png.readUInt32BE(16)).toBeGreaterThan(300);
  expect(png.readUInt32BE(20)).toBeGreaterThan(50);

  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
});
