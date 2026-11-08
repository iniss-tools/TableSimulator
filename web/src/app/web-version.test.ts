import { expect, test } from "bun:test";
import { entryScript, shouldReload } from "./web-version";

test("vstupny skript zostavy zo stranky servera", () => {
  const html = `<head>
    <link rel="modulepreload" crossorigin href="/assets/vendor-A1.js">
    <script type="module" crossorigin src="/assets/index-C_fDWT8z.js"></script>
    <link rel="stylesheet" crossorigin href="/assets/index-B2.css">
  </head>`;
  expect(entryScript(html)).toBe("/assets/index-C_fDWT8z.js");
  expect(entryScript('<script type="module" src="/src/main.tsx"></script>')).toBeNull();
});

test("kiosk sa nacita znova raz pre kazdu verziu", () => {
  expect(shouldReload("/assets/index-B.js", null)).toBe(true);
  expect(shouldReload("/assets/index-B.js", "/assets/index-B.js")).toBe(false);
  expect(shouldReload("/assets/index-C.js", "/assets/index-B.js")).toBe(true);
});
