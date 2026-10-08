// Datumy a casy v jazyku obsluhy (miestny cas prehliadaca).
import { locale } from "@/i18n";

/** Datum a cas (kratky datum, cas so sekundami); prazdne pre null. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "";
  return new Date(value).toLocaleString(locale(), { dateStyle: "short", timeStyle: "medium" });
}

/** Datum a cas zvlast (napr. pod sebou v uzkom stlpci); prazdne pre null. */
export function formatDateTimeParts(value: string | null | undefined): [date: string, time: string] {
  if (!value) return ["", ""];
  const date = new Date(value);
  return [date.toLocaleDateString(locale(), { dateStyle: "short" }), date.toLocaleTimeString(locale(), { timeStyle: "medium" })];
}

const AGO_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["second", 60],
  ["minute", 60],
  ["hour", 24],
  ["day", Infinity],
];

/** Ako davno ("pred 5 s", "pred 3 min") vzhladom na cas now (ms). */
export function formatAgo(value: string, now: number): string {
  let amount = Math.max(0, Math.round((now - Date.parse(value)) / 1000));
  const format = new Intl.RelativeTimeFormat(locale(), { numeric: "auto", style: "short" });
  for (const [unit, size] of AGO_UNITS) {
    if (amount < size) return format.format(-amount, unit);
    amount = Math.floor(amount / size);
  }
  return "";
}
