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
