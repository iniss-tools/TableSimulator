// Nastavenia: ulozenie zmeny, zahodenie a varovanie pri odchode s neulozenymi zmenami.
import { expect, test } from "@playwright/test";

test("ulozenie, zahodenie a odchod bez ulozenia", async ({ page }) => {
  await page.goto("/#/settings");
  const host = page.getByLabel("Adresa tohto počítača pre INISS");
  await host.fill("10.0.0.5");
  await expect(page.getByText("Neuložené zmeny")).toBeVisible();
  await page.getByRole("button", { name: "Uložiť", exact: true }).click();
  await expect(page.getByText("Uložené.")).toBeVisible();
  await expect(page.getByText("Neuložené zmeny")).toBeHidden();
  await page.reload();
  await expect(host).toHaveValue("10.0.0.5");

  // chybna hodnota - chyba servera v liste ulozenia
  await host.fill("zla adresa");
  await page.getByRole("button", { name: "Uložiť", exact: true }).click();
  await expect(page.getByText("Adresa pre INISS nesmie obsahovať medzery.")).toBeVisible();

  // odchod s neulozenou zmenou sa pyta; zrusenie necha stranku
  await page.getByRole("link", { name: "Linky", exact: true }).click();
  const confirm = page.getByRole("alertdialog");
  await expect(confirm).toContainText("Odísť bez uloženia?");
  await confirm.getByRole("button", { name: "Zrušiť" }).click();
  await expect(page).toHaveURL(/#\/settings$/);

  await page.getByRole("button", { name: "Zahodiť zmeny" }).click();
  await expect(host).toHaveValue("10.0.0.5");
  await host.fill("");
  await page.getByRole("button", { name: "Uložiť", exact: true }).click();
  await expect(page.getByText("Uložené.")).toBeVisible();
});

test("zoznam pisiem a vzhladov v okne", async ({ page }) => {
  await page.goto("/#/settings");
  await page.getByRole("button", { name: /Dostupné vzhľady: \d+/ }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByRole("cell", { name: "elen-led", exact: true })).toBeVisible();
  await dialog.getByRole("button", { name: "Zavrieť" }).first().click();
  await expect(dialog).toBeHidden();
});
