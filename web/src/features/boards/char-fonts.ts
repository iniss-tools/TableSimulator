// Cisla pisma znakovej tabule LCD1 a ERP: ktore tabula dostala a ktore maju priradene pismo.
import type { BoardDto, CharFontDto } from "@/api";

/** Najvacsie cislo pisma bez inverzie (0x40) a blikania (0x80). */
export const MAX_FONT_NUMBER = 63;

/** Vyrobcovia, ktorym INISS posiela cislo pisma pri kazdom znaku. */
const FONT_MANUFACTURERS = ["LCD1", "ERP"];

/** Tabula dostava cislo pisma pri kazdom znaku. */
export function hasCharFonts(manufacturer: string): boolean {
  return FONT_MANUFACTURERS.includes(manufacturer.toUpperCase());
}

/** Cisla pisma s priradenym pismom vzostupne. */
export function charFontNumbers(fonts: Record<string, CharFontDto> | null | undefined): number[] {
  return Object.keys(fonts ?? {})
    .map((k) => Number.parseInt(k, 10))
    .filter((n) => Number.isInteger(n) && n >= 0 && n <= MAX_FONT_NUMBER)
    .sort((a, b) => a - b);
}

/** Cisla pisma (bez inverzie a blikania), ktore tabula od INISSu dostala - zo znakov na tabuli. */
export function usedCharFonts(board: BoardDto | undefined): number[] {
  const used = new Set<number>();
  for (const row of board?.display.charRows ?? [])
    for (const layer of row.layers) layer.fonts.forEach((font, i) => layer.codes[i] >= 0 && used.add(font & MAX_FONT_NUMBER));
  return [...used].sort((a, b) => a - b);
}
