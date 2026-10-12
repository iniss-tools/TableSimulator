// Cela tabul: kopia vstavanej predlohy, uprava a ulozenie, predloha v okne tabule s cislom kolaje, odstranenie
// pouzivanej predlohy odmietnute, uprava cela tahanim v nahlade s krokmi spat a vodiacimi ciarami, zamok vo vrstvach a Ctrl+S,
// vyber viacerych prvkov (Ctrl+klik, obdlznik, Ctrl+A), spolocne pole, zarovnanie, presun a odstranenie skupiny,
// schranka medzi predlohami, kontextova ponuka, poradie klavesami, tahanie za popis, dvojklik, meranie s Alt a skratky.
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

test("editor cela v nahlade: vyber, tahanie, sipky, krok spat, zamok a Ctrl+S", async ({ page }) => {
  const errors = collectErrors(page);
  // vlastna kopia vstavanej odchodovej predlohy
  const faces = (await (await page.request.get("/api/faces")).json()) as { id: string }[];
  const source = faces.find((f) => f.id === "odchody")!;
  const created = await page.request.put("/api/faces/e2e-tahanie", {
    headers: API_HEADERS,
    data: { ...source, id: "e2e-tahanie", name: "E2E ťahanie", builtIn: false },
  });
  expect(created.ok()).toBe(true);

  await page.goto("/#/faces");
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "E2E ťahanie" }).click();
  const canvas = page.getByRole("group", { name: "Prvky čela v náhľade" });
  const title = canvas.getByRole("button", { name: "Text: ODCHODY / DEPARTURES" });
  await expect(title).toBeVisible();

  // tahanie nadpisu doprava - panel vlastnosti ukaze vybrany prvok s novym X
  const box = (await title.boundingBox())!;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  await page.mouse.down();
  await page.mouse.move(box.x + box.width / 2 + 20, box.y + box.height / 2, { steps: 4 });
  await page.mouse.move(box.x + box.width / 2 + 40, box.y + box.height / 2, { steps: 4 });
  await page.mouse.up();
  const properties = page.getByRole("complementary", { name: "Vlastnosti" });
  await expect(title).toHaveAttribute("aria-pressed", "true");
  await expect(properties).toContainText("ODCHODY / DEPARTURES");
  const x = properties.getByLabel("X", { exact: true });
  await expect(x).not.toHaveValue("0");
  await expect(page.getByText("Neuložené zmeny")).toBeVisible();

  // cele tahanie je jeden krok spat; sipka posunie o pol bodu
  await page.keyboard.press("Control+z");
  await expect(x).toHaveValue("0");
  await expect(page.getByText("Neuložené zmeny")).toBeHidden();
  await page.keyboard.press("ArrowRight");
  await expect(x).toHaveValue("0.5");
  await page.getByRole("button", { name: "Späť (Ctrl+Z)" }).click();
  await expect(x).toHaveValue("0");
  await page.getByRole("button", { name: "Znova (Ctrl+Y)" }).click();
  await expect(x).toHaveValue("0.5");
  await canvas.focus();
  await page.keyboard.press("Escape");
  await expect(x).toHaveCount(0);
  // bez vyberu panel ukazuje predlohu
  await expect(properties.getByLabel("Názov")).toHaveValue("E2E ťahanie");
  await page.getByRole("button", { name: "Zahodiť zmeny" }).click();

  // vodiace ciary: maly posun nadpisu sa prichyti spat na stred a pocas tahania je vidiet ciaru
  const center = (await title.boundingBox())!;
  await page.mouse.move(center.x + center.width / 2, center.y + center.height / 2);
  await page.mouse.down();
  await page.mouse.move(center.x + center.width / 2 + 4, center.y + center.height / 2, { steps: 4 });
  await expect(canvas.locator(".bg-pink-500").first()).toBeVisible();
  await page.mouse.up();
  await expect(canvas.locator(".bg-pink-500")).toHaveCount(0);
  await expect(x).toHaveValue("0");

  // zamok vo vrstvach: pas sa na platne neda chytit; ulozi sa s predlohou (Ctrl+S)
  const layers = page.getByRole("list", { name: "Vrstvy" });
  await layers.getByRole("listitem").filter({ hasText: "Plocha" }).getByRole("button", { name: "Zamknúť" }).click();
  await expect(canvas.getByRole("button", { name: /^Plocha/ })).toHaveCSS("pointer-events", "none");
  await page.keyboard.press("Control+s");
  await expect(page.getByText("Predloha je uložená a tabule sa prekreslili.")).toBeVisible();
  const saved = (await (await page.request.get("/api/faces")).json()) as { id: string; elements: { kind: string; locked: boolean }[] }[];
  expect(saved.find((f) => f.id === "e2e-tahanie")!.elements.find((e) => e.kind === "rect")!.locked).toBe(true);

  const removed = await page.request.delete("/api/faces/e2e-tahanie", { headers: API_HEADERS });
  expect(removed.status()).toBe(204);
  expect(errors).toEqual([]);
});

