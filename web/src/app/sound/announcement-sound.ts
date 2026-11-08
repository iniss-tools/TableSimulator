// Hlasenia, ktore prehliadac prehra: ustredne AK Signal UH a rozhlas vzdialeneho INISSu (nahravky z banky INISSu).
// Ciste funkcie bez prehliadaca - prehravanie je v sound-player.ts.
import type { AnnouncementDto, AnnouncementResult, BoardDto } from "@/api";
import { isUh } from "@/features/aksignal/aksignal";

/** Hrajuce hlasenie so zvukmi; kluc je jedinecny na stranke (tabula a cislo hlasenia). */
export interface PlayableAnnouncement {
  key: string;
  announcement: AnnouncementDto;
}

/** Nahravka hlasenia s tichom pred a po nej [ms]. */
export interface SoundStep {
  name: string;
  beforeMs: number;
  afterMs: number;
}

/** Co sa ma zmenit oproti minulemu stavu. */
export interface SoundChanges {
  /** Kluce hrajucich hlaseni - minuly stav pre dalsie volanie. */
  playing: Set<string>;
  /** Nove hlasenia na prehratie. */
  start: PlayableAnnouncement[];
  /** Hlasenia, ktore INISS zastavil alebo zrusil (dohrate nechat dohrat). */
  stop: string[];
}

/** Zariadenie, ktore hra nahravky z banky (AKSIGNAL hra rozhlas sam, bez zoznamu zvukov). */
export const hasSound = (board: BoardDto) => (board.display.akSignal !== null && isUh(board)) || board.display.remoteAudio !== null;

/**
 * Nove a skoncene hlasenia oproti minulemu stavu. Prvy stav (previous null) nespusti nic - hlasenie, ktore uz hra, by
 * zaznelo od zaciatku mimo casu simulatora. Dohrate hlasenie sa nezastavuje, prehliadac ho mohol zacat o chvilu neskor.
 */
export function soundChanges(previous: ReadonlySet<string> | null, boards: BoardDto[]): SoundChanges {
  const playing = new Set<string>();
  const start: PlayableAnnouncement[] = [];
  const results = new Map<string, AnnouncementResult>();
  for (const board of boards) {
    const announcements = [...(board.display.akSignal?.announcements ?? []), ...(board.display.remoteAudio?.announcements ?? [])];
    for (const announcement of announcements) {
      const key = `${board.id}/${announcement.id}`;
      results.set(key, announcement.result);
      if (announcement.result !== "playing" || announcement.sounds.length === 0) continue;
      playing.add(key);
      if (previous && !previous.has(key)) start.push({ key, announcement });
    }
  }

  const stop = previous ? [...previous].filter((key) => !playing.has(key) && results.get(key) !== "completed") : [];
  return { playing, start, stop };
}

/** Nahravky hlasenia v poradi s pauzami (zaporna pauza = ziadna). */
export const soundSteps = (announcement: Pick<AnnouncementDto, "sounds" | "pauses">): SoundStep[] =>
  announcement.sounds.map((name, i) => ({
    name,
    beforeMs: Math.max(0, announcement.pauses?.[i]?.beforeMs ?? 0),
    afterMs: Math.max(0, announcement.pauses?.[i]?.afterMs ?? 0),
  }));
