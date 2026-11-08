import { describe, expect, test } from "bun:test";
import type { DeviceHistoryDto, LightStateDto, ServerLightEventDto } from "@/api";
import { announcementSpans, hourTicks, spans, statesOf, trainWindows } from "./history";

const T0 = Date.parse("2026-11-08T10:00:00Z");
const at = (minutes: number) => new Date(T0 + minutes * 60_000).toISOString();
const ms = (minutes: number) => T0 + minutes * 60_000;

const state = (minutes: number, patch: Partial<LightStateDto> = {}): LightStateDto => ({
  boardId: "6-3",
  at: at(minutes),
  auto: false,
  manual: false,
  dark: false,
  on: false,
  fault: false,
  silent: false,
  poweredOff: false,
  ...patch,
});

const event = (minutes: number, patch: Partial<ServerLightEventDto>): ServerLightEventDto => ({
  at: at(minutes),
  station: "Lipovec",
  name: "Nástupište 1",
  event: "train",
  boardId: "6-3",
  manual: false,
  mode: "AUTO",
  trainNeedsLight: true,
  state: [],
  turnOn: null,
  turnOff: null,
  train: "6412",
  ...patch,
});

describe("historia ustredni", () => {
  test("pasy stavu: stav pred obdobim, spojenie rovnakych a koniec teraz", () => {
    const states = [state(-30, { dark: true }), state(10, { dark: true, auto: true, on: true }), state(20, { dark: true }), state(40)];
    expect(spans(states, "dark", ms(0), ms(60))).toEqual([{ from: ms(0), to: ms(40) }]);
    expect(spans(states, "on", ms(0), ms(60))).toEqual([{ from: ms(10), to: ms(20) }]);
    // posledny stav plati len po teraz
    expect(spans([state(50, { auto: true })], "auto", ms(0), ms(120), ms(70))).toEqual([{ from: ms(50), to: ms(70) }]);
  });

  test("stavy, hlasenia a okna vlakov len danej ustredne", () => {
    const history: DeviceHistoryDto = {
      from: at(0),
      to: at(60),
      lights: [state(5), { ...state(1), boardId: "7-0" }, state(-5)],
      announcements: [
        { boardId: "6-3", channel: 0, sounds: [], startedAt: at(-2), endedAt: at(3), result: "completed", value: 1, title: null, queue: null, circuit: null },
        {
          boardId: "7-0",
          channel: 1,
          sounds: ["CZ/a"],
          startedAt: at(4),
          endedAt: at(5),
          result: "cancelled",
          value: null,
          title: null,
          queue: null,
          circuit: null,
        },
      ],
      server: [],
    };
    expect(statesOf(history, "6-3").map((s) => s.at)).toEqual([at(-5), at(5)]);
    expect(announcementSpans(history, "6-3", ms(0), ms(60)).map((s) => [s.from, s.to])).toEqual([[ms(0), ms(3)]]);

    const windows = trainWindows(
      [
        event(10, { turnOff: at(18) }),
        event(10, { turnOff: at(18), name: "Nástupište 2" }),
        event(12, { trainNeedsLight: false, turnOn: at(30) }),
        event(14, { boardId: "7-0", turnOff: at(20) }),
      ],
      "6-3",
      ms(0),
      ms(60),
    );
    expect(windows).toEqual([{ from: ms(10), to: ms(18), train: "6412", name: "Nástupište 1" }]);
  });

  test("hodiny na osi", () => {
    const start = new Date(2026, 10, 8, 10, 20).getTime();
    expect(hourTicks(start, start + 2 * 3_600_000).map((t) => new Date(t).getHours())).toEqual([11, 12]);
    expect(hourTicks(start, start + 24 * 3_600_000).length).toBeLessThanOrEqual(12);
  });
});
