// Cela tabul: kopia vstavanej predlohy, uprava a ulozenie, predloha v okne tabule s cislom kolaje, odstranenie
// pouzivanej predlohy odmietnute.
import { expect, test } from "@playwright/test";
import { API_HEADERS, collectErrors, createLine, deleteLine, elenRow, sendToLine } from "./helpers";

const LINE = 72;

test("predloha cela: kopia, uprava, tabula s kolajou a odstranenie", async ({ page }) => {
  const errors = collectErrors(page);
  await createLine(page, LINE, "E2E čelo");
  // INISS: tabula s adresou 4 sa zalozi sama
  await sendToLine(47000 + LINE, [elenRow(4, 1, "E2E CELO")]);

  await page.goto("/#/faces");
  await expect(page.getByText("Vstavanú predlohu nemožno meniť", { exact: false })).toBeVisible();
  // kopia nastupistnej predlohy - ma premennu {track}
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "Nástupištná – koľaj vľavo, stanica a hodiny" }).click();
  await page.getByRole("button", { name: "Nová predloha" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("E2E čelo");
  await expect(dialog.getByLabel("Kľúč")).toHaveValue("e2e-celo");
  await dialog.getByRole("button", { name: "Vytvoriť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.locator("code", { hasText: /^e2e-celo$/ })).toBeVisible();

  await page.getByLabel("Vľavo", { exact: true }).fill("20");
  await expect(page.getByText("Neuložené zmeny")).toBeVisible();
  await page.getByRole("button", { name: "Uložiť" }).click();
  await expect(page.getByText("Predloha je uložená a tabule sa prekreslili.")).toBeVisible();
  const faces = (await (await page.request.get("/api/faces")).json()) as { id: string; margin: { left: number } }[];
  expect(faces.find((f) => f.id === "e2e-celo")?.margin.left).toBe(20);

  // okno tabule: zalozka Celo, predloha a cislo kolaje
  await page.goto("/#/");
  const card = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 4` });
  await card.getByRole("button", { name: "Upraviť" }).click();
  const boardDialog = page.getByRole("dialog");
  await boardDialog.getByRole("tab", { name: "Čelo" }).click();
  await boardDialog.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "E2E čelo (e2e-celo)" }).click();
  await boardDialog.getByLabel("Koľaj").fill("3");
  await boardDialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(boardDialog).toBeHidden();
  const state = (await (await page.request.get("/api/state")).json()) as {
    boards: { id: string; profile: { face: string; faceValues: Record<string, string> } }[];
  };
  const board = state.boards.find((b) => b.id === `${LINE}-4`)!;
  expect(board.profile.face).toBe("e2e-celo");
  expect(board.profile.faceValues.track).toBe("3");

  // pouzivana predloha sa neda odstranit
  await page.goto("/#/faces");
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "E2E čelo" }).click();
  // tlacidlo stranky s textom (prvky maju ikonove tlacidla s rovnakym popisom)
  await page.getByRole("button", { name: "Odstrániť" }).filter({ hasText: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(page.getByText("Predlohu používajú tabule.", { exact: false })).toBeVisible();

  await deleteLine(page, LINE);
  const removed = await page.request.delete("/api/faces/e2e-celo", { headers: API_HEADERS });
  expect(removed.status()).toBe(204);
  expect(errors.filter((e) => !e.includes("400") && !e.includes("Bad Request"))).toEqual([]);
});
