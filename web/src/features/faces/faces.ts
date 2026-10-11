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
      return { ...NEW_ELEMENT, kind, anchor: "top", align: "right", vAlign: "middle", x: 4, size: 9 };
    default:
      return { ...NEW_ELEMENT, kind, anchor: "top", vAlign: "middle", x: 4, size: 6, text: t("faces.element.newText") };
  }
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
