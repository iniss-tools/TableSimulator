// Informacne panely: textova sprava v okne s nahladom, playlist, skupina a panel; stranka panelu hra spravu
// a prehlad panelov ukaze, ze je pripojeny a co hra; panel bez skupiny je mimo prevadzky.
import { expect, test } from "@playwright/test";
import { apiPost, collectErrors } from "./helpers";

test("sprava, playlist, skupina a panel", async ({ page, context }) => {
  const errors = collectErrors(page);
  await page.goto("/#/signage/messages");
  await page.getByRole("button", { name: "Nová správa" }).click();
  await page.getByRole("menuitem", { name: "Text" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("E2E uvítanie");
  await dialog.getByLabel("Odsek 1").fill("Vitajte na stanici");
  // nahlad kresli spravu hned
  await expect(dialog.locator("p", { hasText: "Vitajte na stanici" })).toBeVisible();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole("cell", { name: "E2E uvítanie" })).toBeVisible();

  const overview = (await (await page.request.get("/api/signage")).json()) as { messages: { id: number; definition: { name: string } }[] };
  const message = overview.messages.find((m) => m.definition.name === "E2E uvítanie")!;
  const playlist = await apiPost<{ id: number }>(page, "/api/signage/playlists", {
    name: "E2E celý deň",
    period: "daily",
    start: "2020-01-01",
    end: null,
    weekdays: [],
    from: 0,
    to: 0,
    items: [{ message: message.id, duration: null }],
  });
  const group = await apiPost<{ id: number }>(page, "/api/signage/groups", {
    name: "E2E hala",
    note: "",
    playlists: [playlist.id],
    defaultPlaylist: null,
    defaultMessage: null,
  });
  const panel = await apiPost<{ id: number }>(page, "/api/signage/panels", { name: "E2E monitor", note: "", group: group.id });
  const idle = await apiPost<{ id: number }>(page, "/api/signage/panels", { name: "E2E bez skupiny", note: "", group: null });

  const monitor = await context.newPage();
  await monitor.goto(`/#/panel/${panel.id}`);
  await expect(monitor.getByText("Vitajte na stanici")).toBeVisible();
  await monitor.goto(`/#/panel/${idle.id}`);
  await expect(monitor.getByText("Mimo prevádzky")).toBeVisible();
  await expect(monitor.getByText("Panel nie je zaradený do skupiny.")).toBeVisible();

  await page.goto("/#/signage/panels");
  const row = page.getByRole("row", { name: /E2E monitor/ });
  await expect(row).toContainText("pripojený");
  await expect(row).toContainText("E2E uvítanie");
  expect(errors).toEqual([]);
});
