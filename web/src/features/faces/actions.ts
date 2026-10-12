// Prikazy editora ciel spolocne pre klavesy a kontextovu ponuku a schranka prvkov. Schranka plati pre vsetky predlohy,
// kym je aplikacia otvorena - prvky sa tak daju preniest aj zo vstavanej predlohy do vlastnej.
import { useSyncExternalStore } from "react";
import type { FaceElementDto } from "@/api";

export type FaceAction =
  "undo" | "redo" | "selectAll" | "copy" | "cut" | "paste" | "duplicate" | "remove" | "forward" | "backward" | "front" | "back" | "lock" | "hide";

/**
 * Prikaz pre klavesovu skratku s Ctrl (Cmd); null = ziadny. Poradie kreslenia podla polohy klaves ] a [ (vpravo od P,
 * na slovenskej klavesnici a a u), nie podla znaku - ten by bez AltGr nesiel napisat.
 */
export function shortcutAction(e: Pick<KeyboardEvent, "key" | "code" | "ctrlKey" | "metaKey" | "shiftKey" | "altKey">): FaceAction | null {
  if (!(e.ctrlKey || e.metaKey) || e.altKey) return null;
  if (e.code === "BracketRight") return e.shiftKey ? "front" : "forward";
  if (e.code === "BracketLeft") return e.shiftKey ? "back" : "backward";
  switch (e.key.toLowerCase()) {
    case "z":
      return e.shiftKey ? "redo" : "undo";
    case "y":
      return "redo";
    case "a":
      return "selectAll";
    case "c":
      return "copy";
    case "x":
      return "cut";
    case "v":
      return "paste";
    case "d":
      return "duplicate";
    default:
      return null;
  }
}

let copied: readonly FaceElementDto[] = [];
const listeners = new Set<() => void>();

/** Prvky do schranky - vlozia sa odomknute. */
export function copyElements(elements: readonly FaceElementDto[]) {
  copied = elements.map((e) => ({ ...e, locked: false }));
  for (const listener of listeners) listener();
}

/** Prvky v schranke (stranka sa prekresli po kopirovani). */
export function useClipboard(): readonly FaceElementDto[] {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => copied,
  );
}
