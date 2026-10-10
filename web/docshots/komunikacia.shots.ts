// Snimky kapitoly Komunikacia: log komunikacie, zaznamy a prehravanie, porovnanie zaznamov, statistiky.
import { expect, test, type APIRequestContext } from "@playwright/test";
import { API_HEADERS } from "../e2e/helpers";
import type { RecordingInfoDto, RecordingsDto } from "../src/api/generated/types.gen";
import { open, shot, THEMES } from "./shoot";

/** Zaznamy ukazkovej stanice od najstarsieho: A pred zmenou grafikonu, B po nej. */
async function recordings(request: APIRequestContext): Promise<[RecordingInfoDto, RecordingInfoDto]> {
  const { items } = (await (await request.get("/api/recordings")).json()) as RecordingsDto;
  const sorted = [...items].sort((x, y) => Date.parse(x.created) - Date.parse(y.created));
  expect(sorted.length).toBeGreaterThanOrEqual(2);
  return [sorted[0], sorted[1]];
}

for (const theme of THEMES) {
  test(`log komunikacie (${theme})`, async ({ page }) => {
    await open(page, "/log", theme);
    await shot(page, "log-komunikacie/log", theme, { ready: page.getByText("04 v dátach").first() });
  });

  test(`zaznamy (${theme})`, async ({ page }) => {
    const [a] = await recordings(page.request);
    await open(page, "/recordings", theme);
    await shot(page, "zaznamy/zaznamy", theme, { ready: page.getByText("Po zmene grafikonu") });

    // prehravanie pozastavene v polovici zaznamu A
    const play = await page.request.post("/api/replay", {
      headers: API_HEADERS,
      data: { name: a.name, speed: 1, loop: false, position: a.duration / 2, paused: true },
    });
    expect(play.ok(), await play.text()).toBe(true);
    try {
      await expect(page.getByRole("slider", { name: "Časová os záznamu" })).toBeVisible();
      await shot(page, "zaznamy/prehravanie", theme, { target: page.locator("main") });
    } finally {
      await page.request.post("/api/replay/stop", { headers: API_HEADERS });
    }
  });

  test(`porovnanie zaznamov (${theme})`, async ({ page }) => {
    const [a, b] = await recordings(page.request);
    await open(page, `/recordings/compare?a=${encodeURIComponent(a.name)}&b=${encodeURIComponent(b.name)}`, theme);
    await expect(page.getByText(/odlišné: \d/)).toBeVisible({ timeout: 30_000 });
    await shot(page, "porovnanie-zaznamov/porovnanie", theme);
    await page.getByRole("button", { name: "Zobraziť tabuľu v tomto okamihu" }).first().click();
    const dialog = page.getByRole("dialog");
    await shot(page, "porovnanie-zaznamov/tabula", theme, { target: dialog });
  });

  test(`statistiky (${theme})`, async ({ page }) => {
    await open(page, "/statistics", theme);
    await expect(page.getByRole("columnheader", { name: "Najdlhšia pauza" })).toBeVisible();
    await shot(page, "statistiky/statistiky", theme, { height: 1640 });
  });
}
