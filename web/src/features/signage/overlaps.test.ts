import { describe, expect, test } from "bun:test";
import type { SetSignagePlaylistRequest, SignagePlaylistDto } from "../../api/generated/types.gen";
import { playlistOverlaps } from "./overlaps";

let next = 1;
const playlist = (definition: Partial<SetSignagePlaylistRequest>): SignagePlaylistDto => ({
  id: next++,
  definition: { name: "P", period: "daily", start: "2026-10-01", end: null, weekdays: [], from: 0, to: 0, items: [], ...definition },
});

const overlaps = (...playlists: SignagePlaylistDto[]) => playlistOverlaps(playlists).length;

describe("prekryvajuce sa playlisty", () => {
  test("kazdy den - casove okna", () => {
    expect(overlaps(playlist({ from: 360, to: 540 }), playlist({ from: 480, to: 600 }))).toBe(1);
    expect(overlaps(playlist({ from: 360, to: 540 }), playlist({ from: 540, to: 600 }))).toBe(0);
    // cely den sa prekryva so vsetkym
    expect(overlaps(playlist({}), playlist({ from: 600, to: 660 }))).toBe(1);
  });

  test("okno cez polnoc", () => {
    expect(overlaps(playlist({ from: 1320, to: 120 }), playlist({ from: 60, to: 300 }))).toBe(1);
    expect(overlaps(playlist({ from: 1320, to: 0 }), playlist({ from: 60, to: 300 }))).toBe(0);
  });

  test("rozne druhy sa prekryvat smu", () => {
    expect(overlaps(playlist({ period: "daily" }), playlist({ period: "once" }))).toBe(0);
    expect(overlaps(playlist({ period: "weekly", weekdays: [1] }), playlist({ period: "daily" }))).toBe(0);
  });

  test("dni platnosti", () => {
    expect(overlaps(playlist({ end: "2026-10-05" }), playlist({ start: "2026-10-06" }))).toBe(0);
    expect(overlaps(playlist({ period: "once", start: "2026-10-08" }), playlist({ period: "once", start: "2026-10-08" }))).toBe(1);
    expect(overlaps(playlist({ period: "once", start: "2026-10-08" }), playlist({ period: "once", start: "2026-10-09" }))).toBe(0);
  });

  test("dni v tyzdni", () => {
    expect(overlaps(playlist({ period: "weekly", weekdays: [1, 2] }), playlist({ period: "weekly", weekdays: [3] }))).toBe(0);
    expect(overlaps(playlist({ period: "weekly", weekdays: [1, 2] }), playlist({ period: "weekly", weekdays: [2, 5] }))).toBe(1);
    // spolocny utorok, ale spolocny rozsah je len stvrtok 8. 10. az piatok 9. 10. 2026
    expect(
      overlaps(
        playlist({ period: "weekly", weekdays: [2, 4], end: "2026-10-09" }),
        playlist({ period: "weekly", weekdays: [2], start: "2026-10-08", end: "2026-10-09" }),
      ),
    ).toBe(0);
  });
});
