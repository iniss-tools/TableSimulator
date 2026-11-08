// Historia ustredni AK Signal na casovej osi: pasy stavov svetla, hlasenia a okna vlakov z logu servera INISS2;
// rozhlas vzdialeneho INISSu len hlasenia.
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAkSignalHistoryOptions, type BoardDto, type DeviceHistoryDto } from "@/api";
import { OptionSelect } from "@/components/common/option-select";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { locale, t } from "@/i18n";
import { announcementSpans, hourTicks, LIGHT_LANES, spans, statesOf, trainWindows, type LightLane, type Span } from "./history";

type Range = "15m" | "2h" | "day";

/** Dlzka priebeznych obdobi [ms]. */
const RECENT: Record<Exclude<Range, "day">, number> = { "15m": 15 * 60_000, "2h": 2 * 3_600_000 };

const LANE_CLASS: Record<LightLane, string> = {
  on: "fill-amber-400",
  auto: "fill-orange-500",
  manual: "fill-yellow-600",
  dark: "fill-blue-500",
  fault: "fill-red-500",
  silent: "fill-red-300",
  poweredOff: "fill-zinc-500",
};

const RESULT_CLASS: Record<string, string> = {
  completed: "fill-green-500",
  stopped: "fill-green-300",
  cancelled: "fill-red-500",
  playing: "fill-green-500",
};

const clock = (ms: number) => new Date(ms).toLocaleTimeString(locale(), { hour: "2-digit", minute: "2-digit" });

/** Dnesny datum v tvare pre pole datumu (miestny cas). */
function today(now: number): string {
  const d = new Date(now);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Jeden riadok osi: pasy v percentach sirky. */
function Lane({ label, items, from, to }: { label: string; items: (Span & { className: string; title: string })[]; from: number; to: number }) {
  const width = to - from;
  return (
    <>
      <span className="truncate text-xs text-muted-foreground">{label}</span>
      <svg className="h-3.5 w-full rounded-sm bg-muted" viewBox="0 0 1000 10" preserveAspectRatio="none" role="img" aria-label={label}>
        {items.map((item, i) => (
          <rect
            key={i}
            x={((item.from - from) / width) * 1000}
            y={0}
            width={Math.max(1, ((item.to - item.from) / width) * 1000)}
            height={10}
            className={item.className}
          >
            <title>{item.title}</title>
          </rect>
        ))}
      </svg>
    </>
  );
}

function Device({ board, history, from, to, until }: { board: BoardDto; history: DeviceHistoryDto; from: number; to: number; until: number }) {
  const remote = board.display.family === "remoteaudio";
  const states = statesOf(history, board.id);
  const lanes = remote ? [] : LIGHT_LANES.filter((lane) => lane !== "poweredOff" || states.some((s) => s.poweredOff));
  const announcements = announcementSpans(history, board.id, from, to);
  const trains = trainWindows(history.server, board.id, from, to);
  const span = (s: Span) => `${clock(s.from)}–${clock(s.to)}`;
  return (
    <section className="flex flex-col gap-1">
      <h3 className="text-sm font-medium">{board.name || t(remote ? "remoteaudio.unnamed" : "aksignal.unnamed", { address: board.address })}</h3>
      <div className="grid grid-cols-[7rem_minmax(0,1fr)] items-center gap-x-3 gap-y-1">
        {lanes.map((lane) => (
          <Lane
            key={lane}
            label={t(`aksignal.lane.${lane}`)}
            from={from}
            to={to}
            items={spans(states, lane, from, to, until).map((s) => ({ ...s, className: LANE_CLASS[lane], title: `${t(`aksignal.lane.${lane}`)} ${span(s)}` }))}
          />
        ))}
        <Lane
          label={t("aksignal.lane.audio")}
          from={from}
          to={to}
          items={announcements.map((a) => ({
            ...a,
            className: RESULT_CLASS[a.item.result] ?? "fill-green-500",
            title: `${span(a)} · ${a.item.title ?? (a.item.sounds.length > 0 ? a.item.sounds.join(" · ") : t("aksignal.audio.value", { value: a.item.value ?? "–" }))} · ${t(remote && a.item.result === "cancelled" ? "remoteaudio.result.break" : `aksignal.result.${a.item.result}`)}`,
          }))}
        />
        {trains.length > 0 && (
          <Lane
            label={t("aksignal.lane.trains")}
            from={from}
            to={to}
            items={trains.map((w) => ({
              ...w,
              className: "fill-sky-400",
              title: `${t("aksignal.train", { train: w.train || "?" })} · ${w.name} · ${span(w)}`,
            }))}
          />
        )}
      </div>
    </section>
  );
}

/** Historia ustredni za posledne 2 hodiny (priebezne) alebo za zvoleny den. */
export function HistoryTimeline({ boards }: { boards: BoardDto[] }) {
  const [range, setRange] = useState<Range>("2h");
  // kratke obdobie sa obnovuje castejsie
  const refresh = range === "15m" ? 10_000 : 30_000;
  const now = useNow(refresh);
  const [day, setDay] = useState(() => today(Date.now()));
  const dayStart = new Date(`${day}T00:00:00`).getTime();
  const from = range === "day" ? dayStart : now - RECENT[range];
  const to = range === "day" ? dayStart + 24 * 3_600_000 : now;
  const query = useQuery({
    ...getAkSignalHistoryOptions({ query: { from: new Date(from).toISOString(), to: new Date(to).toISOString() } }),
    refetchInterval: range === "day" ? false : refresh,
    enabled: Number.isFinite(from),
  });
  const ticks = hourTicks(from, to);
  const width = to - from;

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("aksignal.history")}</CardTitle>
        <CardDescription>{t("aksignal.history.hint")}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex flex-wrap items-center gap-2">
          <OptionSelect<Range>
            value={range}
            onValueChange={setRange}
            options={[
              { value: "15m", label: t("aksignal.history.15m") },
              { value: "2h", label: t("aksignal.history.2h") },
              { value: "day", label: t("aksignal.history.day") },
            ]}
            ariaLabel={t("aksignal.history")}
          />
          {range === "day" && <Input type="date" className="w-44" value={day} max={today(now)} onChange={(e) => e.target.value && setDay(e.target.value)} />}
        </div>
        {query.isPending ? (
          <Skeleton className="h-32 w-full" />
        ) : query.data ? (
          <div className="flex flex-col gap-5">
            <div className="grid grid-cols-[7rem_minmax(0,1fr)] gap-x-3">
              <span />
              <div className="relative h-4 text-[10px] text-muted-foreground">
                {ticks.map((tick) => (
                  <span key={tick} className={cn("absolute -translate-x-1/2 tabular-nums")} style={{ left: `${((tick - from) / width) * 100}%` }}>
                    {clock(tick)}
                  </span>
                ))}
              </div>
            </div>
            {boards.map((board) => (
              <Device key={board.id} board={board} history={query.data} from={from} to={to} until={Math.min(to, now)} />
            ))}
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
