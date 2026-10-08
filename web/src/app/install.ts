// Ponuka instalacie aplikacie (Chrome, Edge, Android): prehliadac ju posle raz po nacitani - odlozi sa pre tlacidlo
// kiosku (namiesto listy prehliadaca).
import { useSyncExternalStore } from "react";

interface InstallPromptEvent extends Event {
  prompt(): Promise<void>;
}

let deferred: InstallPromptEvent | null = null;
const listeners = new Set<() => void>();

function notify() {
  listeners.forEach((l) => l());
}

addEventListener("beforeinstallprompt", (event) => {
  event.preventDefault();
  deferred = event as InstallPromptEvent;
  notify();
});

addEventListener("appinstalled", () => {
  deferred = null;
  notify();
});

/** Nainstalovanie aplikacie, ak ho prehliadac ponuka; null = nie (uz je nainstalovana, nepodporuje, chyba HTTPS). */
export function useInstall(): (() => void) | null {
  const event = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => deferred,
  );
  if (!event) return null;
  return () => {
    void event.prompt().finally(() => {
      // ponuka sa da pouzit len raz
      deferred = null;
      notify();
    });
  };
}
