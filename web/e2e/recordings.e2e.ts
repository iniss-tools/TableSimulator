// Zaznam komunikacie: spustenie s poznamkou, priebeh a zastavenie - zaznam pribudne do zoznamu.
import { expect, test } from "@playwright/test";

test("spustenie a zastavenie zaznamu", async ({ page }) => {
  await page.goto("/#/recordings");
  await page.getByLabel("Poznámka").fill("e2e zaznam");
  await page.getByRole("button", { name: "Spustiť záznam" }).click();
  await expect(page.getByText(/Nahráva sa/)).toBeVisible();
  await page.getByRole("button", { name: "Zastaviť záznam" }).click();
  await expect(page.getByText(/Nahráva sa/)).toBeHidden();
  await expect(page.getByRole("cell", { name: /e2e zaznam/ })).toBeVisible();
});
