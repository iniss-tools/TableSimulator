// Prehlad tabul: zive zobrazenie, stav komunikacie, poruchy.
import { useEffect, useState } from "react";
import { api, ApiError, boardConfig, defaultProfile, PERMISSIONS, type Board, type BoardConfig, type FaultKind, type MonitorInfo } from "../api";
import { hasText, locale, t } from "../i18n";
import { useCan, useLive } from "../store";
import { BoardDialog } from "./BoardDialog";
import { BoardView } from "./BoardView";
import { Dialog, Field } from "./Dialog";

const FAULTS: FaultKind[] = ["none", "noReply", "nak", "delay", "garbage", "status"];

/** Poruchy, ktore tabula danej rodiny vie predstierat - stav hlasia len tabule so stavovou odpovedou. */
const faultsFor = (board: Board) => FAULTS.filter((f) => f !== "status" || board.display.family === "chars");

function newBoard(line: number, manufacturer: string): BoardConfig {
  return {
    line,
    address: 1,
    manufacturer,
    name: "",
    design: null,
    fault: { kind: "none", delayMs: 0, status: 0 },
    secondAddress: null,
    profile: defaultProfile(manufacturer),
  };
}

/** INISS posiela tabuli spravy pre ineho vyrobcu - dovod a prepnutie na spravneho. */
function MismatchWarning({ board, edit }: { board: Board; edit: boolean }) {
  const mismatch = board.mismatch!;
  const key = `mismatch.${mismatch.evidence}`;
  const suggested = mismatch.suggested.join(", ");
  return (
    <div className="mismatch" role="alert">
      <div>
        <strong>{t("mismatch.title")}</strong> {hasText(key) ? t(key, { suggested }) : ""}
      </div>
      <div className="mismatch-actions">
        {edit && mismatch.suggested.map((m) => (
          <button key={m} className="secondary" onClick={() => api.putBoard({ ...boardConfig(board), manufacturer: m })}>
            {t("mismatch.switch", { manufacturer: m })}
          </button>
        ))}
      </div>
    </div>
  );
}

function BoardCard({ board, onEdit, edit }: { board: Board; onEdit: () => void; edit: boolean }) {
  const name = board.name || t("board.unnamed", { address: board.address });
  const last = board.stats.lastFrameAt ? new Date(board.stats.lastFrameAt).toLocaleTimeString(locale()) : null;
  const d = board.display;
  // hodiny tabule idu od nastavenia dalej
  const clock = d.clock && d.clockSetAt ? new Date(Date.parse(d.clock) + (Date.now() - Date.parse(d.clockSetAt))).toLocaleString(locale()) : null;

  const setFault = (kind: FaultKind) =>
    api.setFault(board.id, {
      kind,
      delayMs: kind === "delay" ? Math.max(board.fault.delayMs, 500) : 0,
      // predvolene "chybna sprava"
      status: kind === "status" ? board.fault.status || 0x01 : 0,
    });
  const remove = () => {
    if (confirm(t("board.delete.confirm", { name }))) api.deleteBoard(board.id);
  };

  return (
    <article className={board.fault.kind !== "none" ? "card card-fault" : "card"}>
      <header className="card-head">
        <div>
          <h3>{name}</h3>
          <div className="muted">
            {board.secondAddress
              ? t("board.lineAddress2", { line: board.line, address: board.address, second: board.secondAddress })
              : t("board.lineAddress", { line: board.line, address: board.address })}{" "}
            · {board.manufacturer}
            {board.autoCreated && <> · {t("board.auto")}</>}
          </div>
        </div>
        <div className="card-actions">
          <a className="icon-button" href={`#/view/${board.id}`} title={t("board.fullscreen")}>
            ⛶
          </a>
          {edit && (
            <>
          <button className="icon-button" onClick={onEdit} title={t("common.edit")}>
            ✎
          </button>
          <button className="icon-button" onClick={() => api.clearBoard(board.id)} title={t("board.clear")}>
            ⌫
          </button>
          <button className="icon-button" onClick={remove} title={t("common.delete")}>
            ✕
          </button>
            </>
          )}
        </div>
      </header>
      {board.mismatch && <MismatchWarning board={board} edit={edit} />}
      <BoardView board={board} />
      <footer className="card-foot">
        <span className="muted">
          {last ? t("board.lastFrame", { time: last }) : t("board.noFrames")} ·{" "}
          {t("board.stats", { frames: board.stats.frames, acks: board.stats.acks, naks: board.stats.naks })} ·{" "}
          {d.brightness > 0 ? t("board.brightness", { value: d.brightness }) : t("board.dark")}
          {clock && <> · {t("board.clock", { time: clock })}</>}
        </span>
        <label className="fault">
          {t("fault.label")}{" "}
          <select value={board.fault.kind} disabled={!edit} onChange={(e) => setFault(e.target.value as FaultKind)}>
            {faultsFor(board).map((f) => (
              <option key={f} value={f}>
                {t(`fault.${f}`)}
              </option>
            ))}
          </select>
          {board.fault.kind === "delay" && (
            <input
              type="number"
              className="narrow"
              min={0}
              step={100}
              value={board.fault.delayMs}
              title={t("fault.delayMs")}
              onChange={(e) => api.setFault(board.id, { kind: "delay", delayMs: Number.parseInt(e.target.value, 10) || 0, status: 0 })}
            />
          )}
          {board.fault.kind === "status" && (
            <input
              className="narrow"
              defaultValue={board.fault.status.toString(16).toUpperCase().padStart(2, "0")}
              title={t("fault.statusBits")}
              onBlur={(e) => {
                const status = Number.parseInt(e.target.value, 16);
                if (!Number.isNaN(status)) api.setFault(board.id, { kind: "status", delayMs: 0, status: status & 0xff });
              }}
            />
          )}
        </label>
      </footer>
      {!board.profile.fontFile && <p className="hint small">{t("board.approxFont")}</p>}
    </article>
  );
}

