// Osvetlenie a rozhlas: ustredna AK Signal (snimac tmy, server zapne automatiku, porucha svetiel v odpovedi), krok scenara
// so stavom ustredne a rozhlas vzdialeneho INISSu (hlasenie, prerusenie = odpoved bez stavu fronty).
import { expect, test } from "@playwright/test";
import {
  akSignalFrame,
  API_HEADERS,
  collectErrors,
  createBoard,
  createLine,
  deleteLine,
  openLine,
  openPage,
  remoteQueueMessage,
  remoteQueueState,
} from "./helpers";

const AK_LINE = 66;
const REMOTE_LINE = 67;

/** Prikazy AKSIGNAL: dotaz na svetlo a zapnutie automatiky. */
const GET_LIGHT = 0x20;
const SET_LIGHT = 0x22;

test("ustredna AK Signal a krok scenara so stavom ustredne", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await createLine(page, AK_LINE, "E2E svetlá", 131, "AKSIGNAL");
  await createBoard(page, AK_LINE, 5, "AKSIGNAL", "Nástupište E2E");
  // server INISS2: stav svetla z odpovede AKSIGNAL (00 05 prikaz stav sucet)
  const server = await openLine(47000 + AK_LINE);
  const lightState = async (command: number) => (await server.ask(akSignalFrame(5, command)))[3];

  await openPage(page, "Osvetlenie a rozhlas");
  const card = page.locator("[data-slot=card]").filter({ hasText: "Nástupište E2E" }).first();
  await card.getByRole("combobox", { name: "Snímač tmy" }).click();
  await page.getByRole("option", { name: "Stále tma" }).click();

  // snimac hlasi tmu (tma + svetla v poriadku), potom server zapne automatiku - v tme svieti
  await expect.poll(() => lightState(GET_LIGHT)).toBe(0x0c);
  expect(await lightState(SET_LIGHT)).toBe(0x0d);
  await expect(card.getByText("Svieti", { exact: true })).toBeVisible();

  // porucha svetiel z nastavenia - server ju vidi v odpovedi
  await card.getByRole("checkbox", { name: "Porucha svetiel" }).click();
  await expect.poll(() => lightState(GET_LIGHT)).toBe(0x05);
  await card.getByRole("checkbox", { name: "Porucha svetiel" }).click();
  await expect.poll(() => lightState(GET_LIGHT)).toBe(0x0d);

  // krok scenara: porucha svetiel na vsetkych ustredniach linky
  await openPage(page, "Scenáre porúch");
  await page.getByRole("button", { name: "Nový scenár" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("E2E ústredňa");
  await dialog.getByRole("button", { name: "Pridať krok" }).click();
  await dialog.getByRole("combobox", { name: "Linka" }).click();
  await page.getByRole("option", { name: `${AK_LINE} – E2E svetlá` }).click();
  await dialog.getByRole("combobox", { name: "Čo sa stane" }).click();
  await page.getByRole("option", { name: "Stav ústredne AK Signal" }).click();
  await expect(dialog.getByRole("checkbox", { name: "Porucha svetiel" })).toBeChecked();
  await dialog.getByLabel("Trvá [s]").fill("120");
  await expect(dialog.getByText(`Linka ${AK_LINE} · všetky ústredne – Ústredňa: porucha svetiel`)).toBeVisible();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();

  const scenario = page.locator("[data-slot=card]").filter({ hasText: "E2E ústredňa" });
  await scenario.getByRole("button", { name: "Spustiť" }).click();
  await expect.poll(() => lightState(GET_LIGHT)).toBe(0x05);
  await openPage(page, "Osvetlenie a rozhlas");
  await expect(card.getByText("scenár", { exact: true })).toBeVisible();
  // nastavenie ustredne sa nemeni
  await expect(card.getByRole("checkbox", { name: "Porucha svetiel" })).not.toBeChecked();

  await openPage(page, "Scenáre porúch");
  await scenario.getByRole("button", { name: "Zastaviť" }).click();
  await expect.poll(() => lightState(GET_LIGHT)).toBe(0x0d);
  await scenario.getByRole("button", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(scenario).toBeHidden();

  server.close();
  await deleteLine(page, AK_LINE);
  expect(errors).toEqual([]);
});

test("rozhlas vzdialeneho INISSu - hlasenie a prerusenie", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await createLine(page, REMOTE_LINE, "E2E vzdialený rozhlas", 128);
  const controller = await openLine(47000 + REMOTE_LINE);
  const queueState = async (frame: number[]) => remoteQueueState(await controller.ask(frame));

  // hlasenie s dlhou pauzou - hra, kym ho obsluha neprerusi
  expect(await queueState(remoteQueueMessage(1, 0, "SK/R1/9900100,0,600000", "Os 3412 Lipovec"))).toBe(0x103);
  expect(await queueState(remoteQueueMessage(2, 1))).toBe(0x503);

  await openPage(page, "Osvetlenie a rozhlas");
  const card = page.locator("[data-slot=card]").filter({ hasText: `linka ${REMOTE_LINE} · vzdialený INISS` });
  await expect(card.getByText("Os 3412 Lipovec").first()).toBeVisible();
  await card.getByRole("button", { name: "Prerušiť" }).click();
  await expect(card.getByText("prerušené")).toBeVisible();

  // riadiaci INISS dostane raz stav fronty 0 (remote break), potom volnu frontu
  expect(await queueState(remoteQueueMessage(3, 1))).toBe(0);
  expect(await queueState(remoteQueueMessage(4, 0))).toBe(1);
  const board = (await (await page.request.get(`/api/boards/${REMOTE_LINE}-0`, { headers: API_HEADERS })).json()) as {
    display: { remoteAudio: { announcements: { result: string }[] } };
  };
  expect(board.display.remoteAudio.announcements.map((a) => a.result)).toEqual(["cancelled"]);

  controller.close();
  await deleteLine(page, REMOTE_LINE);
  expect(errors).toEqual([]);
});
