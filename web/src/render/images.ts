// Obrazky cela (logo ako data URL v predlohe): nacitanie do pamati a stav. Kreslenie je synchronne - obrazok, ktory
// sa este nacitava, sa nenakresli a tabula sa prekresli, ked je nacitany.
import type { BoardFaceDto } from "../api/generated/types.gen";

const images = new Map<string, HTMLImageElement>();

function load(src: string): HTMLImageElement {
  let image = images.get(src);
  if (!image) {
    image = new Image();
    image.decoding = "async";
    image.src = src;
    images.set(src, image);
  }
  return image;
}

/** Nacitany obrazok; null = este sa nacitava, nepodaril sa alebo zdroj chyba. */
export function faceImage(src: string): HTMLImageElement | null {
  if (!src) return null;
  const image = load(src);
  return image.complete && image.naturalWidth > 0 ? image : null;
}

/** Zdroje obrazkov cela. */
function sources(face: BoardFaceDto | null): string[] {
  return (face?.elements ?? []).filter((e) => e.kind === "image" && e.image).map((e) => e.image);
}

/** Vsetky obrazky cela su nacitane (alebo ziadne nema). */
export function faceImagesReady(face: BoardFaceDto | null): boolean {
  return sources(face).every((src) => faceImage(src) !== null || load(src).complete);
}

/** Nacita obrazky cela; true = niektory sa prave nacital (treba prekreslit). */
export async function loadFaceImages(face: BoardFaceDto | null): Promise<boolean> {
  const pending = sources(face).filter((src) => !load(src).complete);
  if (pending.length === 0) return false;
  await Promise.all(
    pending.map((src) =>
      load(src)
        .decode()
        .catch(() => undefined),
    ),
  );
  return true;
}
