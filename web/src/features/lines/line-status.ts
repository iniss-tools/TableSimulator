// Stav linky pre obsluhu: farba, vypadok (INISS sa odpojil, linka nebezi), nazov a popis s casom.
import type { LineDto, LineStatus } from "@/api";
import type { Tone } from "@/components/common/status-badge";
import { t } from "@/i18n";
import { formatAgo } from "@/lib/format";

/** Vyber vsetkych liniek (cisla liniek su 1-100). */
export const ALL_LINES = 0;

export const STATUS_TONES: Record<LineStatus, Tone> = { connected: "ok", listening: "wait", lost: "bad", error: "bad", stopped: "off" };

/** Vypadok - INISS sa odpojil alebo linka nebezi (obsadeny port, chybajuci seriovy port). */
export const isOutage = (status: LineStatus): status is "lost" | "error" => status === "lost" || status === "error";

/** Nazov linky v prehladoch a hlaseniach: "Linka 1 – Nastupistia". */
export const lineLabel = (line: LineDto) => `${t("field.line")} ${line.number}${line.name ? ` – ${line.name}` : ""}`;

/**
 * Cas k stavu: pri pripojenom INISSe posledna sprava (INISS posiela len pri zmene, ticho nie je porucha), pri vypadku
 * ako davno nastal; inak nic.
 */
export function lineStatusTime(line: LineDto, now: number): string {
  if (line.status === "connected") return line.lastReceivedAt ? t("line.lastMessage", { ago: formatAgo(line.lastReceivedAt, now) }) : t("line.noMessage");
  return isOutage(line.status) ? formatAgo(line.statusSince, now) : "";
}
