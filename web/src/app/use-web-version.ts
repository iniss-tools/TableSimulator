// Nove rozhranie na serveri (novsia zostava, aktualizacia): otvorena stranka bezi so starym kodom, kym sa nenacita
// znova. Po kazdom pripojeni k serveru (aj po jeho restarte) porovna svoj vstupny skript so strankou servera.
import { useEffect, useSyncExternalStore } from "react";
import { useConnection } from "@/api";
import { entryScript, shouldReload } from "./web-version";

const RELOADED_KEY = "ts.reloadedFor";
const listeners = new Set<() => void>();

/** Vstupny skript novsej zostavy na serveri; null = stranka je aktualna. */
let latest: string | null = null;
let checking = false;

const loaded = () => document.querySelector<HTMLScriptElement>('script[type="module"][src^="/assets/"]')?.getAttribute("src") ?? null;

async function check() {
  const mine = loaded();
  if (!mine || checking) return;
  checking = true;
  try {
    // stranka mimo service workera (ten berie z pamate len navigaciu) a bez vyrovnavacej pamate prehliadaca
    const response = await fetch("/", { cache: "no-store" });
    const found = response.ok ? entryScript(await response.text()) : null;
    if (found && found !== mine && found !== latest) {
      latest = found;
      listeners.forEach((listener) => listener());
    }
  } catch {
    // server je znova nedostupny - skusi sa pri dalsom pripojeni
  } finally {
    checking = false;
  }
}

/** Po kazdom pripojeni k serveru zisti, ci server nema novsie rozhranie; vrati true, ak ma. */
export function useWebOutdated(): boolean {
  const { connected } = useConnection();
  useEffect(() => {
    if (connected) void check();
  }, [connected]);
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => latest !== null,
  );
}

/** Kiosk bez obsluhy sa s novym rozhranim nacita znova sam - raz pre kazdu verziu. */
export function useReloadWhenOutdated() {
  const outdated = useWebOutdated();
  useEffect(() => {
    if (!outdated || !latest) return;
    let reloadedFor: string | null;
    try {
      reloadedFor = sessionStorage.getItem(RELOADED_KEY);
      sessionStorage.setItem(RELOADED_KEY, latest);
    } catch {
      // bez ulozenia by sa kiosk mohol nacitavat dookola - radsej nie
      return;
    }
    if (shouldReload(latest, reloadedFor)) location.reload();
  }, [outdated]);
}
