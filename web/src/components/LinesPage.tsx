// Komunikacne linky - zodpovedaju sekciam Driver v INISSe.
import { useState } from "react";
import { api, ApiError, lineConfig, type LineConfig, type LineMode } from "../api";
import { t, type TextKey } from "../i18n";
import { useLive } from "../store";
import { Dialog, Field } from "./Dialog";

function CopyButton({ text }: { text: string }) {
  const [done, setDone] = useState(false);
  return (
    <button
      className="icon-button"
      title={done ? t("common.copied") : t("common.copy")}
      onClick={() => {
        navigator.clipboard?.writeText(text).then(() => {
          setDone(true);
          setTimeout(() => setDone(false), 1500);
        });
      }}
    >
      {done ? "✓" : "⧉"}
    </button>
  );
}

function LineDialog({ initial, isNew, onClose }: { initial: LineConfig; isNew: boolean; onClose: () => void }) {
  const live = useLive();
  const [config, setConfig] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      await api.putLine(config);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog title={isNew ? t("dialog.line.new") : t("dialog.line.edit", { number: config.number })} onClose={onClose} onSubmit={save} error={error} busy={busy}>
      <div className="form-grid">
        <Field label={t("field.number")}>
          <input
            type="number"
            min={1}
            max={100}
            value={config.number}
            disabled={!isNew}
            onChange={(e) => {
              const number = Number.parseInt(e.target.value, 10) || 1;
              setConfig({ ...config, number, endpoint: `TCP://0.0.0.0:${47000 + number}` });
            }}
          />
        </Field>
        <Field label={t("field.tableClass")}>
          <select value={config.tableClass} onChange={(e) => setConfig({ ...config, tableClass: Number(e.target.value) })}>
            {live.supportedClasses.map((c) => (
              <option key={c} value={c}>
                {t(`line.class.${c}` as TextKey)}
              </option>
            ))}
          </select>
        </Field>
        <Field label={t("field.name")} wide>
          <input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
        </Field>
        <Field label={t("field.endpoint")} hint={t("field.endpoint.hint")} wide>
          <input value={config.endpoint} onChange={(e) => setConfig({ ...config, endpoint: e.target.value })} />
        </Field>
        <Field label={t("field.mode")} hint={t("line.mode.hint")} wide>
          <select value={config.mode} onChange={(e) => setConfig({ ...config, mode: e.target.value as LineMode })}>
            <option value="display">{t("line.mode.display")}</option>
            <option value="monitor">{t("line.mode.monitor")}</option>
          </select>
        </Field>
        <label className="check field-wide">
          <input type="checkbox" checked={config.autoLearn} onChange={(e) => setConfig({ ...config, autoLearn: e.target.checked })} />
          {t("field.autoLearn")}
        </label>
        <Field label={t("field.lineManufacturer")}>
          <select value={config.manufacturer} onChange={(e) => setConfig({ ...config, manufacturer: e.target.value })}>
            {live.manufacturers.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </Field>
        <Field label={t("field.mirror")} hint={t("field.mirror.hint")} wide>
          <input value={config.mirror ?? ""} placeholder="COM10:9600,n,8,1" onChange={(e) => setConfig({ ...config, mirror: e.target.value.trim() || null })} />
        </Field>
      </div>
    </Dialog>
  );
}

export function LinesPage() {
  const live = useLive();
  const [editing, setEditing] = useState<{ config: LineConfig; isNew: boolean } | null>(null);

  const add = () => {
    const used = new Set(live.lines.map((l) => l.number));
    let number = 1;
    while (used.has(number)) number++;
    setEditing({
      isNew: true,
      config: { number, tableClass: 4, endpoint: `TCP://0.0.0.0:${47000 + number}`, name: "", mode: "display", autoLearn: true, manufacturer: "ELEN", mirror: null },
    });
  };

  return (
    <section>
      <div className="toolbar">
        <h2>{t("lines.title")}</h2>
        <span className="spacer" />
        <button onClick={add}>{t("lines.add")}</button>
      </div>
      <p className="hint">{t("lines.hint")}</p>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{t("lines.col.number")}</th>
              <th>{t("lines.col.name")}</th>
              <th>{t("lines.col.class")}</th>
              <th>{t("lines.col.endpoint")}</th>
              <th>{t("lines.col.mode")}</th>
              <th>{t("lines.col.status")}</th>
              <th>{t("lines.col.tablePort")}</th>
              <th>{t("lines.col.traffic")}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {live.lines.map((l) => (
              <tr key={l.number}>
                <td>{l.number}</td>
                <td>{l.name}</td>
                <td>{t(`line.class.${l.tableClass}` as TextKey)}</td>
                <td>
                  <code>{l.endpoint}</code>
                  {l.mirror && (
                    <div className="muted small">
                      {t("lines.mirror", { port: l.mirror })}
                      {l.mirrorError && <span className="tag">{l.mirrorError}</span>}
                    </div>
                  )}
                </td>
                <td>{t(`line.mode.${l.mode}`)}</td>
                <td>
                  <span className={`status status-${l.status}`}>{t(`line.status.${l.status}`)}</span>
                  {l.statusDetail && <div className="muted small">{l.statusDetail}</div>}
                </td>
                <td className="nowrap">
                  {l.inissTablePort ? (
                    <>
                      <code>{l.inissTablePort}</code> <CopyButton text={l.inissTablePort} />
                    </>
                  ) : (
                    <span className="muted small">{t("lines.serialTablePort")}</span>
                  )}
                </td>
                <td className="muted small">{t("lines.traffic", { frames: l.stats.frames, bad: l.stats.badFrames, connections: l.stats.connections })}</td>
                <td className="nowrap">
                  <button className="icon-button" title={t("common.edit")} onClick={() => setEditing({ config: lineConfig(l), isNew: false })}>
                    ✎
                  </button>
                  <button
                    className="icon-button"
                    title={t("common.delete")}
                    onClick={() => confirm(t("line.delete.confirm", { number: l.number })) && api.deleteLine(l.number)}
                  >
                    ✕
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {editing && <LineDialog initial={editing.config} isNew={editing.isNew} onClose={() => setEditing(null)} />}
    </section>
  );
}
