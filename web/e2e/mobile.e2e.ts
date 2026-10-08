// Mobil: menu v bocnom paneli sa otvara tlacidlom, stranka sa neposuva do strany.
import { expect, test } from "@playwright/test";

test.use({ viewport: { width: 375, height: 812 }, isMobile: true, hasTouch: true });

test("menu na mobile a bez posuvania do strany", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: "Zobraziť alebo skryť menu" }).click();
  await page.getByRole("link", { name: "Nastavenia", exact: true }).click();
  await expect(page.locator("header h1")).toHaveText("Nastavenia");
  const width = await page.evaluate(() => document.documentElement.scrollWidth);
  expect(width).toBeLessThanOrEqual(375);
});
