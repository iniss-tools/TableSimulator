// Listova tabula FERS: stanica so zoznamami listov (ako ju posle GVDEditor), INISS zapise kody listov - karta tabule,
// profil so zoznamami a zaznam komunikacie; kym sa listy tocia, tabula na dotaz mlci.
import { connect } from "node:net";
import { expect, test } from "@playwright/test";
import { apiPost, collectErrors, deleteLine, openPage } from "./helpers";

const LINE = 64;
const PORT = 47000 + LINE;

/** Sprava Elektrocasu (FERS): adresa, dlzka, pocitac 0, prikaz, parametre a sucet. */
function fersFrame(address: number, command: number, params: number[]): number[] {
  const frame = [address, params.length + 5, 0, command, ...params];
  return [...frame, frame.reduce((a, b) => a + b, 0) & 0xff];
}

/** Posle spravu a vrati odpoved, ktora pride do limitu (prazdna = tabula mlci). */
async function send(frame: number[], wait = 400): Promise<number[]> {
  const socket = connect(PORT, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  const reply: number[] = [];
  socket.on("data", (data: Buffer) => reply.push(...data));
  socket.write(Buffer.from(frame));
  await new Promise((r) => setTimeout(r, wait));
  socket.destroy();
  return reply;
}

test("listova tabula FERS so zoznamami listov", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/");
  await apiPost(page, "/api/station", {
    lines: [{ number: LINE, tableClass: 3, port: PORT, name: "E2E listová" }],
    boards: [
      {
        line: LINE,
        address: 20,
        manufacturer: "FERS",
        name: "Listy",
        flapRecords: 4,
        flaps: {
          linesPerRecord: 1,
          modules: [
            { line: 0, position: 0, span: 1, list: "Ciel" },
            { line: 0, position: 1, span: 1, list: "Druh" },
          ],
          lists: { Ciel: { B: "*BRATISLAVA", C: "BUDAPEST", c: "*BUDAPEST" }, Druh: { R: "*R", O: "Os" } },
        },
      },
    ],
  });

  // zapis potvrdi stavom 70, kym sa listy tocia, na dotaz 01 tabula mlci
  expect(await send(fersFrame(20, 0x10, [0, 0, ..."cR".split("").map((c) => c.charCodeAt(0))]))).toEqual([0x00, 0x06, 0x14, 0x70, 0x00, 0x8a]);
  expect(await send(fersFrame(20, 0x01, []), 300)).toEqual([]);

  await openPage(page, "Tabule");
  const card = page.locator("[data-slot=card]").filter({ hasText: `linka ${LINE} · adresa 20` });
  await expect(card).toContainText("FERS");
  await expect(card.locator("canvas")).toBeVisible();
  await expect(card).toContainText("listy sa otáčajú");

  await card.getByRole("button", { name: "Upraviť" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("tab", { name: "Profil" }).click();
  await expect(dialog.getByText("Moduly: 2, zoznamy listov: 2")).toBeVisible();
  await expect(dialog.getByLabel("Otáčanie listov (ms)")).toHaveValue("3000");

  // farba listu: Os zlty, cely zoznam cielov biely
  await dialog.getByRole("tab", { name: "Listy" }).click();
  await dialog.getByRole("combobox", { name: "Zoznam listov" }).click();
  await page.getByRole("option", { name: "Druh" }).click();
  await dialog.getByRole("combobox", { name: "Farba listu O" }).click();
  await page.getByRole("option", { name: "žltý" }).click();
  await dialog.getByRole("combobox", { name: "Zoznam listov" }).click();
  await page.getByRole("option", { name: "Ciel" }).click();
  await dialog.getByRole("combobox", { name: "Farba všetkých listov zoznamu" }).click();
  await page.getByRole("option", { name: "biely" }).click();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  const state = (await (await page.request.get("/api/state")).json()) as { boards: { id: string; profile: { flaps: { colors: unknown } } }[] };
  expect(state.boards.find((b) => b.id === `${LINE}-20`)?.profile.flaps.colors).toEqual({
    Druh: { O: "yellow" },
    Ciel: { B: "white", C: "white", c: "white" },
  });

  await openPage(page, "Komunikácia");
  await expect(page.getByText("Zápis listov: riadok 0, pozícia 0: „cR“")).toBeVisible();
  await expect(page.getByText(/ešte sa otáčajú, tabuľa mlčí/).first()).toBeVisible();

  await deleteLine(page, LINE);
  expect(errors).toEqual([]);
});
