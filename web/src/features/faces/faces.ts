// Predlohy ciel tabul na webe: popisy premennych, novy prvok podla druhu, nazvy farieb a tabula na nahlad.
import type { BoardDto, BoardProfileDto, FaceElementDto, FaceElementKind } from "@/api";
import { SAMPLE_BOARD } from "@/features/designs/sample-board";
import { boardKind } from "@/lib/boards";
import { NEW_ELEMENT } from "@/render/face";
import { t, type TextKey } from "@/i18n";

/** Premenne, ktore poznaju vstavane predlohy - ostatne sa ukazu ako {kluc}. */
const KNOWN_VARIABLES = ["track", "platform", "station", "direction"];

/** Popis premennej cela pre obsluhu. */
export function variableLabel(key: string): string {
  return KNOWN_VARIABLES.includes(key) ? t(`face.var.${key}` as TextKey) : `{${key}}`;
}

/** Nazvy farieb vzhladu, ktore sa daju pouzit namiesto #rrggbb. */
export const FACE_COLOR_TOKENS = ["text", "band", "panel", "plate", "accent"] as const;

/** Novy prvok cela daneho druhu - rozumne predvolene hodnoty na doladenie. */
export function newElement(kind: FaceElementKind): FaceElementDto {
  switch (kind) {
    case "rect":
      return { ...NEW_ELEMENT, kind, anchor: "top", height: -2, fill: "band" };
    case "clock":
      return { ...NEW_ELEMENT, kind, anchor: "top", align: "right", vAlign: "middle", x: 4, size: 9, render: "display" };
    case "symbol":
      return { ...NEW_ELEMENT, kind, anchor: "top", align: "right", vAlign: "middle", x: 4, size: 10, symbol: "arrowUp" };
    case "image":
      return { ...NEW_ELEMENT, kind, anchor: "top", x: 2, y: 1, width: 30, height: -1 };
    default:
      return { ...NEW_ELEMENT, kind, anchor: "top", vAlign: "middle", x: 4, size: 6, text: t("faces.element.newText") };
  }
}

/** Kratky popis prvku: prvy riadok textu, symbol, inak ukotvenie. */
export function elementSummary(element: FaceElementDto): string {
  if (element.kind === "text") return element.text.split("\n")[0];
  if (element.kind === "symbol") return t(`face.symbol.${element.symbol}`);
  return t(`face.anchor.${element.anchor}`);
}

/** Popis prvku pre obsluhu: druh a kratky popis. */
export function elementLabel(element: FaceElementDto): string {
  return `${t(`face.kind.${element.kind}`)}: ${elementSummary(element)}`;
}

/** Najvacsi subor obrazka cela - server prijme data URL najviac 400 000 znakov. */
export const MAX_IMAGE_BYTES = 300 * 1024;

/** Formaty obrazkov, ktore cela prijmu. */
export const IMAGE_TYPES = ["image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"];

/** Obrazok zo suboru ako data URL; chyba s klucom textu (velky alebo nepodporovany subor). */
export function readImage(file: File): Promise<string> {
  if (!IMAGE_TYPES.includes(file.type)) return Promise.reject(new Error("face.image.unsupported"));
  if (file.size > MAX_IMAGE_BYTES) return Promise.reject(new Error("face.image.tooLarge"));
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => (typeof reader.result === "string" ? resolve(reader.result) : reject(new Error("face.image.unsupported")));
    reader.onerror = () => reject(new Error("face.image.unsupported"));
    reader.readAsDataURL(file);
  });
}

/** Tabula na nahlad cela: ziva tabula s upravenym profilom, inak ukazkova (ELEN) alebo prazdna inej rodiny. */
export function previewBoard(profile: BoardProfileDto, manufacturer: string, name: string, live: BoardDto | undefined): BoardDto {
  if (live) return { ...live, name, profile };
  const kind = boardKind(manufacturer);
  if (kind === "elen") return { ...SAMPLE_BOARD, manufacturer, name, profile };
  const family = kind === "flaps" ? "flaps" : "chars";
  return {
    ...SAMPLE_BOARD,
    manufacturer,
    name,
    profile,
    display: { ...SAMPLE_BOARD.display, family, rows: [], charRows: [], flapRows: [] },
  };
}
