// Statistika komunikacie v case: obdobie (hodina az 30 dni, posun dozadu), linka, grafy sprav, odpovedi a bajtov,
// stav liniek a tabule so suctami a grafom vybranej tabule. Minutove suhrny uklada server, novy udaj pribudne kazdu minutu.
import { useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { getTrafficStatisticsOptions, useSimulatorState } from "@/api";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { locale, t } from "@/i18n";
import { activeLines, boardBuckets, boardTotals, bucketTimes, lineBuckets, RANGES, rangeEnd, type RangeKey } from "./statistics";
import { BoardChart, BytesChart, MessagesChart, RepliesChart, StatusBands } from "./TrafficCharts";

/** Vsetky linky vo vybere. */
const ALL = 0;

const RANGE_KEYS = Object.keys(RANGES) as RangeKey[];

function formatGap(ms: number | null): string {
  if (ms === null) return "–";
  const seconds = ms / 1000;
  return seconds < 120 ? `${seconds.toLocaleString(locale(), { maximumFractionDigits: 1 })} s` : `${Math.round(seconds / 60).toLocaleString(locale())} min`;
}

export function StatisticsPage() {
  const live = useSimulatorState();
  const [range, setRange] = useState<RangeKey>("hour");
  const [offset, setOffset] = useState(0);
  const [line, setLine] = useState(ALL);
  const [selected, setSelected] = useState<string | null>(null);
  const now = useNow(30_000);
  const to = rangeEnd(now, range, offset);
  const from = to - RANGES[range] * 60_000;
  const query = useQuery({
    ...getTrafficStatisticsOptions({ query: { from: new Date(from).toISOString(), to: new Date(to).toISOString(), line: line === ALL ? undefined : line } }),
    placeholderData: keepPreviousData,
  });
  const data = query.data;

  const view = useMemo(() => {
    if (!data) return null;
    const times = bucketTimes(data);
    const lines = lineBuckets(data.lines, times, null);
    const totals = boardTotals(data.boards);
    const sum = lines.reduce(
      (s, b) => ({
        messages: s.messages + b.ok + b.badChecksum + b.noBoard + b.other,
        errors: s.errors + b.badChecksum + b.noBoard + b.noise + b.other,
        faults: s.faults + b.faults,
        connections: s.connections + b.connections,
        disconnections: s.disconnections + b.disconnections,
      }),
      { messages: 0, errors: 0, faults: 0, connections: 0, disconnections: 0 },
    );
    return { times, lines, totals, sum, active: activeLines(data) };
  }, [data]);

  const withDate = range !== "hour" && range !== "day";
  const selectedTotal = view?.totals.find((b) => `${b.line}-${b.address}` === selected) ?? null;
  const boardName = (lineNumber: number, address: number) => live.boards.find((b) => b.line === lineNumber && b.address === address)?.name ?? "";
  const lineOptions = [
    { value: ALL, label: t("stats.allLines") },
    ...live.lines.map((l) => ({ value: l.number, label: l.name ? `${l.number} – ${l.name}` : String(l.number) })),
  ];
  const dateRange = new Intl.DateTimeFormat(locale(), { dateStyle: "short", timeStyle: "short" }).formatRange(new Date(from), new Date(to));

  return (
    <>
      <PageHeader description={t("stats.hint", { days: data?.retentionDays ?? 30 })} />
      {/* ovladace vo vlastnom riadku s pevnou sirkou - pri posune obdobia nesmu preskakovat pod popis */}
      <div className="mb-2 flex flex-wrap items-center gap-2">
        <OptionSelect<number> value={line} onValueChange={setLine} options={lineOptions} ariaLabel={t("field.line")} className="w-56" />
        <ToggleGroup
          variant="outline"
          value={[range]}
          onValueChange={(values: string[]) => {
            const next = values[0] as RangeKey | undefined;
            if (next) {
              setRange(next);
              setOffset(0);
            }
          }}
        >
          {RANGE_KEYS.map((key) => (
            <ToggleGroupItem key={key} value={key}>
              {t(`stats.range.${key}`)}
            </ToggleGroupItem>
          ))}
        </ToggleGroup>
        <div className="flex items-center gap-1">
          <Button variant="outline" size="icon" title={t("stats.earlier")} onClick={() => setOffset(offset + 1)}>
            <ChevronLeft />
          </Button>
          <Button variant="outline" size="icon" title={t("stats.later")} disabled={offset === 0} onClick={() => setOffset(offset - 1)}>
            <ChevronRight />
          </Button>
          <Button variant="outline" disabled={offset === 0} onClick={() => setOffset(0)}>
            {t("stats.now")}
          </Button>
        </div>
      </div>

      <p className="mb-4 text-sm text-muted-foreground">
        {dateRange}
        {data && <> · {t("stats.bucket", { minutes: data.bucketMinutes })}</>}
        {view && (
          <>
            {" "}
            · {t("stats.summary", { messages: view.sum.messages, errors: view.sum.errors, faults: view.sum.faults })} ·{" "}
            {t("stats.connections", { connections: view.sum.connections, disconnections: view.sum.disconnections })}
          </>
        )}
      </p>

      {!view ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <div className={cn("grid gap-4 xl:grid-cols-2", query.isFetching && "opacity-80")}>
          <Card className="xl:col-span-2">
            <CardHeader>
              <CardTitle>{t("stats.messages")}</CardTitle>
              <CardDescription>{t("stats.messages.hint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <MessagesChart data={view.lines} withDate={withDate} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>{t("stats.repliesTitle")}</CardTitle>
              <CardDescription>{t("stats.replies.hint")}</CardDescription>
            </CardHeader>
            <CardContent>
              <RepliesChart data={view.lines} withDate={withDate} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>{t("stats.bytes")}</CardTitle>
            </CardHeader>
            <CardContent>
              <BytesChart data={view.lines} withDate={withDate} />
            </CardContent>
          </Card>
          <Card className="xl:col-span-2">
            <CardHeader>
              <CardTitle>{t("stats.status")}</CardTitle>
              <CardDescription>{t("stats.status.hint")}</CardDescription>
            </CardHeader>
            <CardContent>
              {view.active.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t("stats.noStatus")}</p>
              ) : (
                <StatusBands lines={view.active} changes={data!.statusChanges} from={Date.parse(data!.from)} to={Date.parse(data!.to)} />
              )}
            </CardContent>
          </Card>
          <Card className="xl:col-span-2">
            <CardHeader>
              <CardTitle>{t("stats.boards")}</CardTitle>
              <CardDescription>{t("stats.boards.hint")}</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              {view.totals.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t("stats.noBoards")}</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t("stats.col.board")}</TableHead>
                      <TableHead className="text-right">{t("stats.col.frames")}</TableHead>
                      <TableHead className="text-right">{t("stats.acks")}</TableHead>
                      <TableHead className="text-right">{t("stats.naks")}</TableHead>
                      <TableHead className="text-right">{t("stats.unanswered")}</TableHead>
                      <TableHead className="text-right">{t("stats.maxGap")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {view.totals.map((b) => {
                      const id = `${b.line}-${b.address}`;
                      const name = boardName(b.line, b.address);
                      return (
                        <TableRow
                          key={id}
                          data-state={id === selected ? "selected" : undefined}
                          className="cursor-pointer"
                          onClick={() => setSelected(id === selected ? null : id)}
                        >
                          <TableCell>
                            {t("stats.board", { line: b.line, address: b.address })}
                            {name && <span className="text-muted-foreground"> · {name}</span>}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">{b.frames}</TableCell>
                          <TableCell className="text-right tabular-nums">{b.acks}</TableCell>
                          <TableCell className={cn("text-right tabular-nums", b.naks > 0 && "text-red-600 dark:text-red-400")}>{b.naks}</TableCell>
                          <TableCell className={cn("text-right tabular-nums", b.unanswered > 0 && "text-amber-600 dark:text-amber-400")}>
                            {b.unanswered}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">{formatGap(b.maxGapMs)}</TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              )}
              {selectedTotal && (
                <div>
                  <p className="mb-2 text-sm font-medium">
                    {t("stats.board", { line: selectedTotal.line, address: selectedTotal.address })} {boardName(selectedTotal.line, selectedTotal.address)}
                  </p>
                  <BoardChart data={boardBuckets(data!.boards, view.times, selectedTotal.line, selectedTotal.address)} withDate={withDate} />
                </div>
              )}
              {view.totals.length > 0 && !selectedTotal && <p className="text-sm text-muted-foreground">{t("stats.selectBoard")}</p>}
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}
