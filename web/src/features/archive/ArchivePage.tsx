// Archiv tabul a hlaseni INISSu - denne subory TAB/REP z priecinkov logov (ako v INISSView). Filtruje a strankuje server.
import { useCallback, useEffect, useMemo, useState, type SubmitEvent } from "react";
import { ListPlus, RefreshCw, Search, TriangleAlert } from "lucide-react";
import {
  errorText,
  getArchiveDays,
  getReportArchive,
  getTableArchive,
  PERMISSIONS,
  useCan,
  type ArchiveDaysDto,
  type ArchiveTrainDto,
  type GetTableArchiveData,
  type ReportArchiveEntryDto,
  type TableArchiveEntryDto,
} from "@/api";
import { CheckField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { hasText, t } from "@/i18n";

const PAGE = 200;

export type ArchiveKind = "tables" | "reports";
type ArchiveQuery = NonNullable<GetTableArchiveData["query"]>;

/** Den z API (rrrr-mm-dd) ako d. m. rrrr. */
function dayText(value: string): string {
  const [year, month, day] = value.split("-");
  return `${Number(day)}. ${Number(month)}. ${year}`;
}

function Train({ train }: { train: ArchiveTrainDto }) {
  return (
    <span>
      <strong className="whitespace-nowrap">{[train.type, train.number].filter(Boolean).join(" ")}</strong> {train.name}
      {train.date && <span className="text-xs text-muted-foreground"> · {dayText(train.date)}</span>}
    </span>
  );
}

/** Kolaj prichodu, pripadne aj odlisna kolaj odchodu. */
function tracks(arrival: string, departure: string): string {
  return departure && departure !== arrival ? `${arrival || "–"} / ${departure}` : arrival;
}

/** Meskanie prichodu, pripadne aj odlisne meskanie odchodu. */
function delays(arrival: number | null, departure: number | null): string {
  if (arrival === null && departure === null) return "";
  const value = departure !== null && departure !== arrival ? `${arrival ?? "–"} / ${departure}` : String(arrival);
  return t("archive.minutes", { value });
}

function detailText(key: string, value: string): string {
  const labelKey = `archive.detail.${key}`;
  const label = hasText(labelKey) ? t(labelKey) : key;
  return value === "true" ? label : `${label}: ${value}`;
}

const tableColumn = columnsFor<TableArchiveEntryDto>();
const reportColumn = columnsFor<ReportArchiveEntryDto>();

function tableColumns() {
  return [
    tableColumn.accessor("time", { header: t("archive.col.time"), meta: { className: "w-20" } }),
    tableColumn.accessor("station", { header: t("archive.col.station") }),
    tableColumn.display({
      id: "train",
      header: t("archive.col.train"),
      cell: ({ row }) => {
        const e = row.original;
        return e.train ? (
          <Train train={e.train} />
        ) : (
          <div className="flex flex-col whitespace-normal">
            <span className="text-xs text-muted-foreground">{t("archive.infoText")}</span>
            {e.text || <span className="text-muted-foreground">{t("archive.textRemoved")}</span>}
          </div>
        );
      },
    }),
    tableColumn.display({
      id: "tables",
      header: t("archive.col.tables"),
      cell: ({ row }) => {
        const e = row.original;
        return (
          <div className="flex flex-col gap-1.5 whitespace-normal">
            {e.tables.length === 0 && e.train && <span className="text-muted-foreground">{t("archive.noTables")}</span>}
            {e.tables.map((table, j) => (
              <div key={j}>
                <span className="font-medium">{table.names.join(", ")}</span>
                {table.logical && <span className="text-xs text-muted-foreground"> · {t("archive.logical", { name: table.logical, row: table.row })}</span>}
                {table.columns.length > 0 && (
                  <div className="flex flex-wrap gap-x-3.5 gap-y-0.5">
                    {table.columns.map((c, k) => (
                      <span key={k}>
                        <span className="text-muted-foreground">{c.name}</span> {c.value || <span className="text-muted-foreground">–</span>}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        );
      },
    }),
  ];
}

function reportColumns() {
  return [
    reportColumn.accessor("time", { header: t("archive.col.time"), meta: { className: "w-20" } }),
    reportColumn.accessor("kind", { header: t("archive.col.kind") }),
    reportColumn.display({
      id: "name",
      header: t("archive.col.name"),
      cell: ({ row }) => (
        <div className="flex flex-col">
          {row.original.name}
          {row.original.auto && <span className="text-xs text-muted-foreground">{t("archive.auto")}</span>}
        </div>
      ),
    }),
    reportColumn.display({
      id: "train",
      header: t("archive.col.train"),
      cell: ({ row }) => (
        <div className="flex flex-col">
          {row.original.train && <Train train={row.original.train} />}
          {row.original.station && <span className="text-xs text-muted-foreground">{row.original.station}</span>}
        </div>
      ),
    }),
    reportColumn.display({ id: "track", header: t("archive.col.track"), cell: ({ row }) => tracks(row.original.arrivalTrack, row.original.departureTrack) }),
    reportColumn.display({ id: "delay", header: t("archive.col.delay"), cell: ({ row }) => delays(row.original.arrivalDelay, row.original.departureDelay) }),
    reportColumn.accessor("text", {
      header: t("archive.col.text"),
      cell: ({ getValue }) => <div className="max-w-[40em] min-w-[16em] whitespace-normal">{getValue()}</div>,
    }),
    reportColumn.display({
      id: "result",
      header: t("archive.col.result"),
      cell: ({ row }) => {
        const value = row.original.result;
        if (!value) return null;
        const key = `archive.result.${value}`;
        return <StatusBadge tone={value === "OK" ? "ok" : "bad"}>{hasText(key) ? t(key) : value}</StatusBadge>;
      },
    }),
    reportColumn.display({
      id: "details",
      header: t("archive.col.details"),
      cell: ({ row }) => (
        <div className="flex flex-col text-xs">
          {row.original.details.map((d) => (
            <span key={d.key}>{detailText(d.key, d.value)}</span>
          ))}
        </div>
      ),
    }),
  ];
}

export function ArchivePage({ kind }: { kind: ArchiveKind }) {
  const can = useCan();
  const [days, setDays] = useState<ArchiveDaysDto | null>(null);
  const [date, setDate] = useState("");
  const [train, setTrain] = useState("");
  const [text, setText] = useState("");
  const [filter, setFilter] = useState({ train: "", text: "" });
  const [newest, setNewest] = useState(true);
  const [tables, setTables] = useState<TableArchiveEntryDto[]>([]);
  const [reports, setReports] = useState<ReportArchiveEntryDto[]>([]);
  const [total, setTotal] = useState(0);
  const [skipped, setSkipped] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const loadDays = useCallback(
    () =>
      getArchiveDays({ throwOnError: true })
        .then(({ data: d }) => {
          const list = kind === "tables" ? d.tables : d.reports;
          setDays(d);
          setDate((old) => (old && list.includes(old) ? old : (list[0] ?? "")));
          setError(null);
        })
        .catch((e) => setError(errorText(e))),
    [kind],
  );
  useEffect(() => {
    void loadDays();
  }, [loadDays]);

  const load = useCallback(
    (offset: number) => {
      if (!date) return;
      const query: ArchiveQuery = { train: filter.train || undefined, text: filter.text || undefined, newest, offset, limit: PAGE };
      const done = (page: { total: number; skipped: number }) => {
        setTotal(page.total);
        setSkipped(page.skipped);
        setError(null);
      };
      const request =
        kind === "tables"
          ? getTableArchive({ path: { date }, query, throwOnError: true }).then(({ data: page }) => {
              setTables((old) => (offset ? [...old, ...page.entries] : page.entries));
              done(page);
            })
          : getReportArchive({ path: { date }, query, throwOnError: true }).then(({ data: page }) => {
              setReports((old) => (offset ? [...old, ...page.entries] : page.entries));
              done(page);
            });
      request.catch((e) => setError(errorText(e)));
    },
    [kind, date, filter, newest],
  );
  useEffect(() => load(0), [load]);

  const search = (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setFilter({ train: train.trim(), text: text.trim() });
  };

  const tableCols = useMemo(() => tableColumns(), []);
  const reportCols = useMemo(() => reportColumns(), []);
  const list = days ? (kind === "tables" ? days.tables : days.reports) : [];
  const shown = kind === "tables" ? tables.length : reports.length;

  return (
    <>
      <PageHeader description={t(kind === "tables" ? "archive.hint.tables" : "archive.hint.reports")} />
      {error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      {days && list.length === 0 ? (
        <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
          <TriangleAlert />
          <AlertDescription className="text-current">
            {t("archive.noDays")}{" "}
            {can(PERMISSIONS.settings) && (
              <a className="underline" href="#/settings">
                {t("nav.settings")}
              </a>
            )}
          </AlertDescription>
        </Alert>
      ) : (
        days && (
          <div className="flex flex-col gap-3">
            <form className="flex flex-wrap items-center gap-2" onSubmit={search}>
              <OptionSelect value={date} onValueChange={setDate} ariaLabel={t("archive.day")} options={list.map((d) => ({ value: d, label: dayText(d) }))} />
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
              <Button type="submit" variant="outline">
                <Search />
                {t("archive.filter.apply")}
              </Button>
              <CheckField label={t("archive.newest")} checked={newest} onCheckedChange={setNewest} className="w-auto" />
              <Button type="button" variant="outline" className="ml-auto" onClick={() => loadDays().then(() => load(0))}>
                <RefreshCw />
                {t("archive.refresh")}
              </Button>
            </form>
            <p className="text-sm text-muted-foreground">
              {t("archive.count", { shown, total })}
              {skipped > 0 && ` ${t("archive.skipped", { count: skipped })}`}
            </p>
            {kind === "tables" ? (
              <DataTable columns={tableCols} data={tables} search={false} pageSize={null} sortable={false} alignTop empty={t("archive.empty")} />
            ) : (
              <DataTable columns={reportCols} data={reports} search={false} pageSize={null} sortable={false} alignTop empty={t("archive.empty")} />
            )}
            {shown < total && (
              <Button variant="outline" className="self-start" onClick={() => load(shown)}>
                <ListPlus />
                {t("archive.more")}
              </Button>
            )}
          </div>
        )
      )}
    </>
  );
}
