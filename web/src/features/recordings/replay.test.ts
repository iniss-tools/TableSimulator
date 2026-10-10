import { expect, test } from "bun:test";
import type { ReplayStatusDto } from "@/api";
import { duration, isOlderReplay, livePosition } from "./replay";

const status = (state: ReplayStatusDto["state"], speed: number): ReplayStatusDto => ({
  name: "z",
  state,
  position: 10,
  duration: 30,
  speed,
  loop: false,
  pass: 1,
  at: new Date(100_000).toISOString(),
});

test("poloha prehravania medzi spravami servera", () => {
  expect(livePosition(status("running", 2), 103_000)).toBe(16);
  expect(livePosition(status("running", 10), 200_000)).toBe(30);
  expect(livePosition(status("paused", 2), 103_000)).toBe(10);
  expect(livePosition(status("running", 0), 103_000)).toBe(10);
});

test("dlzka", () => {
  expect(duration(65.4)).toBe("1:05");
  expect(duration(3725)).toBe("1:02:05");
});

test("starsi stav prehravania neprepise novsi", () => {
  const newer = { ...status("paused", 1), at: new Date(200_000).toISOString() };
  const older = status("running", 1);
  expect(isOlderReplay(older, newer)).toBe(true);
  expect(isOlderReplay(newer, older)).toBe(false);
  expect(isOlderReplay({ ...older, name: "iny" }, newer)).toBe(false);
  expect(isOlderReplay(older, null)).toBe(false);
});
