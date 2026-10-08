// Kiosk ako aplikacia na plochu: spustenie z manifestu (/?view=…), manifest kiosku s celou obrazovkou, service worker.
import { expect, test } from "@playwright/test";

test("kiosk z plochy a jeho manifest", async ({ page }) => {
  // start_url manifestu kiosku - web ju prevedie na adresu za #
  await page.goto("/?view=%2Fwall");
  await expect(page).toHaveURL(/\/#\/wall$/);

  const link = page.locator("link[rel=manifest]");
  await expect(link).toHaveAttribute("href", /\/api\/manifest\?view=%2Fwall/);
  const href = await link.getAttribute("href");
  const manifest = (await (await page.request.get(href!)).json()) as { start_url: string; display: string; icons: { purpose: string }[] };
  expect(manifest.start_url).toBe("/?view=%2Fwall");
  expect(manifest.display).toBe("fullscreen");
  expect(manifest.icons.some((i) => i.purpose === "maskable")).toBe(true);

  // rozhranie v pamati pre start bez servera
  await expect.poll(() => page.evaluate(async () => !!(await navigator.serviceWorker.getRegistration())?.active)).toBe(true);

  // ovladanie kiosku po pohybe mysi
  await page.mouse.move(200, 200);
  await expect(page.getByRole("button", { name: "Celá obrazovka" })).toBeVisible();

  // mimo kiosku manifest hlavnej aplikacie
  await page.goto("/#/");
  await expect(page.locator("link[rel=manifest]")).toHaveAttribute("href", "/manifest.webmanifest");
});
