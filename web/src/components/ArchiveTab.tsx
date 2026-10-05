// Archiv tabul a hlaseni INISSu - denne subory TAB/REP z priecinkov logov (ako v INISSView).
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { api, ApiError, PERMISSIONS, type ArchiveDays, type ArchiveQuery, type ArchiveTrain, type ReportArchiveEntry, type TableArchiveEntry } from "../api";
import { hasText, t, useLanguage } from "../i18n";
import { useCan } from "../store";

const PAGE = 200;

export type ArchiveKind = "tables" | "reports";

const errorText = (e: unknown) => (e instanceof ApiError ? e.message : String(e));

/** Den z API (rrrr-mm-dd) ako d. m. rrrr. */
function dayText(value: string): string {
  const [year, month, day] = value.split("-");
  return `${Number(day)}. ${Number(month)}. ${year}`;
}

function Train({ train }: { train: ArchiveTrain }) {
  return (
    <span>
      <strong className="nowrap">{[train.type, train.number].filter(Boolean).join(" ")}</strong> {train.name}
      {train.date && <span className="muted small"> · {dayText(train.date)}</span>}
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

/** Vysledok hlasenia v jazyku obsluhy; neznamy ostane, ako ho zapisal INISS. */
function Result({ value }: { value: string }) {
  if (!value) return null;
  const key = `archive.result.${value}`;
  return <span className={`status ${value === "OK" ? "status-connected" : "status-error"}`}>{hasText(key) ? t(key) : value}</span>;
}

function detailText(key: string, value: string): string {
  const labelKey = `archive.detail.${key}`;
  const label = hasText(labelKey) ? t(labelKey) : key;
  return value === "true" ? label : `${label}: ${value}`;
}

function TablesList({ entries }: { entries: TableArchiveEntry[] }) {
  return (
    <div className="table-wrap">
      <table className="archive">
        <thead>
          <tr>
            <th>{t("archive.col.time")}</th>
            <th>{t("archive.col.station")}</th>
            <th>{t("archive.col.train")}</th>
            <th>{t("archive.col.tables")}</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((e, i) => (
            <tr key={i}>
              <td className="nowrap">{e.time}</td>
              <td>{e.station}</td>
              <td>
                {e.train ? (
                  <Train train={e.train} />
                ) : (
                  <>
                    <span className="muted small">{t("archive.infoText")}</span>
                    <div>{e.text || <span className="muted">{t("archive.textRemoved")}</span>}</div>
                  </>
                )}
              </td>
              <td>
                {e.tables.length === 0 && e.train && <span className="muted">{t("archive.noTables")}</span>}
                {e.tables.map((table, j) => (
                  <div key={j} className="archive-group">
                    <span className="archive-names">{table.names.join(", ")}</span>
                    {table.logical && <span className="muted small"> · {t("archive.logical", { name: table.logical, row: table.row })}</span>}
                    {table.columns.length > 0 && (
                      <div className="archive-columns">
                        {table.columns.map((c, k) => (
                          <span key={k}>
                            <span className="muted">{c.name}</span> {c.value || <span className="muted">–</span>}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ReportsList({ entries }: { entries: ReportArchiveEntry[] }) {
  return (
    <div className="table-wrap">
      <table className="archive">
        <thead>
          <tr>
            <th>{t("archive.col.time")}</th>
            <th>{t("archive.col.kind")}</th>
            <th>{t("archive.col.name")}</th>
            <th>{t("archive.col.train")}</th>
            <th>{t("archive.col.track")}</th>
            <th>{t("archive.col.delay")}</th>
            <th>{t("archive.col.text")}</th>
            <th>{t("archive.col.result")}</th>
            <th>{t("archive.col.details")}</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((e, i) => (
            <tr key={i}>
              <td className="nowrap">{e.time}</td>
              <td>{e.kind}</td>
              <td>
                {e.name}
                {e.auto && <div className="muted small">{t("archive.auto")}</div>}
              </td>
              <td>
                {e.train && <Train train={e.train} />}
                {e.station && <div className="muted small">{e.station}</div>}
              </td>
              <td className="nowrap">{tracks(e.arrivalTrack, e.departureTrack)}</td>
              <td className="nowrap">{delays(e.arrivalDelay, e.departureDelay)}</td>
              <td className="archive-text">{e.text}</td>
              <td>
                <Result value={e.result} />
              </td>
              <td className="small">
                {e.details.map((d) => (
                  <div key={d.key}>{detailText(d.key, d.value)}</div>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function ArchiveTab({ kind }: { kind: ArchiveKind }) {
  useLanguage();
  const can = useCan();
  const [days, setDays] = useState<ArchiveDays | null>(null);
  const [date, setDate] = useState("");
  const [train, setTrain] = useState("");
  const [text, setText] = useState("");
  const [filter, setFilter] = useState({ train: "", text: "" });
  const [newest, setNewest] = useState(true);
  const [tables, setTables] = useState<TableArchiveEntry[]>([]);
  const [reports, setReports] = useState<ReportArchiveEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [skipped, setSkipped] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const loadDays = useCallback(
    () =>
      api
        .archiveDays()
        .then((d) => {
          const list = kind === "tables" ? d.tables : d.reports;
          setDays(d);
          setDate((old) => (old && list.includes(old) ? old : (list[0] ?? "")));
          setError(null);
        })
        .catch((e) => setError(errorText(e))),
    [kind],
  );
  useEffect(() => {
    loadDays();
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
          ? api.tableArchive(date, query).then((page) => {
              setTables((old) => (offset ? [...old, ...page.entries] : page.entries));
              done(page);
            })
          : api.reportArchive(date, query).then((page) => {
              setReports((old) => (offset ? [...old, ...page.entries] : page.entries));
              done(page);
            });
      request.catch((e) => setError(errorText(e)));
    },
    [kind, date, filter, newest],
  );
  useEffect(() => load(0), [load]);

  const search = (e: FormEvent) => {
    e.preventDefault();
    setFilter({ train: train.trim(), text: text.trim() });
  };

  const list = days ? (kind === "tables" ? days.tables : days.reports) : [];
  const shown = kind === "tables" ? tables.length : reports.length;
  return (
    <>
      <p className="hint">{t(kind === "tables" ? "archive.hint.tables" : "archive.hint.reports")}</p>
      {error && <p className="error">{error}</p>}
      {days && list.length === 0 ? (
        <p className="empty">
          {t("archive.noDays")} {can(PERMISSIONS.settings) && <a href="#/settings">{t("nav.settings")}</a>}
        </p>
      ) : (
        days && (
          <>
            <form className="toolbar" onSubmit={search}>
              <label>
                {t("archive.day")}
                <select value={date} onChange={(e) => setDate(e.target.value)}>
                  {list.map((d) => (
                    <option key={d} value={d}>
                      {dayText(d)}
                    </option>
                  ))}
                </select>
              </label>
              <input className="archive-train" placeholder={t("archive.filter.train")} value={train} onChange={(e) => setTrain(e.target.value)} />
              <input className="archive-search" placeholder={t("archive.filter.text")} value={text} onChange={(e) => setText(e.target.value)} />
              <button type="submit" className="secondary">
                {t("archive.filter.apply")}
              </button>
              <label className="check">
                <input type="checkbox" checked={newest} onChange={(e) => setNewest(e.target.checked)} />
                {t("archive.newest")}
              </label>
              <span className="spacer" />
              <button type="button" className="secondary" onClick={() => loadDays().then(() => load(0))}>
                {t("archive.refresh")}
              </button>
            </form>
            <p className="muted small">
              {t("archive.count", { shown, total })}
              {skipped > 0 && ` ${t("archive.skipped", { count: skipped })}`}
            </p>
            {shown === 0 ? (
              <p className="empty">{t("archive.empty")}</p>
            ) : kind === "tables" ? (
              <TablesList entries={tables} />
            ) : (
              <ReportsList entries={reports} />
            )}
            {shown < total && (
              <button className="secondary archive-more" onClick={() => load(shown)}>
                {t("archive.more")}
              </button>
            )}
          </>
        )
      )}
    </>
  );
}
