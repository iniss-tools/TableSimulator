// Monitor hlaseni: zivy zoznam hlaseni INISSu z najnovsieho denneho archivu REP (ako archiv hlaseni, len priebezne).
// Najnovsie navrch, nove sa chvilu zvyraznia; vyber podla vlaku a textu, pozastavenie zoznamu pri citani.
import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pause, Play, TriangleAlert } from "lucide-react";
import { errorText, getReportMonitorOptions, PERMISSIONS, useCan, type ReportArchiveEntryDto } from "@/api";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useNow } from "@/hooks/use-now";
import { t } from "@/i18n";
import { dayText, reportColumns } from "./archive-columns";
import { matchesReport, MONITOR_LIMIT } from "./report-monitor";

/** Ako dlho sa nove hlasenie zvyraznuje [ms]. */
const HIGHLIGHT_MS = 5000;

/** Zaznam s poradim v dni (1 = prvy) - podla neho sa zvyraznuju nove. */
type Row = ReportArchiveEntryDto & { seq: number };

export function ReportMonitorPage() {
  const can = useCan();
  const query = useQuery(getReportMonitorOptions({ query: { limit: MONITOR_LIMIT } }));
  const monitor = query.data;
  const [train, setTrain] = useState("");
  const [text, setText] = useState("");
  // pozastaveny zoznam sa nemeni (da sa citat), nove hlasenia sa len pocitaju
  const [frozen, setFrozen] = useState<{ rows: Row[]; total: number } | null>(null);
  // nove hlasenia od poradia "from" do casu "until"
  const [highlight, setHighlight] = useState<{ from: number; until: number } | null>(null);
  const lastTotal = useRef<{ date: string | null; total: number } | null>(null);
  const now = useNow(1000);

  const rows = useMemo<Row[]>(() => (monitor ? monitor.entries.map((e, i) => ({ ...e, seq: monitor.total - i })) : []), [monitor]);

  useEffect(() => {
    if (!monitor) return;
    const last = lastTotal.current;
    // prve nacitanie a novy den sa nezvyraznuju
    if (last && last.date === monitor.date && monitor.total > last.total) setHighlight({ from: last.total + 1, until: Date.now() + HIGHLIGHT_MS });
    lastTotal.current = { date: monitor.date, total: monitor.total };
  }, [monitor]);

  const shown = (frozen?.rows ?? rows).filter((e) => matchesReport(e, train, text));
  const pending = frozen && monitor ? monitor.total - frozen.total : 0;
  const columns = useMemo(() => reportColumns(), []);
  const isNew = (row: Row) => !frozen && highlight !== null && now < highlight.until && row.seq >= highlight.from;

  return (
    <>
      <PageHeader description={t("monitor.hint")} />
      {query.error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{errorText(query.error)}</AlertDescription>
        </Alert>
      )}
      {!monitor ? (
        !query.error && <Skeleton className="h-40 w-full" />
      ) : !monitor.date ? (
        <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
          <TriangleAlert />
          <AlertDescription className="text-current">
            {t("monitor.noFile")}{" "}
            {can(PERMISSIONS.settings) && (
              <a className="underline" href="#/settings">
                {t("nav.settings")}
              </a>
            )}
          </AlertDescription>
        </Alert>
      ) : (
        <div className="flex flex-col gap-3">
          <div className="flex flex-wrap items-center gap-2">
            <Input
              className="w-36"
              placeholder={t("archive.filter.train")}
              aria-label={t("archive.filter.train")}
              value={train}
              onChange={(e) => setTrain(e.target.value)}
            />
            <Input
              className="w-56"
              placeholder={t("archive.filter.text")}
              aria-label={t("archive.filter.text")}
              value={text}
              onChange={(e) => setText(e.target.value)}
            />
            <Button variant="outline" onClick={() => setFrozen(frozen ? null : { rows, total: monitor.total })}>
              {frozen ? <Play /> : <Pause />}
              {frozen ? t("monitor.resume", { count: pending }) : t("monitor.pause")}
            </Button>
            {frozen ? <StatusBadge tone="wait">{t("monitor.paused")}</StatusBadge> : <StatusBadge tone="ok">{t("monitor.live")}</StatusBadge>}
          </div>
          <p className="text-sm text-muted-foreground">
            {t("monitor.file", { day: dayText(monitor.date), file: monitor.file ?? "" })} · {t("monitor.count", { shown: shown.length, total: monitor.total })}
            {monitor.skipped > 0 && ` ${t("archive.skipped", { count: monitor.skipped })}`}
          </p>
          <DataTable
            columns={columns}
            data={shown}
            search={false}
            pageSize={null}
            sortable={false}
            alignTop
            empty={t("monitor.empty")}
            rowClassName={(row) => (isNew(row as Row) ? "bg-amber-500/10 transition-colors" : undefined)}
          />
        </div>
      )}
    </>
  );
}
