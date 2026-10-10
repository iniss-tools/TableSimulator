// Vyber databazy na stranke Nastavenia: SQLite je pouzivana, po vybere PostgreSQL sa ukazu polia pripojenia, skuska
// nedostupneho servera ukaze chybu a zle parametre server odmietne - nic sa neulozi, server ostava na SQLite.
import { expect, test } from "@playwright/test";
import type { DatabaseSettingsDto } from "../src/api/generated/types.gen";
import { collectErrors, openPage } from "./helpers";

test("vyber databazy a skuska pripojenia", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await openPage(page, "Nastavenia");
  await page.getByRole("button", { name: "Databáza", exact: true }).click();
  const section = page.locator("#settings-database");
  await expect(section.getByText("SQLite", { exact: true })).toBeVisible();
  await expect(section.getByRole("button", { name: "Uložiť výber databázy" })).toBeDisabled();

  await section.getByRole("combobox").click();
  await page.getByRole("option", { name: "PostgreSQL" }).click();
  await expect(section.getByLabel("Server")).toHaveValue("localhost");
  await expect(section.getByLabel("Názov databázy")).toHaveValue("tablesimulator");

  await section.getByLabel("Server").fill("127.0.0.1");
  await section.getByLabel("Port").fill("1");
  await section.getByRole("button", { name: "Overiť pripojenie" }).click();
  await expect(section.getByText("Pripojenie zlyhalo")).toBeVisible({ timeout: 15_000 });

  // heslo patri do vlastneho pola, nie do dalsich parametrov
  await section.getByLabel("Ďalšie parametre pripojenia").fill("Password=tajne");
  await section.getByRole("button", { name: "Uložiť výber databázy" }).click();
  await expect(section.getByText("Ďalšie parametre musia byť dvojice názov=hodnota", { exact: false })).toBeVisible();

  await section.getByRole("button", { name: "Zahodiť", exact: true }).click();
  await expect(section.getByRole("combobox")).toHaveText(/SQLite/);
  await expect(section.getByLabel("Server")).toBeHidden();
  const current = (await (await page.request.get("/api/database")).json()) as DatabaseSettingsDto;
  expect(current).toMatchObject({ active: "sqlite", provider: "sqlite", restartRequired: false });
  // zamietnute ulozenie (400) prehliadac zapise do konzoly
  expect(errors.filter((e) => !e.includes("400"))).toEqual([]);
});
