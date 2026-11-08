import { expect, test } from "bun:test";
import type { RemoteQueueDto } from "@/api";
import { remoteAudioHeight, REMOTE_AUDIO_MAX_ROWS } from "@/render/remoteaudio";
import { announcementText, queueState } from "./remote-audio";

const queue = (patch: Partial<RemoteQueueDto> = {}): RemoteQueueDto => ({ name: "", off: false, playing: null, lastCommandAt: null, ...patch });

test("stav fronty a text hlasenia vzdialeneho INISSu", () => {
  expect(queueState(queue())).toBe("idle");
  expect(queueState(queue({ off: true }))).toBe("off");
  const playing = {
    id: 1,
    channel: 0,
    sounds: ["SK/R1/1", "SK/R1/2"],
    startedAt: "2026-11-08T10:00:00Z",
    result: "playing" as const,
    title: null,
    durationMs: 3000,
    value: null,
    endedAt: null,
    queue: "",
    circuit: null,
  };
  expect(queueState(queue({ playing }))).toBe("playing");
  expect(announcementText(playing)).toBe("SK/R1/1 · SK/R1/2");
  expect(announcementText({ ...playing, title: "Os 3412" })).toBe("Os 3412");
});

test("vyska platna rozhlasu podla poctu front", () => {
  expect(remoteAudioHeight(0)).toBe(remoteAudioHeight(1));
  expect(remoteAudioHeight(10)).toBe(remoteAudioHeight(REMOTE_AUDIO_MAX_ROWS));
  expect(remoteAudioHeight(2)).toBeGreaterThan(remoteAudioHeight(1));
});