/** Import projektu ELEN Monitora: simulatory projektu sa stanu tabulami na zvolenej linke. */
function MonitorImportDialog({ monitor, onClose }: { monitor: MonitorInfo; onClose: () => void }) {
  const live = useLive();
  const [project, setProject] = useState(monitor.projects[0] ?? "");
  const [line, setLine] = useState(live.lines[0]?.number ?? 1);
  const [error, setError] = useState<string | null>(null);

  const save = async () => {
    try {
      await api.importMonitor(project, line);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    }
  };

  return (
    <Dialog title={t("monitor.import.title")} onClose={onClose} onSubmit={save} error={error}>
      <p className="hint">{t("monitor.import.hint")}</p>
      <div className="form-grid">
        <Field label={t("monitor.import.project")} wide>
          <select value={project} onChange={(e) => setProject(e.target.value)}>
            {monitor.projects.map((p) => (
              <option key={p}>{p}</option>
            ))}
          </select>
        </Field>
        <Field label={t("field.line")}>
          <select value={line} onChange={(e) => setLine(Number(e.target.value))}>
            {live.lines.map((l) => (
              <option key={l.number} value={l.number}>
                {l.number}
                {l.name ? ` – ${l.name}` : ""}
              </option>
            ))}
          </select>
        </Field>
      </div>
    </Dialog>
  );
}

export function BoardsPage() {
  const live = useLive();
  const edit = useCan()(PERMISSIONS.edit);
  const [filter, setFilter] = useState<number | null>(null);
  const [editing, setEditing] = useState<{ config: BoardConfig; isNew: boolean } | null>(null);
  const [monitor, setMonitor] = useState<MonitorInfo | null>(null);
  const [importing, setImporting] = useState(false);

  useEffect(() => {
    api
      .monitor()
      .then(setMonitor)
      .catch(() => setMonitor(null));
  }, [live.settings]);

  const boards = live.boards.filter((b) => filter === null || b.line === filter);
  const firstLine = live.lines[0];

  return (
    <section>
      <div className="toolbar">
        <select value={filter ?? ""} onChange={(e) => setFilter(e.target.value ? Number(e.target.value) : null)}>
          <option value="">{t("boards.filter.all")}</option>
          {live.lines.map((l) => (
            <option key={l.number} value={l.number}>
              {t("field.line")} {l.number}
              {l.name ? ` – ${l.name}` : ""}
            </option>
          ))}
        </select>
        <span className="spacer" />
        <a className="button secondary" href="#/wall">
          {t("nav.wall")}
        </a>
        {edit && monitor?.available && monitor.projects.length > 0 && (
          <button className="secondary" disabled={live.lines.length === 0} onClick={() => setImporting(true)}>
            {t("monitor.import")}
          </button>
        )}
        {edit && (
          <button disabled={!firstLine} onClick={() => firstLine && setEditing({ config: newBoard(firstLine.number, firstLine.manufacturer), isNew: true })}>
            {t("boards.add")}
          </button>
        )}
      </div>

      {boards.length === 0 ? (
        <div className="empty">
          <h2>{t("boards.empty.title")}</h2>
          <p>{t("boards.empty.text")}</p>
          {live.lines.map((l) => (
            <p key={l.number}>
              <code>
                TableClass={l.tableClass} · TablePort={l.inissTablePort}
              </code>
            </p>
          ))}
        </div>
      ) : (
        <div className="cards">
          {boards.map((b) => (
            <BoardCard key={b.id} board={b} edit={edit} onEdit={() => setEditing({ config: boardConfig(b), isNew: false })} />
          ))}
        </div>
      )}

      {editing && <BoardDialog initial={editing.config} isNew={editing.isNew} onClose={() => setEditing(null)} />}
      {importing && monitor && <MonitorImportDialog monitor={monitor} onClose={() => setImporting(false)} />}
    </section>
  );
}
