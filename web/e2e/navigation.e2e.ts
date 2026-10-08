// Ram aplikacie: vsetky stranky z menu sa otvoria bez chyb, jazyk a tema z ponuky pouzivatela.
import { expect, test } from "@playwright/test";
import { collectErrors, openPage } from "./helpers";

const PAGES = [
  "Vzhľady",
  "Linky",
  "Komunikácia",
  "Záznamy",
  "Virtuálne tabule",
  "Archív tabúľ",
  "Archív hlásení",
  "Používatelia",
  "Prístupové kľúče",
  "Audit",
  "Nastavenia",
  "Tabule",
];

test("menu otvori vsetky stranky bez chyb", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/");
  for (const label of PAGES) await openPage(page, label);
  expect(errors).toEqual([]);
});

test("jazyk a tema z ponuky pouzivatela", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: /Tento počítač/ }).click();
  await page.getByRole("menuitem", { name: "Jazyk" }).hover();
  await page.getByRole("menuitemradio", { name: "Čeština" }).click();
  await expect(page.getByRole("link", { name: "Nastavení", exact: true })).toBeVisible();

  await page.getByRole("button", { name: /Tento počítač/ }).click();
  await page.getByRole("menuitem", { name: "Vzhled" }).hover();
  await page.getByRole("menuitemradio", { name: "Tmavý" }).click();
  await expect(page.locator("html")).toHaveClass(/dark/);
});

test("neznama adresa nespadne", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/neexistuje");
  await expect(page.getByText("Stránka neexistuje.")).toBeVisible();
  await page.getByRole("button", { name: "Na úvodnú stránku" }).click();
  await expect(page.locator("header h1")).toHaveText("Tabule");
  expect(errors).toEqual([]);
});
