// Upozornenie na vypadok linky: INISS sa pripoji a odpoji - prehlad liniek nad tabulami, pocet v menu, oznamenie;
// po navrate oznamenie potvrdi spojenie a pocet zmizne.
import { connect } from "node:net";
import { expect, test } from "@playwright/test";
import { collectErrors, createLine, deleteLine, elenRow } from "./helpers";

const LINE = 62;
const PORT = 47000 + LINE;

/** Spojenie ako INISS: posle riadok a caka na ACK; zavretie = odpojenie INISSu. */
async function connectIniss() {
  const socket = connect(PORT, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  const reply = new Promise<Buffer>((resolve) => socket.once("data", resolve));
  socket.write(Buffer.from(elenRow(2, 1, "VYPADOK")));
  expect([...(await reply)]).toContain(0x06);
  return socket;
}

test("vypadok a navrat linky", async ({ page }) => {
  const errors = collectErrors(page);
  await createLine(page, LINE, "Výpadok");

  await page.goto("/#/");
  const chip = page.getByRole("button", { name: /Linka 62 – Výpadok/ });
  await expect(chip).toContainText("čaká na INISS");

  const first = await connectIniss();
  await expect(chip).toContainText("INISS pripojený");
  await expect(chip).toContainText("posledná správa");
  // ine linky mozu mat vypadok uz pred testom (napr. obsadeny port predvolenej linky) - pocita sa len zmena
  const badge = page.locator("[data-sidebar=menu-badge]");
  const base = (await badge.count()) > 0 ? Number(await badge.textContent()) : 0;

  first.destroy();
  await expect(chip).toContainText("INISS sa odpojil");
  await expect(page.getByText("Linka 62 – Výpadok: INISS sa odpojil")).toBeVisible();
  await expect(badge).toHaveText(String(base + 1));

  // klik vyberie tabule linky
  await chip.click();
  await expect(chip).toHaveAttribute("aria-pressed", "true");
  await expect(page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 2` })).toBeVisible();

  const second = await connectIniss();
  await expect(page.getByText("Linka 62 – Výpadok: INISS je znova pripojený")).toBeVisible();
  if (base > 0) await expect(badge).toHaveText(String(base));
  else await expect(badge).toBeHidden();
  second.destroy();

  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
});
