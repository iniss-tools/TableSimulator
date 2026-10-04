// Ziva komunikacia na linkach - spravy INISSu a odpovede tabul, rozlozene aj v hex.
import { useEffect, useRef, useState } from "react";
import { api, type Frame } from "../api";
import { hasText, locale, t, useLanguage } from "../i18n";
import { openSocket } from "../store";

const MAX = 3000;
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

export function LogPage() {
  useLanguage();
  const [frames, setFrames] = useState<Frame[]>([]);
  const [paused, setPaused] = useState(false);
  const [hideReplies, setHideReplies] = useState(true);
  const [line, setLine] = useState("");
  const [address, setAddress] = useState("");
  const pausedRef = useRef(paused);
  pausedRef.current = paused;

  useEffect(() => {
    let lastSeq = 0;
    const add = (incoming: Frame[]) => {
      const fresh = incoming.filter((f) => f.seq > lastSeq);
      if (fresh.length === 0) return;
      lastSeq = fresh[fresh.length - 1].seq;
      if (!pausedRef.current) setFrames((old) => [...old, ...fresh].slice(-MAX));
    };
    api.log().then(add).catch(() => undefined);
    return openSocket(add, true);
  }, []);

  const shown = frames
    .filter((f) => !hideReplies || f.direction === "rx" || f.status !== "ok")
    .filter((f) => !line || f.line === Number(line))
    .filter((f) => !address || f.address === Number(address))
    .slice()
    .reverse();

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
        <button
          className="secondary"
          onClick={() => {
            api.clearLog();
            setFrames([]);
          }}
        >
          {t("log.clear")}
        </button>
      </div>
      {shown.length === 0 ? (
        <p className="empty">{t("log.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table className="log">
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
                <tr key={f.seq} className={`dir-${f.direction} st-${f.status}`}>
                  <td className="nowrap">{new Date(f.time).toLocaleTimeString(locale(), TIME)}</td>
                  <td>{f.line}</td>
                  <td className="nowrap">{t(`log.dir.${f.direction}`)}</td>
                  <td>{f.address ?? ""}</td>
                  <td>
                    {meaning(f)}
                    {f.status !== "ok" && <span className="tag">{t(`log.status.${f.status}`)}</span>}
                  </td>
                  <td>
                    <code className="hex">{hex(f.hex)}</code>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
