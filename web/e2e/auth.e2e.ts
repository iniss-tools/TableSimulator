// Prihlasovanie (server so zapnutym prihlasovanim): zalozenie prveho spravcu, odhlasenie, zle a spravne heslo.
import { expect, test } from "@playwright/test";
import { E2E_SERVERS } from "./servers";

test.use({ baseURL: E2E_SERVERS.login.url });

test("prvy spravca, odhlasenie a prihlasenie", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText("Prvý správca")).toBeVisible();
  await page.getByLabel("Prihlasovacie meno").fill("spravca");
  await page.getByLabel("Meno", { exact: true }).fill("Správca E2E");
  await page.getByLabel("Heslo", { exact: true }).fill("heslo-e2e-1");
  await page.getByLabel("Heslo znova").fill("heslo-e2e-1");
  await page.getByRole("button", { name: "Založiť správcu" }).click();
  await expect(page.getByRole("link", { name: "Tabule", exact: true })).toBeVisible();

  await page.getByRole("button", { name: /Správca E2E/ }).click();
  await page.getByRole("menuitem", { name: "Odhlásiť" }).click();
  await expect(page.getByText("Prihlásenie", { exact: true })).toBeVisible();

  await page.getByLabel("Prihlasovacie meno").fill("spravca");
  await page.getByLabel("Heslo", { exact: true }).fill("zle-heslo");
  await page.getByRole("button", { name: "Prihlásiť" }).click();
  await expect(page.getByText("Nesprávne meno alebo heslo.")).toBeVisible();

  await page.getByLabel("Heslo", { exact: true }).fill("heslo-e2e-1");
  await page.getByRole("button", { name: "Prihlásiť" }).click();
  await expect(page.getByRole("link", { name: "Tabule", exact: true })).toBeVisible();
});

test("vlastna rola: menu len so strankami, na ktore ma pouzivatel opravnenie", async ({ page }) => {
  // spravca z predchadzajuceho testu zalozi rolu a pouzivatela cez API
  const headers = { "X-Requested-With": "TableSimulator" };
  expect((await page.request.post("/api/auth/login", { headers, data: { userName: "spravca", password: "heslo-e2e-1", remember: false } })).ok()).toBe(true);
  const role = await page.request.post("/api/roles", { headers, data: { displayName: "Panely", permissions: ["simulator.view", "signage.manage"] } });
  expect(role.ok()).toBe(true);
  const { name } = (await role.json()) as { name: string };
  const user = await page.request.post("/api/users", { headers, data: { userName: "panely", displayName: "", role: name, password: "heslo-panely-1" } });
  expect(user.ok()).toBe(true);
  await page.request.post("/api/auth/logout", { headers });

  await page.goto("/");
  await page.getByLabel("Prihlasovacie meno").fill("panely");
  await page.getByLabel("Heslo", { exact: true }).fill("heslo-panely-1");
  await page.getByRole("button", { name: "Prihlásiť" }).click();
  await expect(page.getByRole("link", { name: "Tabule", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Správy", exact: true })).toBeVisible();
  for (const hidden of ["Štatistiky", "Archív tabúľ", "Používatelia", "Roly", "Audit", "Nastavenia"])
    await expect(page.getByRole("link", { name: hidden, exact: true })).toBeHidden();
  await expect(page.getByRole("button", { name: /Panely/ })).toBeVisible();

  await page.goto("/#/settings");
  await expect(page.getByText("Na túto stránku nemáte oprávnenie.")).toBeVisible();
});
