// Prekryvajuce sa playlisty skupiny (ako upozornenie INISSView 2): dva playlisty rovnakeho druhu, ktore mozu platit
// v ten isty den v tom istom case - vyhra z nich neskor zacate okno, co obsluha casto necaka. Rozne druhy sa prekryvat
// smu (jeden den ma prednost pred dnami v tyzdni a tie pred kazdym dnom). Okno cez polnoc sa berie ako cas dna.
import type { SetSignagePlaylistRequest, SignagePlaylistDto } from "../../api/generated/types.gen";

const DAY = 24 * 60;

/** Den 2026-10-08 ako poradove cislo dna (od 1. 1. 1970). */
function dayNumber(iso: string): number {
  const [y, m, d] = iso.split("-").map(Number);
  return Math.round(Date.UTC(y, m - 1, d) / 86_400_000);
}

/** Den v tyzdni 1 (pondelok) az 7 (nedela) poradoveho dna. */
function isoWeekday(day: number): number {
  // 1. 1. 1970 bol stvrtok
  return ((((day + 3) % 7) + 7) % 7) + 1;
}

/** Casove okno ako useky dna [od, do) - okno cez polnoc su dva useky, rovnaky zaciatok a koniec cely den. */
function intervals(p: SetSignagePlaylistRequest): [number, number][] {
  const from = p.from;
  const to = p.to === 0 ? DAY : p.to;
  if (p.from === p.to || (from === 0 && to === DAY)) return [[0, DAY]];
  if (to > from) return [[from, to]];
  return [
    [from, DAY],
    [0, to],
  ];
}

function timesOverlap(a: SetSignagePlaylistRequest, b: SetSignagePlaylistRequest): boolean {
  return intervals(a).some(([s1, e1]) => intervals(b).some(([s2, e2]) => s1 < e2 && s2 < e1));
}

/** Platia oba playlisty aspon v jeden spolocny den. */
function daysOverlap(a: SetSignagePlaylistRequest, b: SetSignagePlaylistRequest): boolean {
  const range = (p: SetSignagePlaylistRequest): [number, number] => [
    dayNumber(p.start),
    p.period === "once" ? dayNumber(p.start) : p.end ? dayNumber(p.end) : Infinity,
  ];
  const [s1, e1] = range(a);
  const [s2, e2] = range(b);
  const from = Math.max(s1, s2);
  const to = Math.min(e1, e2);
  if (from > to) return false;
  if (a.period !== "weekly" && b.period !== "weekly") return true;
  const days = [a, b].filter((p) => p.period === "weekly").map((p) => new Set(p.weekdays));
  const common = [1, 2, 3, 4, 5, 6, 7].filter((d) => days.every((set) => set.has(d)));
  if (common.length === 0) return false;
  // aspon tyzden - kazdy den v tyzdni je v rozsahu
  if (to - from >= 6) return true;
  for (let day = from; day <= to; day++) if (common.includes(isoWeekday(day))) return true;
  return false;
}

/** Dvojice playlistov rovnakeho druhu, ktore mozu platit naraz. */
export function playlistOverlaps(playlists: SignagePlaylistDto[]): [SignagePlaylistDto, SignagePlaylistDto][] {
  const pairs: [SignagePlaylistDto, SignagePlaylistDto][] = [];
  for (let i = 0; i < playlists.length; i++)
    for (let j = i + 1; j < playlists.length; j++) {
      const a = playlists[i].definition;
      const b = playlists[j].definition;
      if (a.period === b.period && daysOverlap(a, b) && timesOverlap(a, b)) pairs.push([playlists[i], playlists[j]]);
    }
  return pairs;
}
