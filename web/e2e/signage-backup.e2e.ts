// Zaloha informacnych panelov: stiahnutie zalohy, pridanie jej obsahu s novymi cislami a obnova "nahradit vsetko"
// s potvrdenim - po nej je vsetko ako pri zalohovani.
import { expect, test, type Page } from "@playwright/test";
import { apiPost, collectErrors } from "./helpers";

interface Overview {
  messages: { id: number }[];
  panels: { id: number }[];
}

const overview = async (page: Page) => (await (await page.request.get("/api/signage")).json()) as Overview;

test("zaloha a obnova informacnych panelov", async ({ page }) => {
  const errors = collectErrors(page);
  const message = await apiPost<{ id: number }>(page, "/api/signage/messages", {
    name: "E2E záloha",
    note: "",
    kind: "text",
    duration: 10,
    virtualBoard: null,
    board: null,
    paragraphs: [{ text: "Záloha", color: "#ffffff", size: "large", align: "center" }],
    background: "#000000",
    media: null,
    url: null,
  });
  await apiPost(page, "/api/signage/panels", { name: "E2E záloha", note: "", group: null });
  const before = await overview(page);

  await page.goto("/#/signage/panels");
  const [download] = await Promise.all([page.waitForEvent("download"), page.getByRole("link", { name: "Zálohovať" }).click()]);
  const backup = await download.path();

  // pridanie: vsetko este raz s novymi cislami
  await page.getByRole("button", { name: "Obnoviť…" }).click();
  const added = page.waitForEvent("filechooser");
  await page.getByRole("menuitem", { name: "Pridať obsah zálohy…" }).click();
  await (await added).setFiles(backup);
  await expect(page.getByText("Pridané zo zálohy")).toBeVisible();
  expect((await overview(page)).messages).toHaveLength(before.messages.length * 2);

  // nahradenie: ako pri zalohovani, s povodnymi cislami
  await page.getByRole("button", { name: "Obnoviť…" }).click();
  const replaced = page.waitForEvent("filechooser");
  await page.getByRole("menuitem", { name: "Nahradiť všetko zálohou…" }).click();
  await (await replaced).setFiles(backup);
  await page.getByRole("alertdialog").getByRole("button", { name: "Obnoviť…" }).click();
  await expect(page.getByText("Záloha obnovená")).toBeVisible();
  const after = await overview(page);
  expect(after.messages.map((m) => m.id)).toEqual(before.messages.map((m) => m.id));
  expect(after.panels.map((p) => p.id)).toEqual(before.panels.map((p) => p.id));
  expect(after.messages.some((m) => m.id === message.id)).toBe(true);
  expect(errors).toEqual([]);
});
