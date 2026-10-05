// Virtualne tabule: definicie (ako vo Web.config INISSView) a XML tabul najdene v priecinkoch logov INISSu.
import { useEffect, useRef, useState } from "react";
import { api, ApiError, PERMISSIONS, type InissViewImported, type VirtualBoard, type VirtualOverview, type VirtualSource } from "../api";
import { t, type TextKey } from "../i18n";
import { onVirtualChanged, reload, useCan, useLive } from "../store";
import { Dialog, Field } from "./Dialog";

/** Cas z XML INISSu (miestny, bez pasma) v tvare D. M. HH:MM:SS. */
function dateTime(value: string | null): string {
  if (!value) return "";
  const [date, time] = value.split("T");
  const [, month, day] = date.split("-");
  return `${Number(day)}. ${Number(month)}. ${time?.slice(0, 8) ?? ""}`;
}

function sourceStatus(source: VirtualSource | undefined): [TextKey, string] {
  if (!source) return ["virtual.status.missing", "status-error"];
  if (source.error) return ["virtual.status.error", "status-error"];
  if (source.stale) return ["virtual.status.stale", "status-listening"];
  return ["virtual.status.ok", "status-connected"];
}

function rowsText(board: VirtualBoard): string {
  if (board.rowCount === null) return board.rowStart > 1 ? t("virtual.rows.allFrom", { start: board.rowStart }) : t("virtual.rows.all");
  return t("virtual.rows.from", { count: board.rowCount, start: board.rowStart });
}

function VirtualDialog({ initial, isNew, sources, onClose }: { initial: VirtualBoard; isNew: boolean; sources: VirtualSource[]; onClose: () => void }) {
  const [board, setBoard] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const number = (value: string) => Number.parseInt(value, 10);

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      await api.putVirtual(board);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog title={isNew ? t("dialog.virtual.new") : t("dialog.virtual.edit", { id: board.id })} onClose={onClose} onSubmit={save} error={error} busy={busy}>
      <div className="form-grid">
        <Field label={t("virtual.field.id")} hint={t("virtual.field.id.hint")}>
          <input type="number" min={1} max={99999} value={board.id} disabled={!isNew} onChange={(e) => setBoard({ ...board, id: number(e.target.value) || 1 })} />
        </Field>
        <Field label={t("virtual.field.source")} hint={t("virtual.field.source.hint")}>
          <input list="virtual-sources" value={board.source} onChange={(e) => setBoard({ ...board, source: e.target.value })} />
          <datalist id="virtual-sources">
            {sources.map((s) => (
              <option key={s.name} value={s.name}>
                {[s.station, s.key].filter(Boolean).join(" – ")}
              </option>
            ))}
          </datalist>
        </Field>
        <Field label={t("virtual.field.name")} wide>
          <input value={board.name} onChange={(e) => setBoard({ ...board, name: e.target.value })} />
        </Field>
        <Field label={t("virtual.field.description")} wide>
          <input value={board.description} onChange={(e) => setBoard({ ...board, description: e.target.value })} />
        </Field>
        <Field label={t("virtual.field.rowCount")} hint={t("virtual.field.rowCount.hint")}>
          <input
            type="number"
            min={1}
            max={999}
            value={board.rowCount ?? ""}
            onChange={(e) => setBoard({ ...board, rowCount: e.target.value === "" ? null : number(e.target.value) || 1 })}
          />
        </Field>
        <Field label={t("virtual.field.rowStart")} hint={t("virtual.field.rowStart.hint")}>
          <input type="number" min={1} max={999} value={board.rowStart} onChange={(e) => setBoard({ ...board, rowStart: number(e.target.value) || 1 })} />
        </Field>
        <Field label={t("virtual.field.separator")} hint={t("virtual.field.separator.hint")} wide>
          <input value={board.separator} placeholder=", " onChange={(e) => setBoard({ ...board, separator: e.target.value })} />
        </Field>
        <Field label={t("virtual.field.template")} hint={t("virtual.field.template.hint")}>
          <input value={board.template} onChange={(e) => setBoard({ ...board, template: e.target.value })} />
        </Field>
        <Field label={t("virtual.field.parameter")} hint={t("virtual.field.parameter.hint")}>
          <input value={board.parameter} onChange={(e) => setBoard({ ...board, parameter: e.target.value })} />
        </Field>
      </div>
    </Dialog>
  );
}

