import { describe, expect, test } from "bun:test";
import type { AnnouncementDto, BoardDto } from "@/api";
import { hasSound, soundChanges, soundSteps } from "./announcement-sound";

function announcement(id: number, result: AnnouncementDto["result"], sounds = ["CZ/vlak/1234"]): AnnouncementDto {
  return {
    id,
    channel: 1,
    sounds,
    startedAt: "2026-11-08T10:00:00Z",
    result,
    pauses: null,
    title: null,
    durationMs: 2000,
    value: null,
    endedAt: null,
    queue: null,
    circuit: null,
  };
}

function board(id: string, manufacturer: string, announcements: AnnouncementDto[], remote = false): BoardDto {
  const devices = remote
    ? { akSignal: null, remoteAudio: { queues: [], announcements, lastQueryAt: null } }
    : { akSignal: { announcements }, remoteAudio: null };
  return { id, manufacturer, display: devices } as unknown as BoardDto;
}

describe("zvuk hlaseni", () => {
  test("prvy stav nespusti hlasenie, ktore uz hra", () => {
    const boards = [board("3-0", "AKSIGNALUH", [announcement(1, "playing")])];
    const first = soundChanges(null, boards);
    expect(first.start).toEqual([]);
    expect([...first.playing]).toEqual(["3-0/1"]);
    expect(soundChanges(first.playing, boards).start).toEqual([]);
  });

  test("nove hlasenie zacne, zastavene skonci, dohrate dohra", () => {
    const before = new Set(["3-0/1", "3-0/2"]);
    const boards = [board("3-0", "AKSIGNALUH", [announcement(1, "stopped"), announcement(2, "completed"), announcement(3, "playing")])];
    const changes = soundChanges(before, boards);
    expect(changes.start.map((s) => s.key)).toEqual(["3-0/3"]);
    expect(changes.stop).toEqual(["3-0/1"]);
  });

  test("zmiznute zariadenie zastavi hlasenie, bez zvukov sa nehra", () => {
    const changes = soundChanges(new Set(["9-0/1"]), [board("4-0", "REMOTE_AUDIO", [announcement(5, "playing", [])], true)]);
    expect(changes.stop).toEqual(["9-0/1"]);
    expect(changes.start).toEqual([]);
  });

  test("pauzy podla poradia zvukov", () => {
    const steps = soundSteps({
      sounds: ["SK/R1/1", "SK/R1/2"],
      pauses: [
        { beforeMs: 0, afterMs: 300 },
        { beforeMs: -50, afterMs: 0 },
      ],
    });
    expect(steps).toEqual([
      { name: "SK/R1/1", beforeMs: 0, afterMs: 300 },
      { name: "SK/R1/2", beforeMs: 0, afterMs: 0 },
    ]);
    expect(soundSteps({ sounds: ["a"], pauses: null })).toEqual([{ name: "a", beforeMs: 0, afterMs: 0 }]);
  });

  test("tlacidlo len pri zariadeni s nahravkami", () => {
    expect(hasSound(board("3-0", "AKSIGNALUH", []))).toBe(true);
    expect(hasSound(board("3-0", "AKSIGNAL", []))).toBe(false);
    expect(hasSound(board("4-0", "REMOTE_AUDIO", [], true))).toBe(true);
    expect(hasSound({ id: "1-1", manufacturer: "ELEN", display: { akSignal: null, remoteAudio: null } } as unknown as BoardDto)).toBe(false);
  });
});
