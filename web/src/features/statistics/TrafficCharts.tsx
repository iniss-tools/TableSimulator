// Grafy statistiky komunikacie: spravy od INISSu podla vysledku, odpovede tabul, bajty, tabula a stav liniek v case.
import { Bar, BarChart, CartesianGrid, Line, ComposedChart, XAxis, YAxis } from "recharts";
import type { LineStatusChangeDto } from "@/api";
import { ChartContainer, ChartLegend, ChartLegendContent, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";
import { STATUS_TONES } from "@/features/lines/line-status";
import { cn } from "@/lib/utils";
import { locale, t } from "@/i18n";
import { statusSegments, type BoardBucket, type LineBucket } from "./statistics";

/** Popis casu intervalu: pri obdobi do dna len cas, inak aj datum. */
function timeLabel(time: number, withDate: boolean): string {
  return new Intl.DateTimeFormat(
    locale(),
    withDate ? { day: "numeric", month: "numeric", hour: "2-digit", minute: "2-digit" } : { hour: "2-digit", minute: "2-digit" },
  ).format(new Date(time));
}

interface ChartProps<T> {
  data: T[];
  withDate: boolean;
}

/** Spolocne osi, mriezka, popis a legenda grafu po intervaloch. */
function Axes({ withDate, order, unit }: { withDate: boolean; order: string[]; unit?: string }) {
  return (
    <>
      <CartesianGrid vertical={false} />
      <XAxis dataKey="time" tickLine={false} axisLine={false} minTickGap={32} tickFormatter={(v: number) => timeLabel(v, withDate)} />
      <YAxis tickLine={false} axisLine={false} width={48} allowDecimals={false} unit={unit} />
      <ChartTooltip
        content={<ChartTooltipContent labelFormatter={(_, payload) => timeLabel(Number((payload[0]?.payload as { time?: number } | undefined)?.time), true)} />}
      />
      {/* legenda v poradi radov grafu, nie podla abecedy */}
      <ChartLegend content={<ChartLegendContent />} itemSorter={(item) => order.indexOf(String(item.dataKey))} />
    </>
  );
}

export function MessagesChart({ data, withDate }: ChartProps<LineBucket>) {
  const config = {
    ok: { label: t("stats.ok"), color: "#16a34a" },
    badChecksum: { label: t("stats.badChecksum"), color: "#dc2626" },
    noBoard: { label: t("stats.noBoard"), color: "#f59e0b" },
    noise: { label: t("stats.noise"), color: "#71717a" },
    other: { label: t("stats.other"), color: "#8b5cf6" },
  } satisfies ChartConfig;
  return (
    <ChartContainer config={config} className="aspect-auto h-64 w-full">
      <BarChart data={data}>
        <Axes withDate={withDate} order={Object.keys(config)} />
        {Object.keys(config).map((key) => (
          <Bar key={key} dataKey={key} stackId="a" fill={`var(--color-${key})`} isAnimationActive={false} />
        ))}
      </BarChart>
    </ChartContainer>
  );
}

export function RepliesChart({ data, withDate }: ChartProps<LineBucket>) {
  const config = {
    replies: { label: t("stats.replies"), color: "#16a34a" },
    faults: { label: t("stats.faults"), color: "#dc2626" },
  } satisfies ChartConfig;
  return (
    <ChartContainer config={config} className="aspect-auto h-52 w-full">
      <BarChart data={data}>
        <Axes withDate={withDate} order={Object.keys(config)} />
        <Bar dataKey="replies" stackId="a" fill="var(--color-replies)" isAnimationActive={false} />
        <Bar dataKey="faults" stackId="a" fill="var(--color-faults)" isAnimationActive={false} />
      </BarChart>
    </ChartContainer>
  );
}

export function BytesChart({ data, withDate }: ChartProps<LineBucket>) {
  const config = {
    bytesIn: { label: t("stats.bytesIn"), color: "#2563eb" },
    bytesOut: { label: t("stats.bytesOut"), color: "#a855f7" },
  } satisfies ChartConfig;
  return (
    <ChartContainer config={config} className="aspect-auto h-52 w-full">
      <BarChart data={data}>
        <Axes withDate={withDate} order={Object.keys(config)} />
        <Bar dataKey="bytesIn" fill="var(--color-bytesIn)" isAnimationActive={false} />
        <Bar dataKey="bytesOut" fill="var(--color-bytesOut)" isAnimationActive={false} />
      </BarChart>
    </ChartContainer>
  );
}

/** Tabula: spravy, NAK a bez odpovede ako stlpce, najdlhsia pauza ako ciara na pravej osi. */
export function BoardChart({ data, withDate }: ChartProps<BoardBucket>) {
  const config = {
    acks: { label: t("stats.acks"), color: "#16a34a" },
    naks: { label: t("stats.naks"), color: "#dc2626" },
    unanswered: { label: t("stats.unanswered"), color: "#f59e0b" },
    maxGap: { label: t("stats.maxGap"), color: "#0ea5e9" },
  } satisfies ChartConfig;
  return (
    <ChartContainer config={config} className="aspect-auto h-64 w-full">
      <ComposedChart data={data}>
        <Axes withDate={withDate} order={Object.keys(config)} />
        <YAxis yAxisId="gap" orientation="right" tickLine={false} axisLine={false} width={48} unit=" s" />
        <Bar dataKey="acks" stackId="a" fill="var(--color-acks)" isAnimationActive={false} />
        <Bar dataKey="naks" stackId="a" fill="var(--color-naks)" isAnimationActive={false} />
        <Bar dataKey="unanswered" stackId="a" fill="var(--color-unanswered)" isAnimationActive={false} />
        <Line yAxisId="gap" dataKey="maxGap" stroke="var(--color-maxGap)" strokeWidth={2} dot={false} connectNulls={false} isAnimationActive={false} />
      </ComposedChart>
    </ChartContainer>
  );
}

const BAND: Record<string, string> = { ok: "bg-emerald-500", wait: "bg-amber-400", bad: "bg-red-500", off: "bg-zinc-400 dark:bg-zinc-600" };

/** Stav kazdej linky v case ako farebny pas (zelena pripojeny, oranzova caka, cervena vypadok, siva zastavena). */
export function StatusBands({ lines, changes, from, to }: { lines: number[]; changes: LineStatusChangeDto[]; from: number; to: number }) {
  const span = to - from;
  const percent = (value: number) => `${((value - from) / span) * 100}%`;
  return (
    <div className="flex flex-col gap-1.5 text-xs">
      {lines.map((line) => (
        <div key={line} className="grid grid-cols-[5rem_minmax(0,1fr)] items-center gap-2">
          <span className="text-muted-foreground">{t("stats.line", { line })}</span>
          <div className="relative h-4 overflow-hidden rounded-sm bg-muted">
            {statusSegments(changes, line, from, to).map((s) => (
              <div
                key={s.start}
                className={cn("absolute inset-y-0", s.status ? BAND[STATUS_TONES[s.status]] : "bg-transparent")}
                style={{ left: percent(s.start), width: `${((s.end - s.start) / span) * 100}%` }}
                title={`${s.status ? t(`line.status.${s.status}`) : t("stats.unknown")}${s.detail ? ` (${s.detail})` : ""}: ${timeLabel(s.start, true)} – ${timeLabel(s.end, true)}`}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
