// Kiosk na tablete a televizore: vlastny manifest (aplikacia z plochy sa otvori na kiosku), obrazovka nezhasne,
// kurzor sa po chvili skryje, cela obrazovka.
import { useEffect, useState, useSyncExternalStore } from "react";
import { accessKey } from "@/api";

/** Ako dlho po poslednom pohybe ostanu ovladacie prvky a kurzor (ms). */
const IDLE_MS = 3000;

/**
 * Manifest kiosku: po "Pridat na plochu / Nainstalovat" sa aplikacia spusti na tejto stranke s klucom zariadenia,
 * na celu obrazovku. Pri odchode zo stranky sa vrati manifest hlavnej aplikacie.
 */
function useKioskManifest(view: string, name: string) {
  useEffect(() => {
    const link = document.querySelector<HTMLLinkElement>("link[rel=manifest]");
    if (!link) return;
    const original = link.getAttribute("href");
    const query = new URLSearchParams({ view, name });
    const key = accessKey();
    if (key) query.set("key", key);
    link.setAttribute("href", `/api/manifest?${query.toString()}`);
    return () => {
      if (original) link.setAttribute("href", original);
    };
  }, [view, name]);
}

/** Obrazovka nezhasne, kym je kiosk otvoreny (len cez HTTPS alebo na tomto pocitaci). */
function useWakeLock() {
  useEffect(() => {
    let lock: WakeLockSentinel | null = null;
    let active = true;
    const request = async () => {
      if (!("wakeLock" in navigator) || document.visibilityState !== "visible") return;
      try {
        const sentinel = await navigator.wakeLock.request("screen");
        if (active) lock = sentinel;
        else void sentinel.release();
      } catch {
        // prehliadac zamok nedal (setrenie baterie, bez HTTPS)
      }
    };
    // po prepnuti spat na kartu treba zamok ziadat znova
    const onVisibility = () => void request();
    void request();
    document.addEventListener("visibilitychange", onVisibility);
    return () => {
      active = false;
      document.removeEventListener("visibilitychange", onVisibility);
      void lock?.release();
    };
  }, []);
}

/** Pouzivatel sa nedavno pohol (mys, dotyk, klavesa) - ukazat kurzor a ovladanie. */
function useActivity(): boolean {
  const [active, setActive] = useState(false);
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined;
    const wake = () => {
      setActive(true);
      clearTimeout(timer);
      timer = setTimeout(() => setActive(false), IDLE_MS);
    };
    const events = ["pointermove", "pointerdown", "keydown"] as const;
    events.forEach((e) => addEventListener(e, wake));
    return () => {
      clearTimeout(timer);
      events.forEach((e) => removeEventListener(e, wake));
    };
  }, []);
  return active;
}

/** Stranka je na celej obrazovke (aj aplikacia spustena z plochy na celu obrazovku). */
export function useFullscreen(): boolean {
  return useSyncExternalStore(
    (listener) => {
      document.addEventListener("fullscreenchange", listener);
      return () => document.removeEventListener("fullscreenchange", listener);
    },
    () => document.fullscreenElement !== null || matchMedia("(display-mode: fullscreen)").matches,
  );
}

/** Prepne celu obrazovku (prehliadac ju dovoli len po kliknuti alebo dotyku). */
export function toggleFullscreen() {
  if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
  else document.documentElement.requestFullscreen?.().catch(() => undefined);
}

/** Spravanie kiosku; vrati, ci ukazat kurzor a ovladacie prvky. */
export function useKiosk(view: string, name: string): boolean {
  useKioskManifest(view, name);
  useWakeLock();
  return useActivity();
}
