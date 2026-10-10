// Ziva komunikacia na linkach - spravy INISSu a odpovede tabul, rozlozene aj v hex. Spravy pribudaju davkovo
// a tabulka kresli len viditelne riadky (TanStack Virtual), takze drzi tisice sprav bez spomalenia.
import { useEffect, useMemo, useRef, useState } from "react";
import { useTable } from "@tanstack/react-table";
import { useVirtualizer } from "@tanstack/react-virtual";
import { Eraser, Pause, Play } from "lucide-react";
import { clearLog, getLog, openSocket, PERMISSIONS, useCan, type FrameDto } from "@/api";
import { CheckField } from "@/components/common/form-field";
import { StatusBadge } from "@/components/common/status-badge";
import { columnsFor, features, type DataTableColumn } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { hasText, locale, t, type TextKey } from "@/i18n";

/** Najviac drzanych sprav. */
const MAX = 3000;
/** Ako casto sa nove spravy pridaju do tabulky [ms] - pri opakovani spravy INISSom chodia v rychlom slede. */
const FLUSH_MS = 250;
const TIME: Intl.DateTimeFormatOptions = { hour: "2-digit", minute: "2-digit", second: "2-digit", fractionalSecondDigits: 3, hour12: false };

/** Texty sprav vzdialeneho INISSu: fronta bez nazvu, nahravky len menami (RWAV = trojice meno,cislo,cislo), vysledok zapisu. */
function remoteArgs(frame: FrameDto, args: Record<string, string>): Record<string, string> {
  const result: Record<string, string> = { ...args, queue: args.queue || t("log.remoteQueueDefault") };
  if (args.recordings)
    result.recordings = args.recordings
      .split(",")
      .filter((_, i) => i % 3 === 0)
      .join(" ");
  if (frame.kind === "remote.reply" && args.result) result.result = t(`remote.result.${args.result}` as TextKey);
  return result;
}

function meaning(frame: FrameDto): string {
  let args = frame.args ?? {};
  let key = `kind.${frame.kind}`;
  if (frame.kind === "elen.moving" && args.last === "1") key = "kind.elen.movingLast";
  if (frame.kind === "elen.row" && !args.text) key = "kind.elen.rowEmpty";
  if (frame.kind === "fers.query" && args.rotating === "1") key = "kind.fers.queryRotating";
  if (frame.kind === "ers.status") {
    const state = `ers.state.${args.status}`;
    args = { ...args, meaning: hasText(state) ? t(state) : t("ers.state.other") };
  }
  const remote = frame.kind.startsWith("remote.");
  if (remote) {
    if (frame.kind === "remote.report" && !args.title) key = "kind.remote.reportUntitled";
    if (frame.kind === "remote.queue") key = args.off === "1" ? "kind.remote.queueOff" : "kind.remote.queueOn";
    if (frame.kind === "remote.reply" && !args.result) key = "kind.remote.replyQueues";
    args = remoteArgs(frame, args);
  }
  let text = hasText(key) ? t(key, args) : frame.kind;
  if (args.broadcast === "1") text += ` (${t("log.broadcast")})`;
  // sprava tabule zabalena v pakete ITP alebo v sprave vzdialeneho INISSu
  if (args.seq && !remote && !frame.kind.startsWith("itp."))
    text += ` · ${args.manufacturer ? t("log.remoteSeq", { seq: args.seq, manufacturer: args.manufacturer }) : t("log.itpSeq", { seq: args.seq })}`;
  return text;
}

function hex(h: string): string {
  return h.replace(/(..)(?!$)/g, "$1 ");
}

const column = columnsFor<FrameDto>();

/** Spravy z servera: najprv ulozene (/api/log), potom zive cez WebSocket (/ws?log=1); pri pozastaveni sa zahadzuju. */
function useFrames(paused: boolean): [FrameDto[], () => void] {
  const [frames, setFrames] = useState<FrameDto[]>([]);
  const pausedRef = useRef(paused);
  useEffect(() => {
    pausedRef.current = paused;
  }, [paused]);

  useEffect(() => {
    let lastSeq = 0;
    let pending: FrameDto[] = [];
    // spravy sa zbieraju a do tabulky idu davkovo - nie jedno prekreslenie na kazdu spravu
    const flush = () => {
      if (pending.length === 0) return;
      const batch = pending;
      pending = [];
      setFrames((old) => (old.length + batch.length > MAX ? [...old, ...batch].slice(-MAX) : [...old, ...batch]));
    };
    const timer = setInterval(flush, FLUSH_MS);
    const add = (incoming: FrameDto[]) => {
      const fresh = incoming.filter((f) => f.seq > lastSeq);
      if (fresh.length === 0) return;
      lastSeq = fresh[fresh.length - 1].seq;
      if (!pausedRef.current) pending.push(...fresh);
    };
    getLog({ throwOnError: true })
      .then(({ data }) => add(data))
      .catch(() => undefined);
    const close = openSocket(add, true);
    return () => {
      clearInterval(timer);
      close();
    };
  }, []);

  return [frames, () => setFrames([])];
}

