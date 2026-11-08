// Historia ustredni AK Signal na casovej osi: intervaly stavov svetla, hlasenia a okna vlakov z logu servera.
import type { AnnouncementHistoryDto, DeviceHistoryDto, LightStateDto, ServerLightEventDto } from "@/api";

/** Interval na osi [ms]. */
export interface Span {
  from: number;
  to: number;
}

/** Stavy svetla, ktore os ukazuje ako pasy. */
export type LightLane = "on" | "auto" | "manual" | "dark" | "fault" | "silent" | "poweredOff";

export const LIGHT_LANES: LightLane[] = ["on", "auto", "manual", "dark", "fault", "silent", "poweredOff"];

/** Okno vlaku z logu servera: od zapisu po planovane zhasnutie. */
export interface TrainWindow extends Span {
  train: string;
  name: string;
}

/** Stavy ustredne v poradi casu (prvy moze byt pred obdobim - stav na jeho zaciatku). */
export const statesOf = (history: DeviceHistoryDto, board: string): LightStateDto[] =>
  history.lights.filter((s) => s.boardId === board).sort((a, b) => Date.parse(a.at) - Date.parse(b.at));

/**
 * Intervaly v obdobi [from, to), kedy plati stav `lane`. Kazdy stav plati od svojho casu po dalsi; posledny po `until`
 * (teraz pri obdobi, ktore este nekoncilo).
 */
export function spans(states: LightStateDto[], lane: LightLane, from: number, to: number, until: number = to): Span[] {
  const result: Span[] = [];
  const end = Math.min(to, until);
  states.forEach((state, i) => {
    if (!state[lane]) return;
    const start = Math.max(from, Date.parse(state.at));
    const stop = Math.min(end, i + 1 < states.length ? Date.parse(states[i + 1].at) : end);
    if (stop <= start) return;
    const last = result.at(-1);
    // nasledujuci stav s tym istym priznakom predlzi interval
    if (last && last.to === start) last.to = stop;
    else result.push({ from: start, to: stop });
  });
  return result;
}

/** Hlasenia ustredne orezane na obdobie. */
export function announcementSpans(history: DeviceHistoryDto, board: string, from: number, to: number): (Span & { item: AnnouncementHistoryDto })[] {
  return history.announcements
    .filter((a) => a.boardId === board)
    .map((item) => ({
      item,
      from: Math.max(from, Date.parse(item.startedAt)),
      to: Math.min(to, Math.max(Date.parse(item.endedAt), Date.parse(item.startedAt) + 1)),
    }))
    .filter((s) => s.to > s.from);
}

/** Okna vlakov zo zapisov "zmena pre vlak" logu servera: vlak potrebuje svetlo do planovaneho zhasnutia. */
export function trainWindows(events: ServerLightEventDto[], board: string, from: number, to: number): TrainWindow[] {
  const windows: TrainWindow[] = [];
  for (const e of events) {
    if (e.boardId !== board || e.event !== "train" || !e.trainNeedsLight || !e.turnOff) continue;
    const start = Math.max(from, Date.parse(e.at));
    const stop = Math.min(to, Date.parse(e.turnOff));
    if (stop <= start) continue;
    // ten isty vlak pre viac svetiel ustredne - jedno okno
    if (windows.some((w) => w.train === e.train && w.from === start && w.to === stop)) continue;
    windows.push({ from: start, to: stop, train: e.train, name: e.name });
  }
  return windows;
}

/** Cele hodiny v obdobi na popis osi (najviac `max` - pri dlhom obdobi kazda n-ta hodina). */
export function hourTicks(from: number, to: number, max = 12): number[] {
  const hour = 3_600_000;
  const step = Math.max(1, Math.ceil((to - from) / hour / max)) * hour;
  const first = new Date(from);
  first.setMinutes(0, 0, 0);
  const ticks: number[] = [];
  for (let t = first.getTime() < from ? first.getTime() + hour : first.getTime(); t <= to; t += hour) {
    if ((t - first.getTime()) % step === 0) ticks.push(t);
  }
  return ticks;
}
