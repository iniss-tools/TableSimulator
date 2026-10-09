// Porovnanie zaznamov: dva nahrate zaznamy s inym textom tabule - odlisna tabula, text oboch stavov a tabula v case.
import { expect, test, type Page } from "@playwright/test";
import { API_HEADERS, collectErrors, elenRow } from "./helpers";

const LINE = 64;

/** Zaznam .tsrec: hlavicka s linkou ELEN a riadky textu tabule 3 v danych casoch. */
function recording(created: string, rows: [number, string][]): string {
  const header = {
    format: "TableSimulator.Recording",
    version: 1,
    created,
    config: {
      lines: [{ number: LINE, tableClass: 4, endpoint: `TCP://0.0.0.0:${47000 + LINE}`, name: "", mode: "display", autoLearn: true, manufacturer: "ELEN" }],
      boards: [],
    },
    note: null,
  };
  const hex = (bytes: number[]) =>
    bytes
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("")
      .toUpperCase();
  return [JSON.stringify(header), ...rows.map(([t, text]) => JSON.stringify({ t, line: LINE, rx: hex(elenRow(3, 1, text)) }))].join("\n") + "\n";
}

async function upload(page: Page, name: string, content: string) {
  const response = await page.request.post(`/api/recordings/upload?name=${name}.tsrec`, {
    headers: { ...API_HEADERS, "Content-Type": "application/octet-stream" },
    data: Buffer.from(content),
  });
  expect(response.ok(), await response.text()).toBe(true);
}

test("porovnanie dvoch zaznamov", async ({ page }) => {
  const errors = collectErrors(page);
  await upload(
    page,
    "e2e-pred",
    recording("2026-10-09T12:00:00+00:00", [
      [1, "OS 3005 ZILINA"],
      [20, "R 611 PUCHOV"],
    ]),
  );
  await upload(
    page,
    "e2e-po",
    recording("2026-10-09T12:30:00+00:00", [
      [4, "OS 3005 ZILINA"],
      [25, "R 611 TRENCIN"],
    ]),
  );

  await page.goto("/#/recordings/compare?a=e2e-pred&b=e2e-po");
  await expect(page.getByText("odlišné: 1")).toBeVisible();
  // rovnaky prvy stav je zbaleny, rozdiel vedla seba
  await expect(page.getByText("1× rovnaký text")).toBeVisible();
  await expect(page.getByText("R 611 PUCHOV")).toBeVisible();
  await expect(page.getByText("R 611 TRENCIN")).toBeVisible();

  await page.getByRole("button", { name: "Zobraziť tabuľu v tomto okamihu" }).first().click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.locator("canvas")).toHaveCount(2);
  await dialog.getByRole("button", { name: "Zavrieť" }).first().click();

  for (const name of ["e2e-pred", "e2e-po"]) await page.request.delete(`/api/recordings/${name}`, { headers: API_HEADERS });
  expect(errors).toEqual([]);
});
