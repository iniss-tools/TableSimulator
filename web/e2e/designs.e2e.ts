// Editor vzhladov: novy vzhlad z kopie, uprava s nahladom, ulozenie do design.json a zmazanie.
import { expect, test } from "@playwright/test";

test("novy vzhlad, uprava a zmazanie", async ({ page }) => {
  await page.goto("/#/designs");
  await expect(page.locator("canvas")).toBeVisible();

  await page.getByRole("button", { name: "Nový vzhľad" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("Test E2E");
  await expect(dialog.getByLabel("ID (priečinok)")).toHaveValue("test-e2e");
  await dialog.getByRole("button", { name: "Vytvoriť" }).click();
  await expect(dialog).toBeHidden();
  await expect(page.locator("code", { hasText: /^test-e2e$/ })).toBeVisible();

  // uprava: rozostup bodov a farba pozadia pasov cela
  await page.getByLabel("Rozostup bodov").fill("7");
  await page.getByRole("textbox", { name: "Pozadie pásov" }).last().fill("#7a0000");
  await expect(page.getByText("Neuložené zmeny")).toBeVisible();
  await page.getByRole("button", { name: "Uložiť" }).click();
  await expect(page.getByText("Vzhľad je uložený a tabule sa prekreslili.")).toBeVisible();

  const design = (await (await page.request.get("/designs/test-e2e/design.json")).json()) as { pitch: number; header: { background: string } };
  expect(design.pitch).toBe(7);
  expect(design.header.background).toBe("#7a0000");

  await page.getByRole("button", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(page.locator("code", { hasText: /^elen-led$/ })).toBeVisible();
  const list = (await (await page.request.get("/api/designs")).json()) as { id: string }[];
  expect(list.some((d) => d.id === "test-e2e")).toBe(false);
});
