// Snimky kapitoly Zobrazenie na zariadeniach: pristupove kluce, okno noveho kluca s odkazom a QR kodom, kiosk tabule.
import { expect, test } from "@playwright/test";
import type { ServerInfoDto } from "../src/api/generated/types.gen";
import { open, shot, THEMES } from "./shoot";

for (const theme of THEMES) {
  test(`pristupove kluce (${theme})`, async ({ page }) => {
    // adresa pocitaca v sieti ukazkova - nie skutocna adresa pocitaca, na ktorom harness bezi
    await page.route("**/api/server", async (route) => {
      const response = await route.fetch();
      const info = (await response.json()) as ServerInfoDto;
      await route.fulfill({ response, json: { ...info, addresses: ["192.168.1.20"] } });
    });
    await open(page, "/keys", theme);
    await shot(page, "pristupove-kluce/kluce", theme, { ready: page.getByRole("cell", { name: "GVDEditor" }) });

    await page.getByRole("button", { name: "Nový kľúč" }).click();
    let dialog = page.getByRole("dialog");
    await dialog.getByLabel("Názov").fill("Televízor v podchode");
    await shot(page, "pristupove-kluce/novy-kluc", theme, { target: dialog });
    await dialog.getByRole("button", { name: "Vytvoriť" }).click();

    dialog = page.getByRole("dialog");
    await dialog.getByRole("combobox", { name: "Stránka zariadenia" }).click();
    await page.getByRole("option", { name: /Informačný panel 3/ }).click();
    await expect(dialog.getByRole("img", { name: "QR kód odkazu" })).toBeVisible();
    await shot(page, "pristupove-kluce/odkaz", theme, { target: dialog });
  });

  test(`kiosk (${theme})`, async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    await open(page, "/view/1-1", theme, "body");
    await expect(page.getByText("Celá obrazovka").or(page.locator("canvas")).first()).toBeVisible();
    // pohyb mysi ukaze ovladanie kiosku (skryje sa po 3 s)
    await page.mouse.move(640, 360);
    await page.mouse.move(650, 370);
    await expect(page.getByText("Celá obrazovka")).toBeVisible();
    await shot(page, "kiosk/tabula", theme);
  });
}
