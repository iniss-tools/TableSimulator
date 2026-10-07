// Zaznamy komunikacie: nahravanie prevadzky liniek a jej prehratie do simulatora.
import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  deleteRecording,
  getRecordingsOptions,
  getRecordingsQueryKey,
  PERMISSIONS,
  playRecording,
  queryClient,
  startRecording,
  stopRecording,
  stopReplay,
  uploadRecording,
  useCan,
} from "../api";
import { locale, t } from "../i18n";

/** Zaznamy, nahravanie a prehravanie - zmeny prinasa WebSocket. */
const useRecordings = () => useQuery(getRecordingsOptions()).data;
const refresh = () => queryClient.invalidateQueries({ queryKey: getRecordingsQueryKey() });

const SPEEDS = [1, 2, 5, 10, 0];

function duration(seconds: number): string {
  const s = Math.max(0, Math.round(seconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const pad = (n: number) => String(n).padStart(2, "0");
  return h > 0 ? `${h}:${pad(m)}:${pad(s % 60)}` : `${m}:${pad(s % 60)}`;
}

function size(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toLocaleString(locale(), { maximumFractionDigits: 1 })} kB`;
  return `${(bytes / 1024 / 1024).toLocaleString(locale(), { maximumFractionDigits: 1 })} MB`;
}

function Recorder() {
  const recorder = useRecordings()?.recorder;
  const can = useCan();
  const [note, setNote] = useState("");
  const [, tick] = useState(0);

  // dlzka zaznamu bezi aj medzi spravami servera
  useEffect(() => {
    if (!recorder?.recording) return;
    const timer = setInterval(() => tick((n) => n + 1), 1000);
    return () => clearInterval(timer);
  }, [recorder?.recording]);

  if (recorder?.recording) {
    const elapsed = recorder.started ? (Date.now() - Date.parse(recorder.started)) / 1000 : 0;
    return (
      <div className="toolbar recorder recording">
        <span className="rec-dot" />
        <span>{t("rec.running", { name: recorder.name ?? "", duration: duration(elapsed), entries: recorder.entries, size: size(recorder.bytes) })}</span>
        <span className="spacer" />
        {can(PERMISSIONS.recordings) && <button onClick={() => stopRecording()}>{t("rec.stop")}</button>}
      </div>
    );
  }

  if (!can(PERMISSIONS.recordings)) return null;

  return (
    <form
      className="toolbar recorder"
      onSubmit={(e) => {
        e.preventDefault();
        startRecording({ body: { note } }).then(() => setNote(""));
      }}
    >
      <input className="grow" placeholder={t("rec.note")} value={note} onChange={(e) => setNote(e.target.value)} />
      <button type="submit">
        <span className="rec-dot" /> {t("rec.start")}
      </button>
    </form>
  );
}

function Replay() {
  const replay = useRecordings()?.replay;
  const can = useCan();
  if (!replay) return null;
  const percent = replay.duration > 0 ? Math.min(100, (replay.position / replay.duration) * 100) : 100;
  const running = replay.state === "running";
  return (
    <div className="replay">
      <div className="toolbar">
        <strong>{t(`replay.${replay.state}`, { name: replay.name })}</strong>
        <span className="muted">
          {duration(replay.position)} / {duration(replay.duration)} · {replay.speed > 0 ? `${replay.speed}×` : t("rec.speed.max")}
          {replay.loop && <> · {t("replay.pass", { pass: replay.pass })}</>}
        </span>
        <span className="spacer" />
        {running && (
          <button className="secondary" hidden={!can(PERMISSIONS.recordings)} onClick={() => stopReplay()}>
            {t("replay.stop")}
          </button>
        )}
      </div>
      <div className="progress">
        <div style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}

export function RecordingsPage() {
  const recordings = useRecordings();
  const items = recordings?.items ?? [];
  const recorder = recordings?.recorder;
  const can = useCan();
  const [speed, setSpeed] = useState(1);
  const [loop, setLoop] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const upload = useRef<HTMLInputElement>(null);

  const onUpload = async (files: FileList | null) => {
    setError(null);
    for (const file of Array.from(files ?? [])) {
      try {
        await uploadRecording({ body: file, query: { name: file.name }, throwOnError: true });
      } catch {
        setError(t("rec.uploadBad"));
      }
    }
    if (upload.current) upload.current.value = "";
    refresh();
  };

  return (
    <section>
      <div className="toolbar">
        <h2>{t("rec.title")}</h2>
      </div>
      <p className="hint">{t("rec.hint")}</p>
      <Recorder />
      <Replay />

      <div className="toolbar">
        <label>
          {t("rec.speed")}{" "}
          <select value={speed} onChange={(e) => setSpeed(Number(e.target.value))}>
            {SPEEDS.map((s) => (
              <option key={s} value={s}>
                {s > 0 ? `${s}×` : t("rec.speed.max")}
              </option>
            ))}
          </select>
        </label>
        <label className="check">
          <input type="checkbox" checked={loop} onChange={(e) => setLoop(e.target.checked)} />
          {t("rec.loop")}
        </label>
        <span className="spacer" />
        <input ref={upload} type="file" accept=".tsrec" multiple hidden onChange={(e) => onUpload(e.target.files)} />
        <button className="secondary" hidden={!can(PERMISSIONS.recordings)} onClick={() => upload.current?.click()}>
          {t("rec.upload")}
        </button>
      </div>
      {error && <p className="error">{error}</p>}

      {items.length === 0 ? (
        <p className="empty">{t("rec.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{t("rec.col.name")}</th>
                <th>{t("rec.col.created")}</th>
                <th>{t("rec.col.duration")}</th>
                <th>{t("rec.col.lines")}</th>
                <th>{t("rec.col.frames")}</th>
                <th>{t("rec.col.size")}</th>
                <th>{t("rec.col.note")}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {items.map((r) => (
                <tr key={r.name}>
                  <td className="nowrap">{r.name}</td>
                  <td className="nowrap">{new Date(r.created).toLocaleString(locale())}</td>
                  <td>{duration(r.duration)}</td>
                  <td>{r.lines.join(", ")}</td>
                  <td>{r.frames}</td>
                  <td className="nowrap">{size(r.size)}</td>
                  <td>{r.note}</td>
                  <td className="nowrap">
                    <button hidden={!can(PERMISSIONS.recordings)} onClick={() => playRecording({ body: { name: r.name, speed, loop } })} disabled={recorder?.name === r.name}>
                      {t("rec.play")}
                    </button>{" "}
                    <a className="icon-button" href={`/api/recordings/${encodeURIComponent(r.name)}/file`} download title={t("rec.download")}>
                      ⤓
                    </a>
                    <button
                      className="icon-button"
                      title={t("common.delete")}
                      hidden={!can(PERMISSIONS.recordings)}
                      disabled={recorder?.name === r.name}
                      onClick={() => confirm(t("rec.delete.confirm", { name: r.name })) && deleteRecording({ path: { name: r.name } }).then(refresh)}
                    >
                      ✕
                    </button>
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
