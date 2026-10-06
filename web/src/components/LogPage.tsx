// Ziva komunikacia na linkach - spravy INISSu a odpovede tabul, rozlozene aj v hex.
import { memo, useEffect, useRef, useState } from "react";
import { api, PERMISSIONS, type Frame } from "../api";
import { hasText, locale, t, useLanguage, type Language } from "../i18n";
import { openSocket, useCan } from "../store";

/** Najviac drzanych sprav. */
const MAX = 3000;
/** Pocet zobrazenych riadkov naraz - dalsie az na poziadanie (velka tabulka zdrzuje prehliadac). */
const PAGE = 500;
/** Ako casto sa nove spravy pridaju do tabulky [ms] - pri opakovani spravy INISSom chodia v rychlom slede. */
const FLUSH_MS = 250;
const TIME: Intl.DateTimeFormatOptions = { hour: "2-digit", minute: "2-digit", second: "2-digit", fractionalSecondDigits: 3, hour12: false };

function meaning(frame: Frame): string {
  const args = frame.args ?? {};
  let key = `kind.${frame.kind}`;
  if (frame.kind === "elen.moving" && args.last === "1") key = "kind.elen.movingLast";
  if (frame.kind === "elen.row" && !args.text) key = "kind.elen.rowEmpty";
  let text = hasText(key) ? t(key, args) : frame.kind;
  if (args.broadcast === "1") text += ` (${t("log.broadcast")})`;
  // sprava tabule zabalena v pakete ITP
  if (args.seq && !frame.kind.startsWith("itp.")) text += ` · ${t("log.itpSeq", { seq: args.seq })}`;
  return text;
}

function hex(h: string): string {
  return h.replace(/(..)(?!$)/g, "$1 ");
}

/** Riadok tabulky - nakresli sa raz; znova len pri zmene jazyka. */
const LogRow = memo(function LogRow({ frame }: { frame: Frame; language: Language }) {
  const hintKey = `log.status.${frame.status}.hint`;
  return (
    <tr className={`dir-${frame.direction} st-${frame.status}`}>
      <td className="nowrap">{new Date(frame.time).toLocaleTimeString(locale(), TIME)}</td>
      <td>{frame.line}</td>
      <td className="nowrap">{t(`log.dir.${frame.direction}`)}</td>
      <td>{frame.address ?? ""}</td>
      <td>
        {meaning(frame)}
        {frame.status !== "ok" && (
          <span className="tag" title={hasText(hintKey) ? t(hintKey) : undefined}>
            {t(`log.status.${frame.status}`)}
          </span>
        )}
        {frame.status === "eotInData" && <div className="muted small">{t("log.status.eotInData.hint")}</div>}
      </td>
      <td>
        <code className="hex">{hex(frame.hex)}</code>
      </td>
    </tr>
  );
});

export function LogPage() {
  const language = useLanguage();
  const can = useCan();
  const [frames, setFrames] = useState<Frame[]>([]);
  const [paused, setPaused] = useState(false);
  const [hideReplies, setHideReplies] = useState(true);
  const [line, setLine] = useState("");
  const [address, setAddress] = useState("");
  const [limit, setLimit] = useState(PAGE);
  const pausedRef = useRef(paused);
  pausedRef.current = paused;

  useEffect(() => {
    let lastSeq = 0;
    let pending: Frame[] = [];
    // spravy sa zbieraju a do tabulky idu davkovo - nie jedno prekreslenie na kazdu spravu
    const flush = () => {
      if (pending.length === 0) return;
      const batch = pending;
      pending = [];
      setFrames((old) => (old.length + batch.length > MAX ? [...old, ...batch].slice(-MAX) : [...old, ...batch]));
    };
    const timer = setInterval(flush, FLUSH_MS);
    const add = (incoming: Frame[]) => {
      const fresh = incoming.filter((f) => f.seq > lastSeq);
      if (fresh.length === 0) return;
      lastSeq = fresh[fresh.length - 1].seq;
      if (!pausedRef.current) pending.push(...fresh);
    };
    api.log().then(add).catch(() => undefined);
    const close = openSocket(add, true);
    return () => {
      clearInterval(timer);
      close();
    };
  }, []);

  const matching: Frame[] = [];
  for (let i = frames.length - 1; i >= 0; i--) {
    const f = frames[i];
    if (hideReplies && f.direction !== "rx" && f.status === "ok") continue;
    if (line && f.line !== Number(line)) continue;
    if (address && f.address !== Number(address)) continue;
    matching.push(f);
  }
  const shown = matching.length > limit ? matching.slice(0, limit) : matching;

  return (
    <section>
      <div className="toolbar">
        <h2>{t("log.title")}</h2>
        <span className="spacer" />
        <label>
          {t("log.filter.line")} <input className="narrow" value={line} onChange={(e) => setLine(e.target.value.replace(/\D/g, ""))} />
        </label>
        <label>
          {t("log.filter.address")} <input className="narrow" value={address} onChange={(e) => setAddress(e.target.value.replace(/\D/g, ""))} />
        </label>
        <label className="check">
          <input type="checkbox" checked={hideReplies} onChange={(e) => setHideReplies(e.target.checked)} />
          {t("log.hideReplies")}
        </label>
        <button className="secondary" onClick={() => setPaused(!paused)}>
          {paused ? t("log.resume") : t("log.pause")}
        </button>
        {can(PERMISSIONS.edit) && (
          <button
            className="secondary"
            onClick={() => {
              api.clearLog();
              setFrames([]);
              setLimit(PAGE);
            }}
          >
            {t("log.clear")}
          </button>
        )}
      </div>
      {shown.length === 0 ? (
        <p className="empty">{t("log.empty")}</p>
      ) : (
        <>
          <div className="table-wrap">
            <table className="log">
              <colgroup>
                <col className="log-time" />
                <col className="log-line" />
                <col className="log-dir" />
                <col className="log-address" />
                <col />
                <col className="log-bytes" />
              </colgroup>
              <thead>
                <tr>
                  <th>{t("log.col.time")}</th>
                  <th>{t("log.col.line")}</th>
                  <th>{t("log.col.dir")}</th>
                  <th>{t("log.col.address")}</th>
                  <th>{t("log.col.meaning")}</th>
                  <th>{t("log.col.bytes")}</th>
                </tr>
              </thead>
              <tbody>
                {shown.map((f) => (
                  <LogRow key={f.seq} frame={f} language={language} />
                ))}
              </tbody>
            </table>
          </div>
          {matching.length > shown.length && (
            <p className="log-more">
              <span className="muted small">{t("log.shown", { shown: shown.length, total: matching.length })}</span>{" "}
              <button className="secondary" onClick={() => setLimit(limit + PAGE)}>
                {t("log.more")}
              </button>
            </p>
          )}
        </>
      )}
    </section>
  );
}
