// Informacne panely: pomocne funkcie pre stranky a panel - druhy sprav, casy, popisy playlistov, striedanie poloziek.
import { FileText, Globe, Image, LayoutGrid, MonitorPlay, Video, type LucideIcon } from "lucide-react";
import {
  accessKey,
  type SetSignageMessageRequest,
  type SetSignagePlaylistRequest,
  type SignageMessageDto,
  type SignageMessageKind,
  type SignageOverviewDto,
} from "@/api";
import { locale, t, type TextKey } from "@/i18n";

export const KINDS: SignageMessageKind[] = ["virtual", "text", "image", "video", "board", "web"];

export const KIND_ICONS: Record<SignageMessageKind, LucideIcon> = {
  virtual: MonitorPlay,
  text: FileText,
  image: Image,
  video: Video,
  board: LayoutGrid,
  web: Globe,
};

/** Dni v tyzdni 1 (pondelok) az 7 (nedela). */
export const WEEKDAYS = [1, 2, 3, 4, 5, 6, 7] as const;

/** Adresa obrazka alebo videa spravy - kluc zariadenia v adrese (obrazok a video sa nacitaju bez hlavicky). */
export function mediaUrl(id: string): string {
  const key = accessKey();
  return `/api/signage/media/${id}${key ? `?access_token=${encodeURIComponent(key)}` : ""}`;
}

/** Nova sprava daneho druhu. */
export function newMessage(kind: SignageMessageKind): SetSignageMessageRequest {
  return {
    name: "",
    note: "",
    kind,
    duration: 15,
    virtualBoard: null,
    board: null,
    paragraphs: kind === "text" ? [{ text: "", color: "#ffffff", size: "large", align: "center" }] : [],
    background: "#000000",
    media: null,
    url: kind === "web" ? "https://" : null,
  };
}

/** Novy playlist: kazdy den, cely den, od dnes. */
export function newPlaylist(today: string): SetSignagePlaylistRequest {
  return { name: "", period: "daily", start: today, end: null, weekdays: [], from: 0, to: 0, items: [] };
}

/** Dnesny datum (miestny) v tvare 2026-10-08. */
export function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Minuty dna ako 06:30 (1440 = 24:00). */
export function minutesText(minutes: number): string {
  return `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;
}

/** 06:30 na minuty dna; prazdne alebo zle = null. */
export function parseMinutes(text: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(text);
  if (!match) return null;
  const minutes = Number(match[1]) * 60 + Number(match[2]);
  return minutes <= 24 * 60 ? minutes : null;
}

/** Trvanie: 15 s, 2 min, 1 min 30 s. */
export function durationText(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;
  if (minutes === 0) return t("signage.seconds", { value: rest });
  return rest === 0 ? t("signage.minutes", { value: minutes }) : t("signage.minutesSeconds", { minutes, seconds: rest });
}

/** Datum 2026-10-08 v jazyku obsluhy. */
export function dateText(iso: string): string {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d).toLocaleDateString(locale(), { dateStyle: "medium" });
}

/** Kedy playlist plati: "každý deň od 1. 10. 2026 · 06:00–09:00". */
export function playlistSummary(p: SetSignagePlaylistRequest): string {
  const days =
    p.period === "once"
      ? t("signage.period.onceOn", { date: dateText(p.start) })
      : p.period === "daily"
        ? t("signage.period.dailyFrom", { date: dateText(p.start) })
        : t("signage.period.weeklyFrom", { days: p.weekdays.map((d) => t(`signage.weekday.${d}` as TextKey)).join(", "), date: dateText(p.start) });
  const until = p.period !== "once" && p.end ? ` ${t("signage.period.until", { date: dateText(p.end) })}` : "";
  const window = p.from === p.to ? t("signage.window.allDay") : `${minutesText(p.from)}–${minutesText(p.to === 0 ? 24 * 60 : p.to)}`;
  return `${days}${until} · ${window}`;
}

/** Trvanie polozky playlistu (vlastne alebo spravy). */
export function itemDuration(item: { message: number; duration: number | null }, data: SignageOverviewDto): number {
  return item.duration ?? data.messages.find((m) => m.id === item.message)?.definition.duration ?? 0;
}

/** Nazov spravy (alebo cislo, ak uz neexistuje). */
export function messageName(id: number | null, data: SignageOverviewDto | undefined): string {
  if (id === null) return "";
  return data?.messages.find((m) => m.id === id)?.definition.name ?? `#${id}`;
}

/** Poradie polozky, ktora hra po elapsed sekundach od kotvy (rovnako ako server). */
export function slotAt(durations: number[], elapsed: number): { index: number; remaining: number } {
  const total = durations.reduce((sum, d) => sum + Math.max(1, d), 0);
  if (durations.length === 0 || total === 0) return { index: 0, remaining: 0 };
  let rest = ((Math.floor(elapsed) % total) + total) % total;
  for (let i = 0; i < durations.length; i++) {
    const d = Math.max(1, durations[i]);
    if (rest < d) return { index: i, remaining: d - rest };
    rest -= d;
  }
  return { index: durations.length - 1, remaining: 0 };
}

/** Sprava je pouzita v playlistoch alebo ako predvolena v skupinach - nazvy. */
export function messageUsers(message: SignageMessageDto, data: SignageOverviewDto): string[] {
  return [
    ...data.playlists.filter((p) => p.definition.items.some((i) => i.message === message.id)).map((p) => p.definition.name),
    ...data.groups.filter((g) => g.definition.defaultMessage === message.id).map((g) => g.definition.name),
  ];
}
