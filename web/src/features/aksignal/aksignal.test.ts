import { describe, expect, test } from "bun:test";
import type { AkSignalDto, AnnouncementDto } from "@/api";
import { DEFAULT_AKSIGNAL, isUh, lightMode, playingOn, progress, recentAnnouncements, settingsRequest } from "./aksignal";

const announcement = (id: number, channel: number, result: AnnouncementDto["result"], extra: Partial<AnnouncementDto> = {}): AnnouncementDto => ({
  id,
  channel,
  sounds: ["CZ/vlak/1234"],
  startedAt: "2026-11-08T10:00:00Z",
  result,
  title: null,
  durationMs: 4000,
  value: null,
  endedAt: null,
  queue: null,
  circuit: null,
  ...extra,
});

const state = (patch: Partial<AkSignalDto> = {}): AkSignalDto => ({
  autoMode: false,
  manual: false,
  dark: false,
  lightOn: false,
  lightState: "08",
  audioState: "10",
  announcements: [],
  darkSince: null,
  darkChangesAt: null,
  lightChangedAt: null,
  lightQueryAt: null,
  audioQueryAt: null,
  serverSilent: false,
  banks: {},
  syncedAt: null,
  syncUntil: null,
  conditions: DEFAULT_AKSIGNAL.conditions,
  scenario: null,
  ...patch,
});

describe("ustredna AK Signal", () => {
  test("rezim ako server: rucne ma prednost pred automatikou", () => {
    expect(lightMode(state())).toBe("off");
    expect(lightMode(state({ autoMode: true }))).toBe("auto");
    expect(lightMode(state({ autoMode: true, manual: true }))).toBe("manual");
  });

  test("hlasenie na kanali a posledne hlasenia od najnovsieho", () => {
    const s = state({ announcements: [announcement(1, 1, "completed"), announcement(2, 2, "stopped"), announcement(3, 1, "playing")] });
    expect(playingOn(s, 1)?.id).toBe(3);
    expect(playingOn(s, 2)).toBeNull();
    expect(recentAnnouncements(s, 2).map((a) => a.id)).toEqual([3, 2]);
  });

  test("priebeh podla planovanej dlzky", () => {
    const start = Date.parse("2026-11-08T10:00:00Z");
    expect(progress(announcement(1, 1, "playing"), start + 1000)).toBe(0.25);
    expect(progress(announcement(1, 1, "playing"), start + 9000)).toBe(1);
    expect(progress(announcement(1, 0, "playing", { durationMs: null }), start + 1000)).toBeNull();
    expect(progress(announcement(1, 1, "completed", { endedAt: "2026-11-08T10:00:02Z" }), start + 9000)).toBe(0.5);
  });

  test("ziadost meni len snimac alebo stavy", () => {
    const request = settingsRequest(DEFAULT_AKSIGNAL, { sensor: { mode: "dark", offsetMinutes: 0 } });
    expect(request.sensor.mode).toBe("dark");
    expect(request.conditions).toBe(DEFAULT_AKSIGNAL.conditions);
    expect(request.soundSeconds).toBeNull();
    expect(isUh({ manufacturer: "AkSignalUh" })).toBe(true);
    expect(isUh({ manufacturer: "AKSIGNAL" })).toBe(false);
  });
});
