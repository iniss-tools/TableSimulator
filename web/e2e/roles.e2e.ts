// Vlastna rola: zalozenie s opravneniami, pridelenie pouzivatelovi, uprava, ochrana pouzivanej roly a odstranenie.
import { expect, test } from "@playwright/test";
import type { RoleDto } from "../src/api/generated/types.gen";
import { collectErrors } from "./helpers";

test("vlastna rola: opravnenia, pridelenie a odstranenie", async ({ page }) => {
  const errors = collectErrors(page);
  await page.goto("/#/roles");
  await expect(page.getByRole("row", { name: /Správca/ })).toContainText("všetky, aj budúce");

  await page.getByRole("button", { name: "Pridať rolu" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Názov").fill("Panely E2E");
  // prehlad ma kazda rola
  await expect(dialog.getByRole("checkbox", { name: "Prehľad" })).toBeChecked();
  await expect(dialog.getByRole("checkbox", { name: "Prehľad" })).toBeDisabled();
  await dialog.getByRole("checkbox", { name: "Informačné panely" }).click();
  await dialog.getByRole("button", { name: "Uložiť" }).click();
  await expect(dialog).toBeHidden();
  const row = page.getByRole("row", { name: /Panely E2E/ });
  await expect(row).toContainText("2 z 16");

  // pouzivatel s novou rolou
  await page.goto("/#/users");
  await page.getByRole("button", { name: "Pridať používateľa" }).click();
  const user = page.getByRole("dialog");
  await user.getByLabel("Prihlasovacie meno").fill("panely-e2e");
  await user.getByRole("combobox").click();
  await page.getByRole("option", { name: "Panely E2E" }).click();
  await expect(user.getByText("Smie: Prehľad, Informačné panely.")).toBeVisible();
  await user.getByLabel("Heslo", { exact: true }).fill("heslo-panelov");
  await user.getByLabel("Heslo znova").fill("heslo-panelov");
  await user.getByRole("button", { name: "Uložiť" }).click();
  await expect(user).toBeHidden();
  const userRow = page.getByRole("row", { name: /panely-e2e/ });
  await expect(userRow).toContainText("Panely E2E");

  // uprava roly: pridane statistiky
  await page.goto("/#/roles");
  await row.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Upraviť" }).click();
  await page.getByRole("dialog").getByRole("checkbox", { name: "Štatistiky" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Uložiť" }).click();
  await expect(row).toContainText("3 z 16");
  const roles = (await (await page.request.get("/api/roles")).json()) as RoleDto[];
  expect(roles.find((r) => r.displayName === "Panely E2E")).toMatchObject({ permissions: ["simulator.view", "statistics.view", "signage.manage"], users: 1 });

  // pouzivanu rolu odstranit nejde
  await row.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(page.getByText("Rolu majú pridelenú používatelia alebo prístupové kľúče")).toBeVisible();

  await page.goto("/#/users");
  await userRow.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(userRow).toBeHidden();

  await page.goto("/#/roles");
  await row.getByRole("button", { name: "Akcie" }).click();
  await page.getByRole("menuitem", { name: "Odstrániť" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odstrániť" }).click();
  await expect(row).toBeHidden();
  // odmietnute odstranenie (409) prehliadac zapise do konzoly
  expect(errors.filter((e) => !e.includes("409"))).toEqual([]);
});
