// Ustredna AK Signal: odvodene udaje pre kartu a stranku (rezim ako ho vidi server, hrajuce hlasenia, priebeh).
import type { AkSignalDto, AkSignalProfileDto, AkSignalSettingsRequest, AnnouncementDto, BoardDto } from "@/api";

/** Rezim svetla tak, ako ho server posiela klientovi: rucne ma prednost pred automatikou. */
export type LightMode = "manual" | "auto" | "off";

export function lightMode(state: AkSignalDto): LightMode {
  if (state.manual) return "manual";
  return state.autoMode ? "auto" : "off";
}

/** Ustredna s protokolom AKSIGNALUH (kanaly 1 a 2, zvuky hra sama). */
export const isUh = (board: Pick<BoardDto, "manufacturer">) => board.manufacturer.toUpperCase() === "AKSIGNALUH";

/** Predvolene nastavenie ustredne - zodpoveda AkSignalProfile.Default na serveri. */
export const DEFAULT_AKSIGNAL: AkSignalProfileDto = {
  lights: [],
  audio: [],
  sensor: { mode: "sun", offsetMinutes: 0 },
  conditions: { lightsFault: false, highPriority: false, outputPowerError: false, ampSupplyMissing: false, ampCommError: false, bankNotSynced: false },
  soundSeconds: 1.5,
  syncSeconds: 2,
  silenceSeconds: 10,
};

/** Nastavenie ustredne z profilu tabule (tabula zo samoucenia ho nema). */
export const akSignalProfile = (board: Pick<BoardDto, "profile">): AkSignalProfileDto => board.profile.akSignal ?? DEFAULT_AKSIGNAL;

/** Ziadost o zmenu snimaca alebo stavov ustredne; casovanie ostava. */
export function settingsRequest(profile: AkSignalProfileDto, patch: Partial<Pick<AkSignalProfileDto, "sensor" | "conditions">>): AkSignalSettingsRequest {
  return {
    sensor: patch.sensor ?? profile.sensor,
    conditions: patch.conditions ?? profile.conditions,
    soundSeconds: null,
    syncSeconds: null,
    silenceSeconds: null,
  };
}

/** Hlasenia, ktore prave hraju (pri AKSIGNAL zapnuty rozhlas). */
export const playing = (state: AkSignalDto) => state.announcements.filter((a) => a.result === "playing");

/** Hlasenie, ktore hra na kanali; null = kanal je volny. */
export const playingOn = (state: AkSignalDto, channel: number) => playing(state).find((a) => a.channel === channel) ?? null;

/** Poslednych `count` hlaseni od najnovsieho (ustredna aj rozhlas vzdialeneho INISSu). */
export const recentAnnouncements = (state: { announcements: AnnouncementDto[] }, count: number) => state.announcements.slice(-count).reverse();

/** Priebeh hlasenia 0-1 podla planovanej dlzky; null = bez dlzky (AKSIGNAL - kym ho server nevypne). */
export function progress(announcement: AnnouncementDto, now: number): number | null {
  if (!announcement.durationMs) return null;
  const end = announcement.endedAt ? Date.parse(announcement.endedAt) : now;
  return Math.min(1, Math.max(0, (end - Date.parse(announcement.startedAt)) / announcement.durationMs));
}

/** Ako dlho hlasenie hralo alebo hra [ms]. */
export const elapsed = (announcement: AnnouncementDto, now: number) =>
  Math.max(0, (announcement.endedAt ? Date.parse(announcement.endedAt) : now) - Date.parse(announcement.startedAt));