test("editor cela: vyber viacerych prvkov, spolocne pole, zarovnanie a skupina", async ({ page }) => {
  const errors = collectErrors(page);
  const faces = (await (await page.request.get("/api/faces")).json()) as { id: string }[];
  const source = faces.find((f) => f.id === "odchody")!;
  const created = await page.request.put("/api/faces/e2e-vyber", {
    headers: API_HEADERS,
    data: { ...source, id: "e2e-vyber", name: "E2E výber", builtIn: false },
  });
  expect(created.ok()).toBe(true);

  await page.goto("/#/faces");
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "E2E výber" }).click();
  const canvas = page.getByRole("group", { name: "Prvky čela v náhľade" });
  const properties = page.getByRole("complementary", { name: "Vlastnosti" });
  const title = canvas.getByRole("button", { name: "Text: ODCHODY / DEPARTURES" });
  const clock = canvas.getByRole("button", { name: /^Hodiny/ });
  const items = canvas.locator("[data-face-element]");
  await expect(items).toHaveCount(3);

  // Ctrl+klik prida hodiny k nadpisu: rozne druhy, rozne X ako "Rozne"
  await title.click();
  await clock.click({ modifiers: ["Control"] });
  await expect(properties).toContainText("2 prvky");
  await expect(properties).toContainText("Rôzne druhy");
  const x = properties.getByLabel("X", { exact: true });
  const y = properties.getByLabel("Y", { exact: true });
  await expect(x).toHaveValue("");
  await expect(x).toHaveAttribute("placeholder", "Rôzne");

  // spolocne pole zmeni oba prvky
  await y.fill("2");
  await expect(y).toHaveValue("2");
  await title.click();
  await expect(y).toHaveValue("2");
  await clock.click({ modifiers: ["Shift"] });
  await expect(properties).toContainText("2 prvky");

  // zarovnanie vlavo: lavy okraj hodin na lavom okraji nadpisu, krok spat ho vrati
  const before = (await clock.boundingBox())!;
  await properties.getByRole("button", { name: "Zarovnať vľavo" }).click();
  await expect.poll(async () => Math.round((await clock.boundingBox())!.x)).toBe(Math.round((await title.boundingBox())!.x));
  await page.keyboard.press("Control+z");
  await expect.poll(async () => Math.round((await clock.boundingBox())!.x)).toBe(Math.round(before.x));
  // rozmiestnenie potrebuje aspon tri prvky
  await expect(properties.getByRole("button", { name: "Rozmiestniť vodorovne" })).toBeDisabled();

  // obdlznik od laveho dolneho rohu cez celu tabulu vyberie vsetko
  await canvas.focus();
  await page.keyboard.press("Escape");
  const area = (await canvas.boundingBox())!;
  await page.mouse.move(area.x + 3, area.y + area.height - 3);
  await page.mouse.down();
  await page.mouse.move(area.x + area.width / 2, area.y + area.height / 2, { steps: 4 });
  await page.mouse.move(area.x + area.width - 3, area.y + 3, { steps: 4 });
  await page.mouse.up();
  await expect(canvas.locator("[data-face-element][aria-pressed=true]")).toHaveCount(3);
  await expect(properties).toContainText("3 prvky");
  await expect(properties.getByRole("button", { name: "Rozmiestniť vodorovne" })).toBeEnabled();

  // tahanie nadpisu posunie celu skupinu (Alt = bez prichytenia)
  const titleBefore = (await title.boundingBox())!;
  const clockBefore = (await clock.boundingBox())!;
  await page.keyboard.down("Alt");
  await page.mouse.move(titleBefore.x + titleBefore.width / 2, titleBefore.y + titleBefore.height / 2);
  await page.mouse.down();
  await page.mouse.move(titleBefore.x + titleBefore.width / 2 - 15, titleBefore.y + titleBefore.height / 2, { steps: 4 });
  await page.mouse.move(titleBefore.x + titleBefore.width / 2 - 30, titleBefore.y + titleBefore.height / 2, { steps: 4 });
  await page.mouse.up();
  await page.keyboard.up("Alt");
  const titleAfter = (await title.boundingBox())!;
  const clockAfter = (await clock.boundingBox())!;
  expect(titleAfter.x - titleBefore.x).toBeLessThan(-20);
  expect(Math.abs(clockAfter.x - clockBefore.x - (titleAfter.x - titleBefore.x))).toBeLessThan(1.5);

  // Ctrl+A a Delete odstrani vsetky odomknute prvky, krok spat ich vrati
  await canvas.focus();
  await page.keyboard.press("Control+a");
  await expect(properties).toContainText("3 prvky");
  await page.keyboard.press("Delete");
  await expect(items).toHaveCount(0);
  await page.keyboard.press("Control+z");
  await expect(items).toHaveCount(3);
  await page.getByRole("button", { name: "Zahodiť zmeny" }).click();

  const removed = await page.request.delete("/api/faces/e2e-vyber", { headers: API_HEADERS });
  expect(removed.status()).toBe(204);
  expect(errors).toEqual([]);
});

