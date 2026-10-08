// Linka a tabula: zalozenie linky v okne, INISS posle text na port linky, tabula sa zalozi sama (samoucenie),
// zobrazi spravu v karte, v komunikacii aj na celej obrazovke; odstranenie linky s potvrdenim.
import { expect, test } from "@playwright/test";
import { collectErrors, elenRow, openPage, sendToLine } from "./helpers";

const LINE = 61;
const PORT = 47000 + LINE;

test("linka, sprava od INISSu a samoucena tabula", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/lines");
  await page.getByRole("button", { name: "Pridať linku" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Číslo linky (1–100)").fill(String(LINE));
  await dialog.getByLabel("Názov").fill("E2E linka");
  await expect(dialog.getByLabel("Spojenie (tvar ako TablePort)")).toHaveValue(`TCP://0.0.0.0:${PORT}`);
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole("cell", { name: "E2E linka" })).toBeVisible();

  // INISS: riadok textu tabuli s adresou 3 - tabula odpovie ACK
  const replies = await sendToLine(PORT, [elenRow(3, 1, "E2E TEST")]);
  expect(replies).toContain(0x06);

  await openPage(page, "Tabule");
  const card = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 3` });
  await expect(card).toBeVisible();
  await expect(card).toContainText("1 správ");
  await expect(card.locator("canvas")).toBeVisible();

  await openPage(page, "Komunikácia");
  await expect(page.getByRole("cell", { name: String(LINE), exact: true }).first()).toBeVisible();

  // cela obrazovka tabule
  await page.goto(`/#/view/${LINE}-3`);
  await expect(page.locator("canvas")).toBeVisible();

  await page.goto("/#/lines");
  await page
    .getByRole("row", { name: /E2E linka/ })
    .getByRole("button", { name: "Akcie" })
    .click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(page.getByRole("cell", { name: "E2E linka" })).toBeHidden();
  expect(errors).toEqual([]);
});
