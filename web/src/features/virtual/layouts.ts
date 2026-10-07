// Spolocne pre virtualne tabule: nazov rozlozenia, stav suboru XML, popis zaznamov a casy z XML INISSu.
import type { VirtualBoardDto, VirtualLayoutDto, VirtualSourceDto } from "@/api";
import type { Tone } from "@/components/common/status-badge";
import { hasText, t, type TextKey } from "@/i18n";

/** Nazov rozlozenia pre obsluhu - vstavane v jazyku obsluhy. */
export function layoutName(layout: VirtualLayoutDto): string {
  const key = `vlayout.${layout.id}`;
  return layout.builtIn && hasText(key) ? t(key) : layout.name;
}

/** Stav suboru XML tabule: text a farba stitku. */
export function sourceStatus(source: VirtualSourceDto | undefined): [TextKey, Tone] {
  if (!source) return ["virtual.status.missing", "bad"];
  if (source.error) return ["virtual.status.error", "bad"];
  if (source.stale) return ["virtual.status.stale", "wait"];
  return ["virtual.status.ok", "ok"];
}

/** Ktore zaznamy tabula ukazuje. */
export function rowsText(board: VirtualBoardDto): string {
  if (board.rowCount === null) return board.rowStart > 1 ? t("virtual.rows.allFrom", { start: board.rowStart }) : t("virtual.rows.all");
  return t("virtual.rows.from", { count: board.rowCount, start: board.rowStart });
}

/** Cas z XML INISSu (miestny, bez pasma) v tvare D. M. HH:MM:SS. */
export function xmlDateTime(value: string | null): string {
  if (!value) return "";
  const [date, time] = value.split("T");
  const [, month, day] = date.split("-");
  return `${Number(day)}. ${Number(month)}. ${time?.slice(0, 8) ?? ""}`;
}