/** Nacitanie definicii z Web.config INISSView a ponuka sledovat jeho priecinky. */
function ImportResult({ result, folders, onClose }: { result: InissViewImported; folders: string[]; onClose: () => void }) {
  const live = useLive();
  const can = useCan();
  const missing = result.folders.filter((f) => !folders.some((g) => g.toLowerCase() === f.toLowerCase()));
  const watchFolders = async () => {
    if (!live.settings) return;
    await api.putSettings({ ...live.settings, virtualFolders: [...folders, ...missing] });
    await reload();
    onClose();
  };
  return (
    <div className="banner-info">
      <p>{t("virtual.import.done", { added: result.added, replaced: result.replaced, skipped: result.skipped })}</p>
      {result.duplicates.length > 0 && <p>{t("virtual.import.duplicates", { list: result.duplicates.join(", ") })}</p>}
      {missing.length > 0 && (
        <p>
          {t("virtual.import.folders", { list: missing.join(", ") })}{" "}
          {can(PERMISSIONS.settings) && (
            <button className="secondary" onClick={() => watchFolders().catch(() => undefined)}>
              {t("virtual.import.useFolders")}
            </button>
          )}
        </p>
      )}
      <button className="secondary" onClick={onClose}>
        {t("common.close")}
      </button>
    </div>
  );
}

