// Snimky kapitoly Informacne panely: obrazovka panelu, spravy, playlisty, skupiny a panely s ich oknami. Obsah
// zaklada docshots/signage.ts.
import { expect, test, type Page } from "@playwright/test";
import { open, shot, THEMES } from "./shoot";

/** Okno polozky z ponuky jej riadku. */
async function edit(page: Page, row: string) {
  await page.getByRole("row", { name: row }).first().getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Upraviť" }).click();
  return page.getByRole("dialog");
}

for (const theme of THEMES) {
  test(`panel (${theme})`, async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await open(page, "/panel/3", theme, "body");
    await expect(page.getByText("Výluka", { exact: true })).toBeVisible();
    // ovladanie kiosku sa skryje po 3 s bez pohybu mysi
    await page.waitForTimeout(3500);
    await shot(page, "prehlad/panel", theme);
  });

  test(`spravy (${theme})`, async ({ page }) => {
    await open(page, "/signage/messages", theme);
    await shot(page, "spravy/spravy", theme, { ready: page.getByRole("cell", { name: "Vitajte" }) });
    let dialog = await edit(page, "Výluka Dolné Mesto – Horná Ves");
    await shot(page, "spravy/text", theme, { target: dialog, height: 1300, ready: dialog.getByText("Náhľad") });
    await dialog.getByRole("button", { name: "Zrušiť" }).click();
    dialog = await edit(page, "Odchody – hala");
    await expect(dialog.getByText("Trnava").first()).toBeVisible();
    await shot(page, "spravy/tabula", theme, { target: dialog });
  });

  test(`playlisty a skupiny (${theme})`, async ({ page }) => {
    await open(page, "/signage/playlists", theme);
    await shot(page, "playlisty-a-skupiny/playlisty", theme, { ready: page.getByRole("cell", { name: "Ranná špička" }) });
    const playlist = await edit(page, "Ranná špička");
    await shot(page, "playlisty-a-skupiny/playlist", theme, { target: playlist, height: 1100 });

    await open(page, "/signage/groups", theme);
    await shot(page, "playlisty-a-skupiny/skupiny", theme, { ready: page.getByRole("cell", { name: "Podchod", exact: true }) });
    const group = await edit(page, "Monitory v hale");
    await shot(page, "playlisty-a-skupiny/skupina", theme, { target: group });
  });

  test(`panely (${theme})`, async ({ page, context }) => {
    // panely 1 a 3 sa ozvu serveru - na stranke Panely su pripojene a vidno, co hraju
    for (const id of [1, 3]) {
      const monitor = await context.newPage();
      await monitor.goto(`/#/panel/${id}`);
      await monitor.locator("body").waitFor();
      await monitor.waitForTimeout(1500);
      await monitor.close();
    }
    await open(page, "/signage/panels", theme);
    await expect(page.getByText("pripojený", { exact: true }).first()).toBeVisible();
    await shot(page, "panely/panely", theme);
    const dialog = await edit(page, "Televízor nad pokladňami");
    await shot(page, "panely/okno", theme, { target: dialog });
  });
}
