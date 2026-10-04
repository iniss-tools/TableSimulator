// Preklady rozhrania: slovencina a cestina, jazyk si pamata prehliadac.
import { useSyncExternalStore } from "react";
import { cs } from "./cs";
import { sk, type TextKey } from "./sk";

export type Language = "sk" | "cs";
export type { TextKey };

const dictionaries: Record<Language, Record<TextKey, string>> = { sk, cs };

function initialLanguage(): Language {
  try {
    const saved = localStorage.getItem("lang");
    if (saved === "sk" || saved === "cs") return saved;
  } catch {
    // ulozisko nedostupne
  }
  return navigator.language.toLowerCase().startsWith("cs") ? "cs" : "sk";
}

let language: Language = initialLanguage();
const listeners = new Set<() => void>();

export function setLanguage(value: Language) {
  language = value;
  try {
    localStorage.setItem("lang", value);
  } catch {
    // ulozisko nedostupne
  }
  document.documentElement.lang = value;
  listeners.forEach((l) => l());
}

export function getLanguage(): Language {
  return language;
}

/** Text v aktualnom jazyku; {nazov} sa nahradi hodnotou z params. */
export function t(key: TextKey, params?: Record<string, string | number>): string {
  let text: string = dictionaries[language][key] ?? key;
  if (params) for (const [k, v] of Object.entries(params)) text = text.replaceAll(`{${k}}`, String(v));
  return text;
}

/** Ci kluc existuje (texty druhov sprav sa skladaju z kluca zo servera). */
export function hasText(key: string): key is TextKey {
  return key in sk;
}

export function useLanguage(): Language {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => language,
  );
}

/** Format casu a cisel podla jazyka. */
export function locale(): string {
  return language === "cs" ? "cs-CZ" : "sk-SK";
}
