// Prehravanie zaznamu s casovou osou: zaznam troch sprav (A, B, C po 1,2 s), prehratie, pauza, posun na miesto medzi
// B a C (tabula ukaze B), rychlost 10× a dohratie (tabula ukaze C).
import { expect, test } from "@playwright/test";
import { API_HEADERS, apiPost, collectErrors, createLine, deleteLine, elenRow, sendToLine } from "./helpers";

const LINE = 66;
const PORT = 47000 + LINE;

interface Replay {
  name: string;
  state: string;
  position: number;
}

test("prehravanie zaznamu s casovou osou", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/recordings");
  await createLine(page, LINE, "E2E prehrávanie");
  await apiPost(page, "/api/recordings/start", { note: "e2e casova os" });
  for (const text of ["A", "B", "C"]) {
    if (text !== "A") await page.waitForTimeout(1200);
    await sendToLine(PORT, [elenRow(5, 1, text)]);
  }
  await apiPost(page, "/api/recordings/stop", {});
  const replay = async () => ((await (await page.request.get("/api/recordings")).json()) as { replay: Replay | null }).replay;
  const boardText = async () => {
    const board = (await (await page.request.get(`/api/boards/${LINE}-5`)).json()) as { display: { rows: { elements: { run: { text: string } | null }[] }[] } };
    return board.display.rows.flatMap((r) => r.elements.map((e) => e.run?.text ?? "")).join("");
  };

  await page.getByRole("row").filter({ hasText: "e2e casova os" }).getByRole("button", { name: "Prehrať" }).click();
  const timeline = page.getByLabel("Časová os záznamu");
  const player = page.locator("[data-slot=card]").filter({ has: timeline });
  await expect(player.getByText(/Prehráva sa/)).toBeVisible();
  await player.getByRole("button", { name: "Pozastaviť" }).click();
  await expect(player.getByText(/pozastavené/)).toBeVisible();

  // posun medzi B a C - tabula ukaze B, prehravanie ostane pozastavene
  await timeline.fill("1.8");
  await timeline.blur();
  await expect.poll(async () => (await replay())?.position).toBeCloseTo(1.8, 1);
  expect((await replay())?.state).toBe("paused");
  await expect.poll(boardText).toContain("B");

  await player.getByRole("combobox", { name: "Rýchlosť" }).click();
  await page.getByRole("option", { name: "10×" }).click();
  await expect.poll(async () => ((await (await page.request.get("/api/recordings")).json()) as { replay: { speed: number } }).replay.speed).toBe(10);
  await player.getByRole("button", { name: "Pokračovať" }).click();
  await expect(player.getByText(/skončilo/)).toBeVisible();
  await expect.poll(boardText).toContain("C");

  const name = (await replay())!.name;
  await page.request.delete(`/api/recordings/${encodeURIComponent(name)}`, { headers: API_HEADERS });
  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
});
