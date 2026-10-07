// Vychodzia konfiguracia generovaneho klienta (runtimeConfigPath v tools/api/openapi-ts.config.ts):
// adresy relativne k stranke a chyba odpovede vzdy ako vynimka (interceptory pridava client.ts).
import type { CreateClientConfig } from "./generated/client.gen";

export const createClientConfig: CreateClientConfig = (config) => ({ ...config, baseUrl: "", throwOnError: true });
