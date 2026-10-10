// Snimky kapitoly Sprava servera: casti stranky Nastavenia, pouzivatelia, roly a audit. Cesty, meno a adresy pocitaca
// nahradi mask().
import { expect, test, type Page } from "@playwright/test";
import { mask, open, shot, THEMES } from "./shoot";

/** Cast stranky Nastavenia - okno vysoke, aby ju neprekryla lista s tlacidlom Ulozit. */
async function section(page: Page, id: string, name: string, theme: (typeof THEMES)[number]) {
  const card = page.locator(`#settings-${id}`);
  await card.scrollIntoViewIfNeeded();
  await mask(page);
  await shot(page, name, theme, { target: card, height: 4000 });
}

/** Okno polozky z ponuky jej riadku. */
async function edit(page: Page, row: string) {
  await page.getByRole("row", { name: row }).first().getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Upraviť" }).click();
  return page.getByRole("dialog");
}

for (const theme of THEMES) {
  test(`nastavenia (${theme})`, async ({ page }) => {
    await open(page, "/settings", theme);
    await expect(page.getByText("HTTPS beží:")).toBeVisible();
    await mask(page);
    await shot(page, "nastavenia/nastavenia", theme);
    await section(page, "folders", "nastavenia/priecinky", theme);
    await section(page, "iniss", "nastavenia/iniss", theme);
    await section(page, "network", "nastavenia/siet", theme);
    await section(page, "server", "nastavenia/server", theme);
    await section(page, "https", "https/https", theme);
    await section(page, "alerts", "upozornenia/upozornenia", theme);
    await section(page, "backup", "zaloha-a-obnova/zaloha", theme);
    await section(page, "database", "databaza/databaza", theme);

    // PostgreSQL vyplnene, neulozene
    const database = page.locator("#settings-database");
    await database.getByRole("combobox", { name: "Databáza" }).click();
    await page.getByRole("option", { name: "PostgreSQL" }).click();
    await database.getByLabel("Server").fill("db.stanica.local");
    await database.getByLabel("Názov databázy").fill("tablesimulator");
    await database.getByLabel("Používateľ").fill("simulator");
    await section(page, "database", "databaza/postgres", theme);
  });

  test(`pouzivatelia a roly (${theme})`, async ({ page }) => {
    await open(page, "/users", theme);
    await shot(page, "pouzivatelia-a-roly/pouzivatelia", theme, { ready: page.getByRole("cell", { name: "Informačné panely" }) });
    const user = await edit(page, "Ján Novák");
    await shot(page, "pouzivatelia-a-roly/okno-pouzivatela", theme, { target: user });

    await open(page, "/roles", theme);
    await shot(page, "pouzivatelia-a-roly/roly", theme, { ready: page.getByRole("cell", { name: /Informačné panely/ }) });
    const role = await edit(page, "Informačné panely");
    await shot(page, "pouzivatelia-a-roly/okno-roly", theme, { target: role, height: 1300 });

    await open(page, "/audit", theme);
    await mask(page);
    await shot(page, "pouzivatelia-a-roly/audit", theme, { ready: page.getByRole("cell", { name: "Nový používateľ" }).first() });
  });
}
