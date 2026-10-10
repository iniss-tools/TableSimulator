// Snimky webu simulatora do dokumentacie (iniss-tools-docs/static/img/tablesimulator) proti serveru s ukazkovymi
// datami. Spustenie: bun run docshots (zostavi web aj server); len niektore snimky: bun run docshots -g <text>.
// Prehliadac je nainstalovany Edge; iny cez PLAYWRIGHT_CHANNEL. Kam ukladat: DOCSHOTS_OUT (predvolene repozitar docs vedla).
import { defineConfig } from "@playwright/test";
import { DOCSHOTS_SERVER } from "./server";

export default defineConfig({
  testDir: ".",
  // nie *.spec.ts ani *.e2e.ts - tie spusta bun test a e2e
  testMatch: /.*\.shots\.ts$/,
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [["list"]],
  outputDir: "../e2e/.run/docshots-results",
  globalSetup: "./demo.ts",
  use: {
    baseURL: DOCSHOTS_SERVER.url,
    channel: process.env.PLAYWRIGHT_CHANNEL ?? "msedge",
    locale: "sk-SK",
    timezoneId: "Europe/Bratislava",
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
  },
  webServer: {
    command: `node e2e/server.mjs ${DOCSHOTS_SERVER.name} ${DOCSHOTS_SERVER.port}`,
    cwd: "..",
    url: `${DOCSHOTS_SERVER.url}/api/auth/me`,
    reuseExistingServer: false,
    timeout: 60_000,
    stdout: "ignore",
    stderr: "pipe",
  },
});
