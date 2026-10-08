// Nacitanie liniek a tabul z instalacie INISSu nastavenej v Nastaveniach: nahlad (linky, tabule, zistenia) a import.
import { useMemo } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { CircleAlert, FolderOpen, Info, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import {
  ApiError,
  errorText,
  getInissStationOptions,
  importInissStationMutation,
  type InissLineDto,
  type InissProblemDto,
  type InissTableDto,
  type StationImportedDto,
} from "@/api";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";
import { hasText, t, type TextKey } from "@/i18n";

const SEVERITY_CLASS: Record<InissProblemDto["severity"], string> = {
  error: "text-red-700 dark:text-red-400",
  warning: "text-amber-700 dark:text-amber-400",
  info: "text-muted-foreground",
};

/** Text zistenia - parametre zo servera v poradi {0}, {1}… */
function problemText(problem: InissProblemDto): string {
  return t(`iniss.problem.${problem.code}` as TextKey, Object.fromEntries(problem.args.map((a, i) => [String(i), a])));
}

function className(tableClass: number): string {
  const key = `line.class.${tableClass}`;
  return hasText(key) ? t(key) : t("iniss.class", { value: tableClass });
}

function Tables({ tables }: { tables: InissTableDto[] }) {
  if (tables.length === 0) return <span className="text-muted-foreground">–</span>;
  return (
    <div className="flex flex-col gap-0.5 whitespace-normal">
      {tables.map((table) => (
        <span key={`${table.grafikon}/${table.name}`} className={table.shown ? undefined : "text-muted-foreground"}>
          {t("iniss.table", { name: table.name, address: table.address, manufacturer: table.manufacturer || "?" })}
          {table.automatic && <span className="text-xs text-muted-foreground"> · {t("iniss.automatic")}</span>}
          {!table.shown && <span className="text-xs"> · {t("iniss.notShown")}</span>}
        </span>
      ))}
    </div>
  );
}

const column = columnsFor<InissLineDto>();

function importedText(result: StationImportedDto): string {
  const parts = [
    t("iniss.imported.lines", { added: result.linesAdded, updated: result.linesUpdated }),
    t("iniss.imported.boards", { added: result.boardsAdded, kept: result.boardsKept }),
  ];
  if (result.virtualBoardsAdded > 0 || result.virtualBoardsKept > 0)
    parts.push(t("iniss.imported.virtual", { added: result.virtualBoardsAdded, kept: result.virtualBoardsKept }));
  if (result.logFolder !== "none" && result.logFolder !== "watched") parts.push(t(`iniss.logFolder.${result.logFolder}` as TextKey));
  return parts.join(" ");
}

export function InissStationDialog({ onClose }: { onClose: () => void }) {
  // pri kazdom otvoreni znova - instalacia INISSu sa mohla zmenit
  const query = useQuery({ ...getInissStationOptions(), staleTime: 0, gcTime: 0, retry: false });
  const station = query.data;
  const load = useMutation({
    ...importInissStationMutation(),
    onSuccess: (result) => {
      toast.success(t("iniss.imported"), { description: importedText(result), duration: 10_000 });
      onClose();
    },
  });

  const columns = useMemo(
    () => [
      column.accessor((l) => l.number ?? 0, { id: "number", header: t("iniss.col.line"), cell: ({ row }) => row.original.number ?? "?" }),
      column.accessor("section", { header: t("iniss.col.section") }),
      column.accessor((l) => className(l.tableClass), { id: "class", header: t("iniss.col.class") }),
      column.display({
        id: "port",
        header: t("iniss.col.port"),
        cell: ({ row }) => (
          <div className="flex flex-col">
            <code className="font-mono text-xs">{row.original.port}</code>
            <span className="text-xs text-muted-foreground">{t("iniss.simulatorPort", { port: row.original.simulatorPort })}</span>
          </div>
        ),
      }),
      column.display({ id: "tables", header: t("iniss.col.tables"), cell: ({ row }) => <Tables tables={row.original.tables} /> }),
      column.display({
        id: "state",
        header: t("iniss.col.state"),
        cell: ({ row }) => {
          const line = row.original;
          return (
            <div className="flex max-w-xs min-w-40 flex-col items-start gap-1 whitespace-normal">
              {!line.active ? (
                <StatusBadge tone="bad">{t("iniss.inactive")}</StatusBadge>
              ) : !line.supported ? (
                <StatusBadge tone="off">{t("iniss.unsupported")}</StatusBadge>
              ) : (
                <StatusBadge tone="ok">{t("iniss.loaded")}</StatusBadge>
              )}
              {line.problems.map((p, i) => (
                <span key={i} className={cn("text-xs", SEVERITY_CLASS[p.severity])}>
                  {problemText(p)}
                </span>
              ))}
            </div>
          );
        },
      }),
    ],
    [],
  );

  const notConfigured = query.error instanceof ApiError && query.error.code === "iniss.notConfigured";
  const nothing = station !== undefined && !station.lines.some((l) => l.active && l.supported) && station.xmlTables.length === 0;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] grid-rows-[auto_minmax(0,1fr)_auto] sm:max-w-5xl">
        <DialogHeader>
          <DialogTitle>{t("iniss.title")}</DialogTitle>
          <DialogDescription>{t("iniss.hint")}</DialogDescription>
        </DialogHeader>
        <div className="-mx-4 flex min-h-0 flex-col gap-4 overflow-y-auto px-4 text-sm">
          {query.isPending && <Skeleton className="h-40 w-full" />}
          {query.error && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription>
                {errorText(query.error)}{" "}
                {notConfigured && (
                  <a className="underline" href="#/settings" onClick={onClose}>
                    {t("nav.settings")}
                  </a>
                )}
              </AlertDescription>
            </Alert>
          )}
          {station && (
            <>
              <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-4 gap-y-1">
                <dt className="text-muted-foreground">{t("iniss.program")}</dt>
                <dd>
                  <code className="font-mono text-xs break-all">{station.program}</code>
                </dd>
                <dt className="text-muted-foreground">{t("iniss.config")}</dt>
                <dd>{t(station.registry ? "iniss.config.registry" : "iniss.config.ini", { name: station.appName })}</dd>
                <dt className="text-muted-foreground">{t("iniss.grafikons")}</dt>
                <dd>{station.grafikons.length ? station.grafikons.join(", ") : "–"}</dd>
                <dt className="text-muted-foreground">{t("iniss.virtual")}</dt>
                <dd className="flex flex-wrap items-center gap-1">
                  <FolderOpen className="size-3.5 text-muted-foreground" />
                  <code className="font-mono text-xs break-all">{station.logFolder}</code>
                  {!station.logFolderExists && <StatusBadge tone="wait">{t("iniss.logMissing")}</StatusBadge>}
                  <span className="text-muted-foreground">· {t("iniss.xmlTables", { count: station.xmlTables.length })}</span>
                </dd>
              </dl>
              <DataTable
                columns={columns}
                data={station.lines}
                getRowId={(l) => l.section}
                search={false}
                pageSize={null}
                sortable={false}
                alignTop
                empty={t("iniss.noLines")}
              />
              {station.unserved.length > 0 && (
                <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
                  <TriangleAlert />
                  <AlertDescription className="text-current">
                    <p className="font-medium">{t("iniss.unserved")}</p>
                    <ul className="list-disc pl-4">
                      {station.unserved.map((u) => (
                        <li key={`${u.grafikon}/${u.name}`}>
                          {t("iniss.table", { name: u.name, address: u.address, manufacturer: u.manufacturer || "?" })}
                          {u.problem && ` – ${problemText(u.problem)}`}
                        </li>
                      ))}
                    </ul>
                  </AlertDescription>
                </Alert>
              )}
              <Alert>
                <Info />
                <AlertDescription>{t("iniss.keep")}</AlertDescription>
              </Alert>
            </>
          )}
          {load.error && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription className="whitespace-pre-line">{errorText(load.error)}</AlertDescription>
            </Alert>
          )}
        </div>
        <DialogFooter>
          <DialogClose render={<Button type="button" variant="outline" />}>{t("common.cancel")}</DialogClose>
          <Button type="button" disabled={!station || nothing || load.isPending} onClick={() => load.mutate({})}>
            {load.isPending && <Spinner />}
            {t("iniss.load")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
