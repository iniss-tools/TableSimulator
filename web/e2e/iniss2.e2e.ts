// Server INISS2 v okne Nacitat z INISSu: nahlad liniek, nacitanie s celom a presmerovanie liniek aj jeho zrusenie.
import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { expect, test, type Page } from "@playwright/test";
import type { BoardDto, LineDto, SettingsDto } from "../src/api/generated/types.gen";
import { writeIniss2Install } from "../docshots/iniss2-install";
import { API_HEADERS, collectErrors, deleteLine, openPage } from "./helpers";

async function setProgram(page: Page, settings: SettingsDto, inissProgram: string) {
  const response = await page.request.put("/api/settings", { headers: API_HEADERS, data: { ...settings, inissProgram } });
  expect(response.ok(), await response.text()).toBe(true);
}

async function state(page: Page) {
  return (await (await page.request.get("/api/state")).json()) as { lines: LineDto[]; boards: BoardDto[] };
}

async function openDialog(page: Page) {
  await page.getByRole("button", { name: "Načítať z INISSu" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toContainText("server INISS2 (IniSrv)");
  return dialog;
}

test("server INISS2: nacitanie liniek a ich presmerovanie", async ({ page }) => {
  const errors = collectErrors(page);
  const folder = mkdtempSync(join(tmpdir(), "e2e-iniss2-"));
  const { program, commLines } = writeIniss2Install(folder);
  const original = readFileSync(commLines);

  await page.goto("/#/");
  const settings = (await (await page.request.get("/api/settings")).json()) as SettingsDto;
  await setProgram(page, settings, program);

  try {
    await openPage(page, "Linky");

    // nahlad: linka UDP, seriova linka ide cez TCP a linku zvuku simulator nepozna
    let dialog = await openDialog(page);
    await expect(dialog.getByText("v simulátore UDP 47001")).toBeVisible();
    await expect(dialog.getByText("v simulátore TCP 47002")).toBeVisible();
    await expect(dialog.getByRole("row", { name: /Lipovec_Rozhlas/ })).toContainText("trieda, ktorú simulátor nepozná");

    // nacitanie: linky, tabule a celo z udajov stanice
    await dialog.getByRole("button", { name: "Načítať do simulátora" }).click();
    await expect(page.getByText("Linky a tabule z INISSu načítané")).toBeVisible();
    const loaded = await state(page);
    expect(loaded.lines.find((l) => l.number === 1)?.endpoint).toBe("UDP://0.0.0.0:47001");
    expect(loaded.lines.find((l) => l.number === 2)?.endpoint).toBe("TCP://0.0.0.0:47002");
    const platform = loaded.boards.find((b) => b.id === "1-2");
    expect(platform?.manufacturer).toBe("LCD1");
    expect(platform?.profile.face).toBeTruthy();
    expect(platform?.profile.width).toBe(184);
    expect(loaded.boards.find((b) => b.id === "2-10")?.profile.width).toBe(272);

    // presmerovanie zmeni adresy, pri seriovej linke aj sposob spojenia
    dialog = await openDialog(page);
    await dialog.getByRole("button", { name: "Presmerovať linky" }).click();
    await page.getByRole("alertdialog").getByRole("button", { name: "Presmerovať linky" }).click();
    await expect(page.getByText("Linky sú presmerované. Reštartujte server INISS2.")).toBeVisible();
    await expect(dialog.getByText("pôvodne 10.0.0.11:10001")).toBeVisible();
    await expect(dialog.getByText("pôvodne COM3")).toBeVisible();
    const redirected = readFileSync(commLines, "latin1");
    expect(redirected).toContain('line="127.0.0.1:47001"');
    expect(redirected).toContain('typ="TCP_CLIENT" protocol="LCD" line="127.0.0.1:47002"');
    expect(redirected).toContain('line="10.0.0.12:2000"');

    // zrusenie vrati subor presne do povodnej podoby
    await dialog.getByRole("button", { name: "Zrušiť presmerovanie" }).click();
    await page.getByRole("alertdialog").getByRole("button", { name: "Zrušiť presmerovanie" }).click();
    await expect(page.getByText("Pôvodné adresy sú vrátené. Reštartujte server INISS2.")).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Presmerovať linky" })).toBeVisible();
    expect(readFileSync(commLines).equals(original)).toBe(true);
  } finally {
    await setProgram(page, settings, settings.inissProgram);
    await deleteLine(page, 1);
    await deleteLine(page, 2);
    rmSync(folder, { recursive: true, force: true });
  }

  expect(errors).toEqual([]);
});
