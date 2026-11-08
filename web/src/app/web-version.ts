// Verzia rozhrania: stranka vie, ktoru zostavu nacitala (vstupny skript /assets/index-<hash>.js), a porovna ju so
// strankou, ktoru server prave posiela. Ciste funkcie bez prehliadaca - zistovanie je v use-web-version.ts.

/** Vstupny skript zostavy z HTML stranky; null = vyvojovy server Vite bez zostavy. */
export function entryScript(html: string): string | null {
  return /<script\b[^>]*\bsrc="(\/assets\/[^"]+\.js)"/.exec(html)?.[1] ?? null;
}

/**
 * Kiosk sa nacita znova len raz pre kazdu novu verziu: ak by server aj po nacitani posielal stary kod (napr. stranka
 * z vyrovnavacej pamate), nenacitaval by sa dookola.
 */
export const shouldReload = (latest: string, reloadedFor: string | null) => latest !== reloadedFor;
