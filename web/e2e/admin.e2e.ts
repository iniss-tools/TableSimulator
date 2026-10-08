// Sprava: pouzivatel (zalozenie a odstranenie) a pristupovy kluc zariadenia (odkaz s klucom, odstranenie).
import { expect, test } from "@playwright/test";
import { collectErrors } from "./helpers";

test("pouzivatel: zalozenie a odstranenie", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/users");
  await page.getByRole("button", { name: "Pridať používateľa" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Prihlasovacie meno").fill("operator-e2e");
  await dialog.getByLabel("Meno", { exact: true }).fill("Operátor E2E");
  await dialog.getByLabel("Heslo", { exact: true }).fill("heslo-operatora");
  await dialog.getByLabel("Heslo znova").fill("heslo-operatora");
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  const row = page.getByRole("row", { name: /operator-e2e/ });
  await expect(row).toContainText("Operátor E2E");

  await row.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(row).toBeHidden();
  expect(errors).toEqual([]);
});

test("kluc zariadenia: odkaz s klucom a odstranenie", async ({ page }) => {
  await page.goto("/#/keys");
  await page.getByRole("button", { name: "Nový kľúč" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("TV hala E2E");
  await dialog.getByRole("button", { name: "Vytvoriť" }).click();

  // tajna hodnota sa ukaze len raz - odkaz pre zariadenie ju obsahuje
  const created = page.getByRole("dialog");
  await expect(created.getByLabel("Odkaz pre zariadenie")).toHaveValue(/\?key=.+#\//);
  await created.getByRole("button", { name: "Zavrieť" }).first().click();
  await expect(created).toBeHidden();

  const row = page.getByRole("row", { name: /TV hala E2E/ });
  await row.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(row).toBeHidden();
});
