// Datumy a casy v jazyku obsluhy (miestny cas prehliadaca).
import { locale } from "@/i18n";

/** Datum a cas (kratky datum, cas so sekundami); prazdne pre null. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "";
  return new Date(value).toLocaleString(locale(), { dateStyle: "short", timeStyle: "medium" });
}