export function VirtualPage() {
  const can = useCan();
  const edit = can(PERMISSIONS.edit);
  const [data, setData] = useState<VirtualOverview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<{ board: VirtualBoard; isNew: boolean } | null>(null);
  const [replace, setReplace] = useState(false);
  const [imported, setImported] = useState<InissViewImported | null>(null);
  const upload = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const load = () =>
      api
        .virtual()
        .then((d) => {
          setData(d);
          setError(null);
        })
        .catch((e) => setError(e instanceof ApiError ? e.message : String(e)));
    load();
    return onVirtualChanged(() => load());
  }, []);

  const nextId = () => {
    const used = new Set(data?.boards.map((b) => b.id));
    let id = 1;
    while (used.has(id)) id++;
    return id;
  };
  const add = (source?: VirtualSource) =>
    setEditing({
      isNew: true,
      board: { id: nextId(), name: source?.key ?? "", description: "", source: source?.name ?? "", rowCount: null, rowStart: 1, separator: "", template: "", parameter: "" },
    });

  const onImport = async (files: FileList | null) => {
    const file = files?.[0];
    if (!file) return;
    try {
      setImported(await api.importInissView(await file.text(), replace));
      setError(null);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      if (upload.current) upload.current.value = "";
    }
  };

  if (!data) return error ? <p className="error">{error}</p> : null;
  const sourceOf = (board: VirtualBoard) => data.sources.find((s) => s.name.toLowerCase() === board.source.toLowerCase());
  const folders = data.folders.map((f) => f.path);

  return (
    <section>
      <div className="toolbar">
        <h2>{t("virtual.title")}</h2>
        <span className="spacer" />
        {edit && (
          <>
            <label className="check">
              <input type="checkbox" checked={replace} onChange={(e) => setReplace(e.target.checked)} />
              {t("virtual.import.replace")}
            </label>
            <input ref={upload} type="file" accept=".config,.xml" hidden onChange={(e) => onImport(e.target.files)} />
            <button className="secondary" onClick={() => upload.current?.click()}>
              {t("virtual.import")}
            </button>
            <button onClick={() => add()}>{t("virtual.add")}</button>
          </>
        )}
      </div>
      <p className="hint">{t("virtual.hint")}</p>
      {error && <p className="error">{error}</p>}
      {imported && <ImportResult result={imported} folders={folders} onClose={() => setImported(null)} />}
      {data.folders.length === 0 ? (
        <p className="banner-warn">
          {t("virtual.noFolders")}{" "}
          {can(PERMISSIONS.settings) && <a href="#/settings">{t("nav.settings")}</a>}
        </p>
      ) : (
        <p className="muted small">
          {t("virtual.folders")}{" "}
          {data.folders.map((f, i) => (
            <span key={f.path}>
              {i > 0 && ", "}
              <code>{f.path}</code>
              {!f.exists && <span className="tag">{t("virtual.folder.missing")}</span>}
            </span>
          ))}
        </p>
      )}

      {data.boards.length === 0 ? (
        <p className="empty">{t("virtual.boards.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{t("virtual.col.id")}</th>
                <th>{t("virtual.col.name")}</th>
                <th>{t("virtual.col.source")}</th>
                <th>{t("virtual.col.rows")}</th>
                <th>{t("virtual.col.separator")}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.boards.map((b) => {
                const [status, css] = sourceStatus(sourceOf(b));
                return (
                  <tr key={b.id}>
                    <td>{b.id}</td>
                    <td>
                      {b.name}
                      {b.description && <div className="muted small">{b.description}</div>}
                    </td>
                    <td>
                      <code>{b.source}</code>
                      <div className={`status small ${css}`}>{t(status)}</div>
                    </td>
                    <td>{rowsText(b)}</td>
                    <td>{b.separator ? <code>{`'${b.separator}'`}</code> : <span className="muted small">{t("virtual.separator.none")}</span>}</td>
                    <td className="nowrap">
                      <a className="button secondary" href={`#/virtual/${b.id}`} target="_blank" rel="noreferrer">
                        {t("virtual.open")}
                      </a>
                      {edit && (
                        <>
                          <button className="icon-button" title={t("common.edit")} onClick={() => setEditing({ board: b, isNew: false })}>
                            ✎
                          </button>
                          <button
                            className="icon-button"
                            title={t("common.delete")}
                            onClick={() => confirm(t("virtual.delete.confirm", { id: b.id })) && api.deleteVirtual(b.id).catch((e) => setError(String(e)))}
                          >
                            ✕
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <h3>{t("virtual.sources.title")}</h3>
      {data.sources.length === 0 ? (
        <p className="empty">{t("virtual.sources.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{t("virtual.col.source")}</th>
                <th>{t("virtual.col.station")}</th>
                <th>{t("virtual.col.key")}</th>
                <th>{t("virtual.col.written")}</th>
                <th>{t("virtual.col.validTo")}</th>
                <th>{t("virtual.col.records")}</th>
                <th>{t("virtual.col.status")}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.sources.map((s) => {
                const [status, css] = sourceStatus(s);
                return (
                  <tr key={s.name}>
                    <td>
                      <code>{s.name}</code>
                      <div className="muted small">{s.path}</div>
                    </td>
                    <td>{s.station}</td>
                    <td>{s.key}</td>
                    <td className="nowrap">{dateTime(s.written)}</td>
                    <td className="nowrap">{dateTime(s.validTo)}</td>
                    <td>{s.rows}</td>
                    <td>
                      <span className={`status ${css}`}>{t(status)}</span>
                      {s.error && <div className="muted small">{s.error}</div>}
                    </td>
                    <td className="nowrap">
                      {edit && (
                        <button className="secondary" onClick={() => add(s)}>
                          {t("virtual.createFor")}
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      {editing && <VirtualDialog initial={editing.board} isNew={editing.isNew} sources={data.sources} onClose={() => setEditing(null)} />}
    </section>
  );
}
