// Tema rozhrania: svetla, tmava alebo podla systemu - trieda .dark na <html> (shadcn), volba ostava v prehliadaci.
import { useSyncExternalStore } from "react";

export type Theme = "light" | "dark" | "auto";

export const THEMES: Theme[] = ["light", "dark", "auto"];

const KEY = "theme";
const media = matchMedia("(prefers-color-scheme: dark)");
const listeners = new Set<() => void>();

function load(): Theme {
  try {
    const saved = localStorage.getItem(KEY);
    if (saved === "light" || saved === "dark" || saved === "auto") return saved;
  } catch {
    // ulozisko nedostupne
  }
  return "auto";
}

let theme = load();

function apply() {
  const dark = theme === "dark" || (theme === "auto" && media.matches);
  document.documentElement.classList.toggle("dark", dark);
  document.documentElement.style.colorScheme = dark ? "dark" : "light";
}

media.addEventListener("change", apply);
apply();

export function setTheme(value: Theme) {
  theme = value;
  try {
    localStorage.setItem(KEY, value);
  } catch {
    // ulozisko nedostupne
  }
  apply();
  listeners.forEach((l) => l());
}

export function useTheme(): Theme {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => theme,
  );
}
