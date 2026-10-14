// Opakovane volanie nastroja aplikacie MCP Apps: server pri volani caka na zmenu (dlhe cakanie), takze vysledok
// prichadza az so zmenou; po chybe kratka pauza a dalsi pokus.
import type { McpAppBridge } from "./bridge";

/** Pauza po chybe pred dalsim pokusom [ms]. */
const RETRY_MS = 3000;

export interface Follower<T> {
  /** Argumenty dalsieho volania - podla posledneho vysledku. */
  args: () => Record<string, unknown>;
  data: (value: T) => void;
  /** Chyba volania; null = volanie opat preslo. */
  error: (message: string | null) => void;
}

/** Vola nastroj dookola, kym plati alive(). */
export async function follow<T>(bridge: McpAppBridge, tool: string, follower: Follower<T>, alive: () => boolean): Promise<void> {
  while (alive()) {
    try {
      const data = await bridge.callTool<T>(tool, follower.args());
      if (!alive()) return;
      if (data) {
        follower.data(data);
        follower.error(null);
      }
    } catch (e) {
      if (!alive()) return;
      follower.error(e instanceof Error ? e.message : String(e));
      await new Promise((resolve) => setTimeout(resolve, RETRY_MS));
    }
  }
}
