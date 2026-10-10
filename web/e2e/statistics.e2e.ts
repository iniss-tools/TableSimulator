// Statistika komunikacie: stranka s grafmi, prepnutie obdobia (interval grafu podla obdobia), posun dozadu a spat
// a dotaz na API. Minutove suhrny pribudnu az po minute - test overuje stranku, nie obsah grafov.
import { expect, test } from "@playwright/test";
import { collectErrors, openPage } from "./helpers";

test("statistika komunikacie v case", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await openPage(page, "Štatistiky");

  await expect(page.getByText("Správy od INISSu")).toBeVisible();
  await expect(page.getByText("interval 1 min")).toBeVisible();

  await page.getByRole("button", { name: "Týždeň" }).click();
  await expect(page.getByText("interval 60 min")).toBeVisible();
  const now = page.getByRole("button", { name: "Teraz", exact: true });
  await expect(now).toBeDisabled();
  await page.getByRole("button", { name: "Skôr", exact: true }).click();
  await expect(now).toBeEnabled();
  await now.click();
  await expect(now).toBeDisabled();

  const to = new Date();
  const from = new Date(to.getTime() - 3_600_000);
  const response = await page.request.get(`/api/statistics?from=${from.toISOString()}&to=${to.toISOString()}`);
  expect(response.ok()).toBe(true);
  expect(((await response.json()) as { bucketMinutes: number }).bucketMinutes).toBe(1);
  const invalid = await page.request.get(`/api/statistics?from=${to.toISOString()}&to=${from.toISOString()}`);
  expect(invalid.status()).toBe(400);

  expect(errors).toEqual([]);
});
