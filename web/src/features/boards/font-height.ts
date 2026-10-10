// Vyska zakladneho pisma suboru pisiem ELEN - podla nej sa kontroluje vyska riadku tabule. Subor moze mat pisma roznej
// vysky (napr. 10 a 20 bodov), riadok sa riadi prvym pismom, z ktoreho tabula kresli bez zvlastneho rezu.
import { useEffect, useState } from "react";
import { loadFontSet } from "@/render/fonts";

/** Vyska prveho pisma suboru; null = subor sa nenacital alebo nema pisma. */
export async function baseFontHeight(file: string): Promise<number | null> {
  const set = await loadFontSet(file);
  return set?.fonts[0]?.height || null;
}

/** Vyska zakladneho pisma suboru tabule; null kym sa nenacita alebo bez suboru. */
export function useFontHeight(file: string | null | undefined): number | null {
  const [loaded, setLoaded] = useState<{ file: string; height: number | null } | null>(null);
  useEffect(() => {
    if (!file) return;
    let alive = true;
    void baseFontHeight(file).then((height) => {
      if (alive) setLoaded({ file, height });
    });
    return () => {
      alive = false;
    };
  }, [file]);
  return file && loaded?.file === file ? loaded.height : null;
}
