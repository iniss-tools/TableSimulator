// Snimky kapitoly INISSView a archiv: virtualne tabule, ich okno a obrazovka, rozlozenie, archiv tabul a hlaseni,
// monitor hlaseni. XML a archivy su v ukazkovom priecinku logov (docshots/iniss-log.ts).
import { expect, test, type Page } from "@playwright/test";
import { mask, open, shot, THEMES } from "./shoot";

/** Ponuka akcii riadku tabulky podla textu v riadku. */
async function rowAction(page: Page, row: string | RegExp, action: string) {
  await page.getByRole("row", { name: row }).first().getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: action }).click();
}

for (const theme of THEMES) {
  test(`virtualne tabule (${theme})`, async ({ page }) => {
    await open(page, "/virtual", theme);
    await expect(page.getByRole("cell", { name: "Odchody – hala" })).toBeVisible();
    await mask(page);
    await shot(page, "virtualne-tabule/zoznam", theme, { ready: page.getByText("aktuálne", { exact: true }).first() });
    await page.getByRole("tab", { name: /Súbory XML/ }).click();
    await expect(page.getByRole("cell", { name: "Odchody web" })).toBeVisible();
    await mask(page);
    await shot(page, "virtualne-tabule/subory", theme);
    await page.getByRole("tab", { name: /Tabule/ }).click();

    await rowAction(page, "Odchody – hala", "Upraviť");
    const dialog = page.getByRole("dialog");
    await shot(page, "virtualne-tabule/okno", theme, { target: dialog, ready: dialog.getByLabel("Súbor XML") });
  });

  test(`virtualna obrazovka (${theme})`, async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    for (const [id, name] of [
      [1, "odchody"],
      [3, "podchod"],
    ] as const) {
      await open(page, `/virtual/${id}`, theme, "body");
      await expect(page.getByText("Trnava").first()).toBeVisible();
      // ovladanie kiosku sa skryje po 3 s bez pohybu mysi
      await page.waitForTimeout(3500);
      await shot(page, `virtualne-tabule/${name}`, theme);
    }
  });

  test(`rozlozenia (${theme})`, async ({ page }) => {
    await open(page, "/virtual", theme);
    await page.getByRole("tab", { name: /Rozloženia/ }).click();
    await mask(page);
    await shot(page, "rozlozenia-a-sablony/rozlozenia", theme, { ready: page.getByRole("cell", { name: /Odchody – modrá/ }) });
    await rowAction(page, /Odchody – modrá/, "Kopírovať");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Trnava").first()).toBeVisible();
    await shot(page, "rozlozenia-a-sablony/rozlozenie", theme, { target: dialog, height: 1400 });
  });

  test(`archiv (${theme})`, async ({ page }) => {
    await open(page, "/archive/tables", theme);
    await shot(page, "archiv/archiv-tabul", theme, { ready: page.getByText("Vlak R 612 mešká asi 10 minút").first() });
    await open(page, "/archive/reports", theme);
    await shot(page, "archiv/archiv-hlaseni", theme, { ready: page.getByText("Výluka Dolné Mesto - Horná Ves").first() });
    await open(page, "/archive/reports/live", theme);
    await shot(page, "archiv/monitor", theme, { ready: page.getByText("naživo", { exact: true }) });
  });
}
