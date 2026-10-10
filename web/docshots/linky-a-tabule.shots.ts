// Snimky kapitoly Linky a tabule: okno linky, karta a okno tabule, podporovane tabule, listy FERS, vzhlady, poruchy
// a scenare.
import { expect, test, type Page } from "@playwright/test";
import { API_HEADERS } from "../e2e/helpers";
import type { BoardDto, BoardFaultDto, StateDto } from "../src/api/generated/types.gen";
import { open, shot, THEMES } from "./shoot";

const card = (page: Page, name: string) => page.locator('[data-slot="card"]').filter({ has: page.getByText(name, { exact: true }) });

/** Okno tabule z jej karty na stranke Tabule. */
async function openBoard(page: Page, name: string) {
  await card(page, name).getByRole("button", { name: "Upraviť" }).click();
  return page.getByRole("dialog");
}

/** Nastavi tabuli poruchu cez API (ako vyber na karte). */
async function setFault(page: Page, name: string, fault: BoardFaultDto) {
  const state = (await (await page.request.get("/api/state")).json()) as StateDto;
  const b = state.boards.find((x: BoardDto) => x.name === name)!;
  const response = await page.request.put(`/api/boards/${b.line}/${b.address}`, {
    headers: API_HEADERS,
    data: {
      line: b.line,
      address: b.address,
      manufacturer: b.manufacturer,
      name: b.name,
      profile: b.profile,
      design: b.design,
      fault,
      secondAddress: b.secondAddress,
    },
  });
  expect(response.ok(), await response.text()).toBe(true);
}

for (const theme of THEMES) {
  test(`linky (${theme})`, async ({ page }) => {
    await open(page, "/lines", theme);
    await page.getByRole("row", { name: /Hala/ }).getByRole("button", { name: "Akcie" }).click();
    await page.getByRole("menuitem", { name: "Upraviť" }).click();
    const dialog = page.getByRole("dialog");
    await shot(page, "linky/okno-linky", theme, { target: dialog, ready: dialog.getByLabel("Číslo linky (1–100)") });
  });

  test(`tabule (${theme})`, async ({ page }) => {
    await open(page, "/", theme);
    await shot(page, "tabule/karta", theme, { target: card(page, "Odchody – hala") });
    const dialog = await openBoard(page, "Odchody – hala");
    await shot(page, "tabule/okno-tabule", theme, { target: dialog, ready: dialog.getByLabel("Názov") });
    await dialog.getByRole("tab", { name: "Profil" }).click();
    await shot(page, "tabule/profil", theme, { target: dialog, ready: dialog.getByLabel("Šírka [body]") });
  });

  test(`podporovane-tabule (${theme})`, async ({ page }) => {
    await open(page, "/", theme);
    await expect(page.getByText("Odchody – listová")).toBeVisible();
    await shot(page, "podporovane-tabule/tabule", theme, { height: 1900 });
    const dialog = await openBoard(page, "Odchody – listová");
    await dialog.getByRole("tab", { name: "Listy" }).click();
    await shot(page, "podporovane-tabule/listy", theme, { target: dialog });
  });

  test(`vzhlady (${theme})`, async ({ page }) => {
    await open(page, "/designs", theme);
    await shot(page, "vzhlady/editor", theme, { ready: page.getByText("Náhľad", { exact: true }).first() });
  });

  test(`poruchy-a-scenare (${theme})`, async ({ page }) => {
    await setFault(page, "Podchod – odchody", { kind: "status", delayMs: 0, status: 0x40 });
    try {
      await open(page, "/", theme);
      await shot(page, "poruchy-a-scenare/porucha", theme, { target: card(page, "Podchod – odchody") });
    } finally {
      await setFault(page, "Podchod – odchody", { kind: "none", delayMs: 0, status: 0 });
    }
    await open(page, "/scenarios", theme);
    await expect(page.getByText("Výpadky na nástupištiach")).toBeVisible();
    await shot(page, "poruchy-a-scenare/scenare", theme);
    await card(page, "Výpadky na nástupištiach").getByRole("button", { name: "Upraviť" }).click();
    const dialog = page.getByRole("dialog");
    await shot(page, "poruchy-a-scenare/okno-scenara", theme, { target: dialog, ready: dialog.getByText("Kroky") });
  });
}