export function LogPage() {
  const can = useCan();
  const [paused, setPaused] = useState(false);
  const [frames, clearFrames] = useFrames(paused);
  const [hideReplies, setHideReplies] = useState(true);
  const [line, setLine] = useState("");
  const [address, setAddress] = useState("");
  const scroller = useRef<HTMLDivElement>(null);

  // najnovsie hore
  const shown = useMemo(() => {
    const result: FrameDto[] = [];
    for (let i = frames.length - 1; i >= 0; i--) {
      const f = frames[i];
      if (hideReplies && f.direction !== "rx" && f.status === "ok") continue;
      if (line && f.line !== Number(line)) continue;
      if (address && f.address !== Number(address)) continue;
      result.push(f);
    }
    return result;
  }, [frames, hideReplies, line, address]);

  const columns = useMemo<DataTableColumn<FrameDto>[]>(
    () => [
      column.accessor("time", {
        header: t("log.col.time"),
        meta: { className: "w-28" },
        cell: ({ row }) => <span className="font-mono text-xs tabular-nums">{new Date(row.original.time).toLocaleTimeString(locale(), TIME)}</span>,
      }),
      column.accessor("line", { header: t("log.col.line"), meta: { className: "w-14" } }),
      column.accessor("direction", { header: t("log.col.dir"), meta: { className: "w-32" }, cell: ({ row }) => t(`log.dir.${row.original.direction}`) }),
      column.accessor((f) => f.address ?? "", { id: "address", header: t("log.col.address"), meta: { className: "w-16" } }),
      column.accessor(meaning, {
        id: "meaning",
        header: t("log.col.meaning"),
        cell: ({ row }) => {
          const f = row.original;
          const hintKey = `log.status.${f.status}.hint`;
          return (
            <div className="flex flex-col items-start gap-1 whitespace-normal">
              <span>
                {meaning(f)}
                {f.status !== "ok" && (
                  <StatusBadge tone="bad" title={hasText(hintKey) ? t(hintKey) : undefined}>
                    {t(`log.status.${f.status}`)}
                  </StatusBadge>
                )}
              </span>
              {f.status === "eotInData" && <span className="text-xs text-muted-foreground">{t("log.status.eotInData.hint")}</span>}
            </div>
          );
        },
      }),
      column.accessor("hex", {
        header: t("log.col.bytes"),
        meta: { className: "w-[38%]" },
        cell: ({ row }) => <code className="font-mono text-xs break-all whitespace-normal">{hex(row.original.hex)}</code>,
      }),
    ],
    [],
  );

  const table = useTable({ features, data: shown, columns, getRowId: (f) => String(f.seq), enableSorting: false });
  // vsetky riadky bez strankovania - kresli sa len viditelna cast
  const rows = table.getPrePaginatedRowModel().rows;
  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => scroller.current,
    estimateSize: () => 37,
    overscan: 12,
    getItemKey: (index) => rows[index].id,
  });
  const items = virtualizer.getVirtualItems();
  const top = items.length ? items[0].start : 0;
  const bottom = items.length ? virtualizer.getTotalSize() - items[items.length - 1].end : 0;

  return (
    <div className="flex h-[calc(100dvh-5rem)] min-h-96 flex-col gap-3">
      <div className="flex flex-wrap items-center gap-3">
        <label className="flex items-center gap-2 text-sm">
          {t("log.filter.line")}
          <Input className="w-16" inputMode="numeric" value={line} onChange={(e) => setLine(e.target.value.replace(/\D/g, ""))} />
        </label>
        <label className="flex items-center gap-2 text-sm">
          {t("log.filter.address")}
          <Input className="w-16" inputMode="numeric" value={address} onChange={(e) => setAddress(e.target.value.replace(/\D/g, ""))} />
        </label>
        <CheckField label={t("log.hideReplies")} checked={hideReplies} onCheckedChange={setHideReplies} className="w-auto" />
        <span className="ml-auto text-sm text-muted-foreground">{t("table.count", { count: shown.length })}</span>
        <Button variant={paused ? "default" : "outline"} onClick={() => setPaused(!paused)}>
          {paused ? <Play /> : <Pause />}
          {paused ? t("log.resume") : t("log.pause")}
        </Button>
        {can(PERMISSIONS.boards) && (
          <Button
            variant="outline"
            onClick={() => {
              void clearLog();
              clearFrames();
            }}
          >
            <Eraser />
            {t("log.clear")}
          </Button>
        )}
      </div>
      <div ref={scroller} className="min-h-0 flex-1 overflow-auto rounded-lg border">
        {/* bez obalu Table - hlavicka sa lepi k tomuto posuvaniu */}
        <table className="w-full table-fixed text-sm">
          <TableHeader className="sticky top-0 z-10 bg-background">
            {table.getHeaderGroups().map((group) => (
              <TableRow key={group.id}>
                {group.headers.map((header) => (
                  <TableHead key={header.id} className={header.column.columnDef.meta?.className}>
                    <table.FlexRender header={header} />
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-20 text-center text-muted-foreground">
                  {t("log.empty")}
                </TableCell>
              </TableRow>
            )}
            {top > 0 && <tr style={{ height: top }} />}
            {items.map((item) => {
              const row = rows[item.index];
              const f = row.original;
              return (
                <TableRow
                  key={row.id}
                  data-index={item.index}
                  ref={virtualizer.measureElement}
                  className={cn(f.direction === "tx" && "text-muted-foreground", f.status === "noBoard" && "opacity-60")}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="align-top">
                      <table.FlexRender cell={cell} />
                    </TableCell>
                  ))}
                </TableRow>
              );
            })}
            {bottom > 0 && <tr style={{ height: bottom }} />}
          </TableBody>
        </table>
      </div>
    </div>
  );
}
