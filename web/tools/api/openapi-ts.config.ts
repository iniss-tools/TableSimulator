// Klient API generovany z dokumentu OpenAPI servera (web/openapi.json - snimka, ktoru stazi test OpenApiTests).
// Generator potrebuje JS API TypeScriptu 5.5-6, preto ma vlastny balik; web sam pouziva TypeScript 7.
// Vo web/: "bun run api" prepise src/api/generated - rucne sa tam nic nemeni.
import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig({
  input: "../../openapi.json",
  output: "../../src/api/generated",
  plugins: [
    { name: "@hey-api/client-fetch", runtimeConfigPath: "../../src/api/client-config" },
    "@hey-api/typescript",
    "@hey-api/sdk",
    { name: "@tanstack/react-query", queryOptions: true, queryKeys: true, mutationOptions: true },
  ],
});