test("editor cela: schranka, kontextova ponuka, poradie, tahanie za popis, dvojklik, meranie a skratky", async ({ page }) => {
  const errors = collectErrors(page);
  const faces = (await (await page.request.get("/api/faces")).json()) as { id: string }[];
  const source = faces.find((f) => f.id === "odchody")!;
  const created = await page.request.put("/api/faces/e2e-schranka", {
    headers: API_HEADERS,
    data: { ...source, id: "e2e-schranka", name: "E2E schránka", builtIn: false },
  });
  expect(created.ok()).toBe(true);

  await page.goto("/#/faces");
  const canvas = page.getByRole("group", { name: "Prvky čela v náhľade" });
  const properties = page.getByRole("complementary", { name: "Vlastnosti" });
  const items = canvas.locator("[data-face-element]");
  const layers = page.getByRole("list", { name: "Vrstvy" });

  // kopia zo vstavanej predlohy cez kontextovu ponuku (vstavana sa neda menit - vlozit je vypnute)
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: /^Odchodová – ODCHODY/ }).click();
  const builtInClock = canvas.getByRole("button", { name: /^Hodiny/ });
  await builtInClock.click({ button: "right" });
  await expect(page.getByRole("menuitem", { name: "Vložiť" })).toBeDisabled();
  await page.getByRole("menuitem", { name: "Kopírovať" }).click();

  // vlozenie do vlastnej predlohy navrch, vybrane
  await page.getByRole("combobox", { name: "Predloha čela" }).click();
  await page.getByRole("option", { name: "E2E schránka" }).click();
  await expect(items).toHaveCount(3);
  await canvas.focus();
  await page.keyboard.press("Control+v");
  await expect(items).toHaveCount(4);
  await expect(layers.getByRole("listitem").first()).toContainText("Hodiny");
  await expect(properties).toContainText("Hodiny");

  // Ctrl+D duplikuje, Ctrl+Shift+[ da vyber naspodok, krok spat vrati
  await page.keyboard.press("Control+d");
  await expect(items).toHaveCount(5);
  await page.keyboard.press("Control+Shift+BracketLeft");
  await expect(layers.getByRole("listitem").last()).toContainText("Hodiny");
  await page.keyboard.press("Control+z");
  await expect(layers.getByRole("listitem").first()).toContainText("Hodiny");

  // prave tlacidlo na nadpis ho vyberie (kym je ponuka otvorena, zvysok stranky je pre citacky skryty); vystrihnut a vlozit
  const title = canvas.getByRole("button", { name: "Text: ODCHODY / DEPARTURES" });
  await title.click({ button: "right" });
  await page.getByRole("menuitem", { name: "Vystrihnúť" }).click();
  await expect(page.getByRole("menu")).toBeHidden();
  await expect(items).toHaveCount(4);
  await page.keyboard.press("Control+v");
  await expect(items).toHaveCount(5);
  await expect(title).toHaveAttribute("aria-pressed", "true");

  // tahanie za popis X zmeni hodnotu o pol bodu na 2 px, cele tahanie je jeden krok
  const x = properties.getByLabel("X", { exact: true });
  const start = Number(await x.inputValue());
  const caption = properties.locator("label").filter({ hasText: /^X$/ });
  const at = (await caption.boundingBox())!;
  await page.mouse.move(at.x + 2, at.y + at.height / 2);
  await page.mouse.down();
  await page.mouse.move(at.x + 12, at.y + at.height / 2, { steps: 3 });
  await page.mouse.move(at.x + 22, at.y + at.height / 2, { steps: 3 });
  await page.mouse.up();
  await expect(x).toHaveValue(String(start + 5));
  await expect(x).not.toBeFocused();
  await page.getByRole("button", { name: "Späť (Ctrl+Z)" }).click();
  await expect(x).toHaveValue(String(start));

  // dvojklik na text ho da do pola na upravu
  await title.dblclick();
  await expect(properties.getByLabel("Text", { exact: true })).toBeFocused();

  // meranie s Alt: od nadpisu k hodinam
  await canvas.focus();
  const clock = canvas.getByRole("button", { name: /^Hodiny/ }).first();
  const box = (await clock.boundingBox())!;
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  await page.keyboard.down("Alt");
  await page.mouse.move(box.x + box.width / 2 + 1, box.y + box.height / 2);
  await expect(canvas.locator("span.bg-pink-500").first()).toBeVisible();
  await page.keyboard.up("Alt");
  await expect(canvas.locator("span.bg-pink-500")).toHaveCount(0);

  // skratky klavesom ?
  await page.keyboard.press("Shift+Slash");
  const dialog = page.getByRole("dialog", { name: "Klávesové skratky" });
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText("Duplikovať");
  await page.keyboard.press("Escape");
  await expect(dialog).toBeHidden();

  await page.getByRole("button", { name: "Zahodiť zmeny" }).click();
  const removed = await page.request.delete("/api/faces/e2e-schranka", { headers: API_HEADERS });
  expect(removed.status()).toBe(204);
  expect(errors).toEqual([]);
});
