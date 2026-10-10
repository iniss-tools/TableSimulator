// Statistika komunikacie: obdobia grafov, doplnenie prazdnych intervalov, sucty liniek a tabul a useky stavu linky.
import type { BoardTrafficPointDto, LineStatus, LineStatusChangeDto, LineTrafficPointDto, TrafficStatisticsDto } from "@/api";

/** Obdobia grafov (koniec je teraz alebo posunuty o cele obdobia dozadu). */
export const RANGES = { hour: 60, day: 24 * 60, week: 7 * 24 * 60, month: 30 * 24 * 60 } as const;
export type RangeKey = keyof typeof RANGES;

const MINUTE = 60_000;

/** Koniec obdobia: koniec aktualnej minuty, posunuty o <c>offset</c> obdobi dozadu. */
export function rangeEnd(now: number, range: RangeKey, offset: number): number {
  return (Math.floor(now / MINUTE) + 1) * MINUTE - offset * RANGES[range] * MINUTE;
}

/** Sucet linky (alebo vsetkych liniek) v jednom intervale grafu. */
export interface LineBucket {
  time: number;
  ok: number;
  badChecksum: number;
  noBoard: number;
  noise: number;
  other: number;
  replies: number;
  faults: number;
  bytesIn: number;
  bytesOut: number;
  connections: number;
  disconnections: number;
}

/** Tabula v jednom intervale grafu. */
export interface BoardBucket {
  time: number;
  frames: number;
  acks: number;
  naks: number;
  unanswered: number;
  /** Najdlhsia pauza [s]; null = ziadna. */
  maxGap: number | null;
}

/** Zaciatky vsetkych intervalov obdobia (aj bez komunikacie). */
export function bucketTimes(data: Pick<TrafficStatisticsDto, "from" | "to" | "bucketMinutes">): number[] {
  const step = data.bucketMinutes * MINUTE;
  const times: number[] = [];
  for (let t = Date.parse(data.from); t < Date.parse(data.to); t += step) times.push(t);
  return times;
}

const emptyLine = (time: number): LineBucket => ({
  time,
  ok: 0,
  badChecksum: 0,
  noBoard: 0,
  noise: 0,
  other: 0,
  replies: 0,
  faults: 0,
  bytesIn: 0,
  bytesOut: 0,
  connections: 0,
  disconnections: 0,
});

/** Intervaly linky alebo suctu vsetkych liniek (<c>line</c> null); chybajuce intervaly su nuly. */
export function lineBuckets(points: LineTrafficPointDto[], times: number[], line: number | null): LineBucket[] {
  const byTime = new Map(times.map((t) => [t, emptyLine(t)]));
  for (const p of points) {
    if (line !== null && p.line !== line) continue;
    const bucket = byTime.get(Date.parse(p.time));
    if (!bucket) continue;
    bucket.ok += p.ok;
    bucket.badChecksum += p.badChecksum;
    bucket.noBoard += p.noBoard;
    bucket.noise += p.noise;
    bucket.other += p.other;
    bucket.replies += p.replies;
    bucket.faults += p.faults;
    bucket.bytesIn += p.bytesIn;
    bucket.bytesOut += p.bytesOut;
    bucket.connections += p.connections;
    bucket.disconnections += p.disconnections;
  }
  return [...byTime.values()];
}

/** Intervaly jednej tabule; chybajuce intervaly su nuly. */
export function boardBuckets(points: BoardTrafficPointDto[], times: number[], line: number, address: number): BoardBucket[] {
  const byTime = new Map<number, BoardBucket>(times.map((t) => [t, { time: t, frames: 0, acks: 0, naks: 0, unanswered: 0, maxGap: null }]));
  for (const p of points) {
    if (p.line !== line || p.address !== address) continue;
    const bucket = byTime.get(Date.parse(p.time));
    if (!bucket) continue;
    bucket.frames += p.frames;
    bucket.acks += p.acks;
    bucket.naks += p.naks;
    bucket.unanswered += p.unanswered;
    if (p.maxGapMs !== null) bucket.maxGap = Math.max(bucket.maxGap ?? 0, p.maxGapMs / 1000);
  }
  return [...byTime.values()];
}

/** Suhrn tabule za obdobie. */
export interface BoardTotal {
  line: number;
  address: number;
  frames: number;
  acks: number;
  naks: number;
  unanswered: number;
  /** Najdlhsia pauza [ms]; null = ziadna. */
  maxGapMs: number | null;
}

export function boardTotals(points: BoardTrafficPointDto[]): BoardTotal[] {
  const totals = new Map<string, BoardTotal>();
  for (const p of points) {
    const key = `${p.line}-${p.address}`;
    let total = totals.get(key);
    if (!total) totals.set(key, (total = { line: p.line, address: p.address, frames: 0, acks: 0, naks: 0, unanswered: 0, maxGapMs: null }));
    total.frames += p.frames;
    total.acks += p.acks;
    total.naks += p.naks;
    total.unanswered += p.unanswered;
    if (p.maxGapMs !== null) total.maxGapMs = Math.max(total.maxGapMs ?? 0, p.maxGapMs);
  }
  return [...totals.values()].sort((a, b) => a.line - b.line || a.address - b.address);
}

/** Usek stavu linky v obdobi; stav pred prvou znamou zmenou je neznamy (null). */
export interface StatusSegment {
  start: number;
  end: number;
  status: LineStatus | null;
  detail: string | null;
}

/** Useky stavu linky od zaciatku po koniec obdobia (zmena pred zaciatkom je stav na zaciatku). */
export function statusSegments(changes: LineStatusChangeDto[], line: number, from: number, to: number): StatusSegment[] {
  const own = changes.filter((c) => c.line === line).map((c) => ({ ...c, at: Date.parse(c.time) }));
  const segments: StatusSegment[] = [];
  let current: StatusSegment = { start: from, end: to, status: null, detail: null };
  for (const change of own) {
    const at = Math.max(from, Math.min(to, change.at));
    if (at > current.start) {
      segments.push({ ...current, end: at });
      current = { start: at, end: to, status: change.status, detail: change.detail };
    } else {
      current = { ...current, status: change.status, detail: change.detail };
    }
  }
  segments.push(current);
  return segments.filter((s) => s.end > s.start);
}

/** Linky, ktore v obdobi komunikovali alebo menili stav. */
export function activeLines(data: TrafficStatisticsDto): number[] {
  return [...new Set([...data.lines.map((l) => l.line), ...data.statusChanges.map((s) => s.line)])].sort((a, b) => a - b);
}
