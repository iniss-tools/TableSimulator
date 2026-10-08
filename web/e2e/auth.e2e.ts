// Prihlasovanie (server so zapnutym prihlasovanim): zalozenie prveho spravcu, odhlasenie, zle a spravne heslo.
import { expect, test } from "@playwright/test";
import { E2E_SERVERS } from "./servers";

test.use({ baseURL: E2E_SERVERS.login.url });

test("prvy spravca, odhlasenie a prihlasenie", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText("Prvý správca")).toBeVisible();
  await page.getByLabel("Prihlasovacie meno").fill("spravca");
  await page.getByLabel("Meno", { exact: true }).fill("Správca E2E");
  await page.getByLabel("Heslo", { exact: true }).fill("heslo-e2e-1");
  await page.getByLabel("Heslo znova").fill("heslo-e2e-1");
  await page.getByRole("button", { name: "Založiť správcu" }).click();
  await expect(page.getByRole("link", { name: "Tabule", exact: true })).toBeVisible();

  await page.getByRole("button", { name: /Správca E2E/ }).click();
  await page.getByRole("menuitem", { name: "Odhlásiť" }).click();
  await expect(page.getByText("Prihlásenie", { exact: true })).toBeVisible();

  await page.getByLabel("Prihlasovacie meno").fill("spravca");
  await page.getByLabel("Heslo", { exact: true }).fill("zle-heslo");
  await page.getByRole("button", { name: "Prihlásiť" }).click();
  await expect(page.getByText("Nesprávne meno alebo heslo.")).toBeVisible();

  await page.getByLabel("Heslo", { exact: true }).fill("heslo-e2e-1");
  await page.getByRole("button", { name: "Prihlásiť" }).click();
  await expect(page.getByRole("link", { name: "Tabule", exact: true })).toBeVisible();
});
