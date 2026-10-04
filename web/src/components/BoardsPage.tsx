// Prehlad tabul: zive zobrazenie, stav komunikacie, poruchy.
import { useState } from "react";
import { api, boardConfig, type Board, type BoardConfig, type FaultKind } from "../api";
import { locale, t } from "../i18n";
import { useLive } from "../store";
import { BoardDialog } from "./BoardDialog";
import { BoardView } from "./BoardView";

const FAULTS: FaultKind[] = ["none", "noReply", "nak", "delay", "garbage"];

function newBoard(line: number, manufacturer: string): BoardConfig {
  return {
    line,
    address: 1,
    manufacturer,
    name: "",
    design: null,
    fault: { kind: "none", delayMs: 0 },
    profile: {
      width: 192,
      rows: 2,
      rowHeight: 10,
      fontFile: null,
      fontMap: null,
      alternationMs: 3000,
      blinkOnMs: 500,
      blinkOffMs: 500,
      scrollSpeed: 40,
      movingFont: 0x52,
      autoSize: false,
    },
  };
}

function BoardCard({ board, onEdit }: { board: Board; onEdit: () => void }) {
  const name = board.name || t("board.unnamed", { address: board.address });
  const last = board.stats.lastFrameAt ? new Date(board.stats.lastFrameAt).toLocaleTimeString(locale()) : null;
  const d = board.display;
  // hodiny tabule idu od nastavenia dalej
  const clock = d.clock && d.clockSetAt ? new Date(Date.parse(d.clock) + (Date.now() - Date.parse(d.clockSetAt))).toLocaleString(locale()) : null;

  const setFault = (kind: FaultKind) => api.setFault(board.id, { kind, delayMs: kind === "delay" ? Math.max(board.fault.delayMs, 500) : 0 });
  const remove = () => {
    if (confirm(t("board.delete.confirm", { name }))) api.deleteBoard(board.id);
  };

  return (
    <article className={board.fault.kind !== "none" ? "card card-fault" : "card"}>
      <header className="card-head">
        <div>
          <h3>{name}</h3>
          <div className="muted">
            {t("board.lineAddress", { line: board.line, address: board.address })} · {board.manufacturer}
            {board.autoCreated && <> · {t("board.auto")}</>}
          </div>
        </div>
        <div className="card-actions">
          <a className="icon-button" href={`#/view/${board.id}`} title={t("board.fullscreen")}>
            ⛶
          </a>
          <button className="icon-button" onClick={onEdit} title={t("common.edit")}>
            ✎
          </button>
          <button className="icon-button" onClick={() => api.clearBoard(board.id)} title={t("board.clear")}>
            ⌫
          </button>
          <button className="icon-button" onClick={remove} title={t("common.delete")}>
            ✕
          </button>
        </div>
      </header>
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
          <select value={board.fault.kind} onChange={(e) => setFault(e.target.value as FaultKind)}>
            {FAULTS.map((f) => (
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
              onChange={(e) => api.setFault(board.id, { kind: "delay", delayMs: Number.parseInt(e.target.value, 10) || 0 })}
            />
          )}
        </label>
      </footer>
      {!board.profile.fontFile && <p className="hint small">{t("board.approxFont")}</p>}
    </article>
  );
}

export function BoardsPage() {
  const live = useLive();
  const [filter, setFilter] = useState<number | null>(null);
  const [editing, setEditing] = useState<{ config: BoardConfig; isNew: boolean } | null>(null);

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
        <button disabled={!firstLine} onClick={() => firstLine && setEditing({ config: newBoard(firstLine.number, firstLine.manufacturer), isNew: true })}>
          {t("boards.add")}
        </button>
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
            <BoardCard key={b.id} board={b} onEdit={() => setEditing({ config: boardConfig(b), isNew: false })} />
          ))}
        </div>
      )}

      {editing && <BoardDialog initial={editing.config} isNew={editing.isNew} onClose={() => setEditing(null)} />}
    </section>
  );
}
