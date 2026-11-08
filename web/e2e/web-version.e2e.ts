// Nove rozhranie na serveri: pruh v rame s tlacidlom Nacitat znova, kiosk sa nacita znova sam - len raz.
import { expect, test, type Page } from "@playwright/test";
import { collectErrors } from "./helpers";

// service worker by dotaz na stranku presmeroval mimo page.route
test.use({ serviceWorkers: "block" });

/** Server posiela stranku inej zostavy - len dotaz stranky po pripojeni, navigacia ide na skutocny server. */
async function newerBuild(page: Page) {
  await page.route(
    (url) => url.pathname === "/",
    async (route) => {
      if (route.request().isNavigationRequest()) await route.continue();
      else await route.fulfill({ contentType: "text/html", body: '<script type="module" crossorigin src="/assets/index-novy.js"></script>' });
    },
  );
}

test("nove rozhranie: pruh v rame a kiosk sa nacita znova", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/devices");
  await expect(page.locator("header h1")).toHaveText("Zariadenia");
  // stranka zo servera je ta ista - ziadny pruh
  await page.waitForTimeout(1000);
  await expect(page.getByRole("button", { name: "Načítať znova" })).toBeHidden();

  await newerBuild(page);
  await page.reload();
  await expect(page.getByText("Server má novú verziu rozhrania.")).toBeVisible();
  let loads = 0;
  page.on("load", () => loads++);
  await page.getByRole("button", { name: "Načítať znova" }).click();
  await expect.poll(() => loads).toBe(1);
  expect(errors).toEqual([]);

  // kiosk bez obsluhy: nacita sa znova sam, ale len raz pre tu istu verziu
  const kiosk = await page.context().newPage();
  const kioskErrors = collectErrors(kiosk);
  await newerBuild(kiosk);
  let kioskLoads = 0;
  kiosk.on("load", () => kioskLoads++);
  await kiosk.goto("/#/wall");
  await expect.poll(() => kioskLoads).toBe(2);
  await kiosk.waitForTimeout(3000);
  expect(kioskLoads).toBe(2);
  await kiosk.close();
  expect(kioskErrors).toEqual([]);
});
