// Zvuk hlaseni v prehliadaci: predvolene stlmeny, zapina ho obsluha tlacidlom v zahlavi; plati pre tento prehliadac.
import { useSyncExternalStore } from "react";
import { stopAllAnnouncements, unlockAudio } from "./sound-player";

const KEY = "ts.sound";
const listeners = new Set<() => void>();

function stored(): boolean {
  try {
    return localStorage.getItem(KEY) === "1";
  } catch {
    return false;
  }
}

let enabled = stored();

export function useSoundEnabled(): boolean {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => enabled,
  );
}

/** Zapne alebo stlmi zvuk - volat z kliknutia (prehliadac pusti zvuk len po akcii obsluhy). */
export function setSoundEnabled(on: boolean) {
  if (on) unlockAudio();
  else stopAllAnnouncements();
  enabled = on;
  try {
    if (on) localStorage.setItem(KEY, "1");
    else localStorage.removeItem(KEY);
  } catch {
    // bez ulozenia plati len do obnovenia stranky
  }
  listeners.forEach((listener) => listener());
}
