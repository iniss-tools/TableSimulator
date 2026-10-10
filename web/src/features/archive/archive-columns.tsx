// Stlpce archivu tabul a hlaseni INISSu (zaznamy z dennych suborov TAB/REP) - archiv aj monitor hlaseni.
import type { ArchiveTrainDto, ReportArchiveEntryDto, TableArchiveEntryDto } from "@/api";
import { StatusBadge } from "@/components/common/status-badge";
import { columnsFor } from "@/components/data-table/features";
import { hasText, t } from "@/i18n";

/** Den z API (rrrr-mm-dd) ako d. m. rrrr. */
export function dayText(value: string): string {
  const [year, month, day] = value.split("-");
  return `${Number(day)}. ${Number(month)}. ${year}`;
}

/** Vlak - druh a cislo tucne, nazov, pripadne den jazdy (bunka tabulky, nie komponent). */
function trainCell(train: ArchiveTrainDto) {
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

export function tableColumns() {
  return [
    tableColumn.accessor("time", { header: t("archive.col.time"), meta: { className: "w-20" } }),
    tableColumn.accessor("station", { header: t("archive.col.station") }),
    tableColumn.display({
      id: "train",
      header: t("archive.col.train"),
      cell: ({ row }) => {
        const e = row.original;
        return e.train ? (
          trainCell(e.train)
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

export function reportColumns() {
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
          {row.original.train && trainCell(row.original.train)}
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
