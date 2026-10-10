// Monitor hlaseni: vyber podla vlaku a textu ako v archive (bez ohladu na velkost pismen a diakritiku). Nove hlasenia
// pridava do zoznamu api/live.ts.
import type { ReportArchiveEntryDto } from "@/api";

/** Kolko zaznamov monitor nacita na zaciatku; zivo pribudaju dalsie (najviac 1000, starsie su v archive hlaseni). */
export const MONITOR_LIMIT = 500;

/** Text bez diakritiky a malymi pismenami. */
const plain = (value: string) =>
  value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase();

/** Zaznam vyhovuje cislu vlaku (cast cisla) a hladanemu textu v ktoromkolvek udaji. */
export function matchesReport(entry: ReportArchiveEntryDto, train: string, text: string): boolean {
  const number = train.trim();
  if (number && !(entry.train?.number ?? "").toLowerCase().includes(number.toLowerCase())) return false;
  const needle = plain(text.trim());
  if (!needle) return true;
  const values = [
    entry.kind,
    entry.name,
    entry.station,
    entry.text,
    entry.result,
    entry.arrivalTrack,
    entry.departureTrack,
    entry.train?.type ?? "",
    entry.train?.number ?? "",
    entry.train?.name ?? "",
    ...entry.details.map((d) => d.value),
  ];
  return values.some((v) => v && plain(v).includes(needle));
}
