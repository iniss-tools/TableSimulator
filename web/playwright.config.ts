// Testy weboveho rozhrania v prehliadaci (Playwright) proti skutocnemu serveru simulatora s cistymi datami.
// Spustenie: bun run e2e (zostavi web aj server). Prehliadac je nainstalovany Edge; iny cez PLAYWRIGHT_CHANNEL.
import { defineConfig } from "@playwright/test";
import { E2E_SERVERS } from "./e2e/servers";

export default defineConfig({
  testDir: "./e2e",
  // nie *.spec.ts - tie by spustil aj bun test
  testMatch: /.*\.e2e\.ts$/,
  // servery maju spolocny stav (linky, tabule) - testy idu po jednom
  fullyParallel: false,
  workers: 1,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: [["list"], ["html", { open: "never", outputFolder: "e2e/.run/report" }]],
  outputDir: "e2e/.run/results",
  use: {
    baseURL: E2E_SERVERS.open.url,
    channel: process.env.PLAYWRIGHT_CHANNEL ?? "msedge",
    locale: "sk-SK",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  webServer: Object.values(E2E_SERVERS).map((server) => ({
    command: `node e2e/server.mjs ${server.name} ${server.port}`,
    url: `${server.url}/api/auth/me`,
    reuseExistingServer: false,
    timeout: 60_000,
    stdout: "ignore",
    stderr: "pipe",
  })),
});
