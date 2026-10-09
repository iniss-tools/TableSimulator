// Linka vzdialeneho INISSu (trieda 128): zalozenie v okne (popis triedy, bez vyrobcu samoucenia), riadiaci INISS zapise
// na tabulu ELEN16 aj LCD1 na tej istej linke - obe karty a zaznam s vyrobcom z riadiaceho INISSu.
import { expect, test } from "@playwright/test";
import { collectErrors, deleteLine, elenRow, openPage, remoteTableMessage, sendToLine } from "./helpers";

const LINE = 63;
const PORT = 47000 + LINE;

/** Zapis riadku tabule Elektrocas LCD1 (prikaz 0x16): riadok 0 od stlpca 0, jedna vrstva (cislo 0) - text a pismo kazdeho znaku. */
function lcdRow(address: number, text: string): number[] {
  const params = [0, 0, 1, 0, ...Array.from(text, (ch) => ch.charCodeAt(0)), ...new Array<number>(text.length).fill(0x12)];
  const frame = [address, params.length + 5, 0, 0x16, ...params];
  return [...frame, frame.reduce((a, b) => a + b, 0) & 0xff];
}

test("linka vzdialeneho INISSu s tabulami ELEN aj Elektrocas", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/lines");
  await page.getByRole("button", { name: "Pridať linku" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Číslo linky (1–100)").fill(String(LINE));
  await dialog.getByLabel("Názov").fill("E2E vzdialený");
  await dialog.getByLabel("Trieda linky (TableClass)").click();
  await page.getByRole("option", { name: "128 – vzdialený INISS" }).click();
  await expect(dialog.getByText(/Simulátor zastúpi INISS spustený s \/Remote/)).toBeVisible();
  await expect(dialog.getByLabel("Výrobca tabúľ pri samoučení")).toBeHidden();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole("cell", { name: "128 – vzdialený INISS" })).toBeVisible();

  // ELEN16 (kod 8) na adrese 5 a LCD1 (kod 5) na adrese 20 - kazda sprava dostane odpoved 0xAC
  const replies = await sendToLine(PORT, [remoteTableMessage(1, 8, 5, elenRow(5, 1, "VZDIALENY"), 2), remoteTableMessage(2, 5, 20, lcdRow(20, "Os 3412"), 1)]);
  expect(replies[0]).toBe(0xac);

  await openPage(page, "Tabule");
  const elen = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 5` });
  await expect(elen).toContainText("ELEN16");
  await expect(elen.locator("canvas")).toBeVisible();
  const lcd = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 20` });
  await expect(lcd).toContainText("LCD1");

  await openPage(page, "Komunikácia");
  await expect(page.getByText(/VZDIALENY.*vzdialený INISS, ELEN16, správa 1/)).toBeVisible();

  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
});
