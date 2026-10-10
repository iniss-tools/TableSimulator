// Scenar porucha: novy scenar s krokom na tabuli, spustenie (tabula neodpoveda, karta scenara bezi), zastavenie
// (tabula ma spat povodnu poruchu) a odstranenie.
import { expect, test } from "@playwright/test";
import { API_HEADERS, collectErrors, createLine, deleteLine, elenRow, openPage, sendToLine } from "./helpers";

const LINE = 65;

test("scenar porucha v case", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await createLine(page, LINE, "E2E scenár");
  await sendToLine(47000 + LINE, [elenRow(5, 1, "SCENAR")]);
  const fault = async () => {
    const state = (await (await page.request.get("/api/state")).json()) as { boards: { id: string; fault: { kind: string } }[] };
    return state.boards.find((b) => b.id === `${LINE}-5`)?.fault.kind;
  };

  await openPage(page, "Scenáre porúch");
  await page.getByRole("button", { name: "Nový scenár" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("E2E scenár");
  await dialog.getByRole("button", { name: "Pridať krok" }).click();
  await dialog.getByRole("combobox", { name: "Linka" }).click();
  await page.getByRole("option", { name: `${LINE} – E2E scenár` }).click();
  await dialog.getByRole("combobox", { name: "Na čom" }).click();
  await page.getByRole("option", { name: "5", exact: true }).click();
  await dialog.getByLabel("Trvá [s]").fill("120");
  await expect(dialog.getByText(`Linka ${LINE} · tabuľa 5 – Neodpovedá`)).toBeVisible();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();

  const card = page.locator("[data-slot=card]").filter({ hasText: "E2E scenár" });
  await card.getByRole("button", { name: "Spustiť" }).click();
  await expect(card.getByText("beží · cyklus 1")).toBeVisible();
  await expect.poll(fault).toBe("noReply");

  await card.getByRole("button", { name: "Zastaviť" }).click();
  await expect(card.getByRole("button", { name: "Spustiť" })).toBeVisible();
  await expect.poll(fault).toBe("none");

  await card.getByRole("button", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(card).toBeHidden();

  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
  // nic nezostalo
  const left = (await (await page.request.get("/api/scenarios", { headers: API_HEADERS })).json()) as { name: string }[];
  expect(left.filter((s) => s.name === "E2E scenár")).toEqual([]);
});
