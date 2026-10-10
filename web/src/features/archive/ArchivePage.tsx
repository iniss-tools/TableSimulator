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
  type GetTableArchiveData,
  type ReportArchiveEntryDto,
  type TableArchiveEntryDto,
} from "@/api";
import { CheckField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/data-table/data-table";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { dayText, reportColumns, tableColumns } from "./archive-columns";

const PAGE = 200;

export type ArchiveKind = "tables" | "reports";
type ArchiveQuery = NonNullable<GetTableArchiveData["query"]>;

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
