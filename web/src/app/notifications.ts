// Systemove oznamenia (aj ked okno simulatora nie je vidiet) o vypadkoch liniek. Zapina ich obsluha v ponuke
// pouzivatela, plati pre tento prehliadac; prehliadac ich dovoli len cez HTTPS alebo na tomto pocitaci.
import { useSyncExternalStore } from "react";

const KEY = "ts.notifications";
const ICON = "/icon-192.png";
const listeners = new Set<() => void>();

/** Prehliadac oznamenia vie a stranka ich smie ukazat. */
export const notificationsSupported = () => "Notification" in window && isSecureContext;

function stored(): boolean {
  try {
    return localStorage.getItem(KEY) === "1";
  } catch {
    return false;
  }
}

/** Oznamenia su zapnute a prehliadac ich povolil. */
function enabled(): boolean {
  return notificationsSupported() && Notification.permission === "granted" && stored();
}

export function useNotifications(): boolean {
  return useSyncExternalStore((listener) => {
    listeners.add(listener);
    return () => listeners.delete(listener);
  }, enabled);
}

/** Zapne (prehliadac sa najprv opyta na povolenie) alebo vypne oznamenia; vrati, ci sa zmena podarila. */
export async function setNotifications(on: boolean): Promise<boolean> {
  if (on && Notification.permission !== "granted" && (await Notification.requestPermission()) !== "granted") return false;
  try {
    if (on) localStorage.setItem(KEY, "1");
    else localStorage.removeItem(KEY);
  } catch {
    return false;
  }
  listeners.forEach((listener) => listener());
  return true;
}

/** Oznamenie, ked obsluha okno simulatora prave nevidi; rovnaky tag nahradi predchadzajuce. Klik prepne na stranku hash. */
export function notify(title: string, body: string, tag: string, hash: string) {
  if (!enabled() || document.visibilityState === "visible") return;
  try {
    const notification = new Notification(title, { body, tag, icon: ICON });
    notification.onclick = () => {
      window.focus();
      location.hash = hash;
      notification.close();
    };
  } catch {
    // Android ukazuje oznamenia len cez service worker - klik spracuje sw.js a posle spravu "open"
    void navigator.serviceWorker?.ready.then((registration) => registration.showNotification(title, { body, tag, icon: ICON, data: { hash } }));
  }
}

navigator.serviceWorker?.addEventListener("message", (event: MessageEvent<{ type?: string; hash?: string } | null>) => {
  if (event.data?.type === "open" && event.data.hash) location.hash = event.data.hash;
});
