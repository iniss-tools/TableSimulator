// Virtualna tabula na celu obrazovku (#/virtual/<cislo>) - obsah XML tabule INISSu ako v INISSView.
import { useEffect, useMemo, useRef, useState, type CSSProperties } from "react";
import { api, ApiError, type VirtualView } from "../api";
import { hasText, t, useLanguage } from "../i18n";
import { columnText, virtualColumns } from "../render/virtual";
import { onVirtualChanged, useLive } from "../store";

/** Aktualny cas, obnovovany po intervale (hodiny, striedanie a blikanie textov). */
function useNow(interval: number): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), interval);
    return () => clearInterval(timer);
  }, [interval]);
  return now;
}

const pad = (n: number) => String(n).padStart(2, "0");

function clock(now: number): string {
  const d = new Date(now);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

/** Cas z XML INISSu (miestny, bez pasma) v tvare HH:MM, z ineho dna aj s datumom. */
function time(value: string | null, now: number): string {
  if (!value) return "";
  const d = new Date(now);
  const today = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const [, month, day] = value.slice(0, 10).split("-");
  return value.startsWith(today) ? value.slice(11, 16) : `${Number(day)}. ${Number(month)}. ${value.slice(11, 16)}`;
}

function fullscreen() {
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.().catch(() => undefined);
}

export function VirtualKiosk({ id }: { id: number }) {
  useLanguage();
  const live = useLive();
  const [view, setView] = useState<VirtualView | null>(null);
  const [error, setError] = useState<string | null>(null);
  // subor tabule - zmena ineho XML ju nezaujima
  const source = useRef<string | null>(null);
  const now = useNow(250);

  useEffect(() => {
    const load = () =>
      api
        .virtualView(id)
        .then((v) => {
          source.current = v.board.source.toLowerCase();
          setView(v);
          setError(null);
        })
        .catch((e) => setError(e instanceof ApiError ? e.message : String(e)));
    load();
    return onVirtualChanged((sources, definitions) => {
      if (definitions || sources.some((s) => s.toLowerCase() === source.current)) load();
    });
  }, [id]);

  const columns = useMemo(() => virtualColumns(view?.rows ?? []), [view]);
  const rows = view?.rows ?? [];
  // pevny pocet zaznamov tabule urci velkost pisma; inak aspon 8 riadkov, aby malo zaznamov nebolo obrovskych
  const lines = view?.board.rowCount ?? Math.max(rows.length, 8);

  return (
    <div className={view?.source?.stale ? "vboard vboard-stale" : "vboard"} onDoubleClick={fullscreen} style={{ "--rows": lines } as CSSProperties}>
      <header className="vboard-head">
        <span className="vboard-title">{view?.board.name}</span>
        <span className="vboard-station">{view?.source?.station}</span>
        <span className="vboard-clock">{clock(now)}</span>
      </header>
      {error && <div className="vboard-note">{error}</div>}
      {view && !view.source && <div className="vboard-note">{t("virtual.view.missing", { name: view.board.source })}</div>}
      {view?.source?.stale && <div className="vboard-note">{t("virtual.view.stale", { time: time(view.source.validTo, now) })}</div>}
      {view?.source?.error && <div className="vboard-note">{t("virtual.view.error", { error: view.source.error })}</div>}
      {view?.source && rows.length === 0 && <div className="vboard-empty">{t("virtual.view.empty")}</div>}
      {rows.length > 0 && (
        <table className="vboard-table">
          <thead>
            <tr>
              {columns.map((c) => {
                const key = `virtual.cell.${c.key}`;
                return <th key={c.key}>{hasText(key) ? t(key) : c.key}</th>;
              })}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr key={i}>
                {columns.map((c) => (
                  <td key={c.key} className={`vc-${c.key}`}>
                    {columnText(row, c, now)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {!live.connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}
