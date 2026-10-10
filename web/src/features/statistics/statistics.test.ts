import { expect, test } from "bun:test";
import type { BoardTrafficPointDto, LineTrafficPointDto } from "@/api";
import { boardBuckets, boardTotals, bucketTimes, lineBuckets, rangeEnd, statusSegments } from "./statistics";

const at = (minute: number) => new Date(minute * 60_000).toISOString();

const line = (n: number, minute: number, ok: number): LineTrafficPointDto => ({
  line: n,
  time: at(minute),
  ok,
  badChecksum: 1,
  noBoard: 0,
  noise: 0,
  other: 0,
  replies: ok,
  faults: 0,
  bytesIn: ok * 10,
  bytesOut: ok,
  connections: 0,
  disconnections: 0,
});

const board = (minute: number, frames: number, gap: number | null): BoardTrafficPointDto => ({
  line: 1,
  address: 5,
  time: at(minute),
  frames,
  acks: frames,
  naks: 0,
  unanswered: 0,
  maxGapMs: gap,
});

test("intervaly obdobia a koniec obdobia", () => {
  expect(bucketTimes({ from: at(100), to: at(130), bucketMinutes: 10 })).toEqual([100, 110, 120].map((m) => m * 60_000));
  expect(rangeEnd(90_500, "hour", 0)).toBe(120_000);
  expect(rangeEnd(90_500, "hour", 1)).toBe(120_000 - 3_600_000);
});

test("sucty liniek a tabul po intervaloch", () => {
  const times = bucketTimes({ from: at(0), to: at(3), bucketMinutes: 1 });
  const all = lineBuckets([line(1, 0, 2), line(2, 0, 3), line(1, 2, 1)], times, null);
  expect(all.map((b) => b.ok)).toEqual([5, 0, 1]);
  expect(all[0].badChecksum).toBe(2);
  expect(lineBuckets([line(1, 0, 2), line(2, 0, 3)], times, 2)[0].ok).toBe(3);

  const buckets = boardBuckets([board(0, 2, 30_000), board(2, 1, null)], times, 1, 5);
  expect(buckets.map((b) => b.maxGap)).toEqual([30, null, null]);
  expect(boardTotals([board(0, 2, 30_000), board(2, 1, 45_000)])).toEqual([
    { line: 1, address: 5, frames: 3, acks: 3, naks: 0, unanswered: 0, maxGapMs: 45_000 },
  ]);
});

test("useky stavu linky", () => {
  const changes = [
    { line: 1, time: at(-5), status: "connected" as const, detail: null },
    { line: 1, time: at(10), status: "lost" as const, detail: null },
    { line: 2, time: at(3), status: "listening" as const, detail: null },
  ];
  const from = 0;
  const to = 20 * 60_000;
  expect(statusSegments(changes, 1, from, to).map((s) => [s.start / 60_000, s.end / 60_000, s.status])).toEqual([
    [0, 10, "connected"],
    [10, 20, "lost"],
  ]);
  expect(statusSegments(changes, 2, from, to).map((s) => s.status)).toEqual([null, "listening"]);
});
