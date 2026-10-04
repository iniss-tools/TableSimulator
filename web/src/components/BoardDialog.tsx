// Uprava tabule: identita, profil (hardver vratane hlavicky) a vzhlad.
import { useEffect, useState } from "react";
import { api, ApiError, defaultProfile, type BoardConfig, type BoardHeader, type BoardTemplate, type DesignInfo, type HeaderText, type Profile } from "../api";
import { t } from "../i18n";
import { manufacturersFor, useLive } from "../store";
import { Dialog, Field } from "./Dialog";

interface Props {
  initial: BoardConfig;
  isNew: boolean;
  onClose: () => void;
}

const num = (value: string) => Number.parseInt(value, 10) || 0;

/** Editor hlavicky: vyska, texty s polohou a hodiny. */
function HeaderEditor({ header, width, onChange }: { header: BoardHeader | null; width: number; onChange: (h: BoardHeader | null) => void }) {
  if (!header) {
    return (
      <button type="button" className="secondary" onClick={() => onChange({ height: 20, texts: [], clock: null })}>
        {t("header.add")}
      </button>
    );
  }

  const setText = (i: number, patch: Partial<HeaderText>) => onChange({ ...header, texts: header.texts.map((x, j) => (j === i ? { ...x, ...patch } : x)) });

  return (
    <div className="header-editor">
      <div className="form-grid">
        <Field label={t("header.height")}>
          <input type="number" min={1} max={200} value={header.height} onChange={(e) => onChange({ ...header, height: num(e.target.value) })} />
        </Field>
        <label className="check">
          <input
            type="checkbox"
            checked={!!header.clock}
            onChange={(e) => onChange({ ...header, clock: e.target.checked ? { x: Math.max(0, width - 45), y: 4, height: 8 } : null })}
          />
          {t("header.clock")}
        </label>
        {header.clock && (
          <Field label={t("header.clockPos")}>
            <span className="inline-inputs">
              <input type="number" className="narrow" value={header.clock.x} onChange={(e) => onChange({ ...header, clock: { ...header.clock!, x: num(e.target.value) } })} />
              <input type="number" className="narrow" value={header.clock.y} onChange={(e) => onChange({ ...header, clock: { ...header.clock!, y: num(e.target.value) } })} />
              <input
                type="number"
                className="narrow"
                min={1}
                value={header.clock.height}
                onChange={(e) => onChange({ ...header, clock: { ...header.clock!, height: num(e.target.value) } })}
              />
            </span>
          </Field>
        )}
      </div>
      <table className="compact">
        <thead>
          <tr>
            <th>X</th>
            <th>Y</th>
            <th>{t("header.textHeight")}</th>
            <th>{t("header.text")}</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {header.texts.map((text, i) => (
            <tr key={i}>
              <td>
                <input type="number" className="narrow" value={text.x} onChange={(e) => setText(i, { x: num(e.target.value) })} />
              </td>
              <td>
                <input type="number" className="narrow" value={text.y} onChange={(e) => setText(i, { y: num(e.target.value) })} />
              </td>
              <td>
                <input type="number" className="narrow" min={1} value={text.height} onChange={(e) => setText(i, { height: num(e.target.value) })} />
              </td>
              <td className="grow">
                <input value={text.text} onChange={(e) => setText(i, { text: e.target.value })} />
              </td>
              <td>
                <button type="button" className="icon-button" title={t("common.delete")} onClick={() => onChange({ ...header, texts: header.texts.filter((_, j) => j !== i) })}>
                  ✕
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="toolbar">
        <button type="button" className="secondary" onClick={() => onChange({ ...header, texts: [...header.texts, { x: 0, y: 1, height: 8, text: "" }] })}>
          {t("header.addText")}
        </button>
        <span className="spacer" />
        <button type="button" className="secondary" onClick={() => onChange(null)}>
          {t("header.remove")}
        </button>
      </div>
    </div>
  );
}

export function BoardDialog({ initial, isNew, onClose }: Props) {
  const live = useLive();
  const [config, setConfig] = useState<BoardConfig>(initial);
  const [fonts, setFonts] = useState<string[]>([]);
  const [designs, setDesigns] = useState<DesignInfo[]>([]);
  const [templates, setTemplates] = useState<BoardTemplate[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.fonts().then(setFonts).catch(() => setFonts([]));
    api.designs().then(setDesigns).catch(() => setDesigns([]));
    api
      .monitor()
      .then((m) => setTemplates(m.panels))
      .catch(() => setTemplates([]));
  }, []);

  const profile = (patch: Partial<Profile>) => setConfig({ ...config, profile: { ...config.profile, ...patch } });
  const lineManufacturers = (line: number) => manufacturersFor(live, live.lines.find((l) => l.number === line)?.tableClass);
  const manufacturers = lineManufacturers(config.line);
  const shownManufacturers = config.manufacturer && !manufacturers.includes(config.manufacturer) ? [config.manufacturer, ...manufacturers] : manufacturers;

  const applyTemplate = (file: string) => {
    const template = templates.find((x) => x.file === file);
    if (template) setConfig({ ...config, profile: { ...template.profile, autoSize: false }, name: config.name || template.name });
  };

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      await api.putBoard(config);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const title = isNew ? t("dialog.board.new") : t("dialog.board.edit", { name: config.name || t("board.unnamed", { address: config.address }) });
  return (
    <Dialog title={title} onClose={onClose} onSubmit={save} error={error} busy={busy}>
      <div className="form-grid">
        <Field label={t("field.line")}>
          <select
            value={config.line}
            disabled={!isNew}
            onChange={(e) => {
              const line = num(e.target.value);
              const available = lineManufacturers(line);
              const manufacturer = available.includes(config.manufacturer) ? config.manufacturer : (available[0] ?? config.manufacturer);
              setConfig({ ...config, line, manufacturer, profile: manufacturer === config.manufacturer ? config.profile : defaultProfile(manufacturer) });
            }}
          >
            {live.lines.map((l) => (
              <option key={l.number} value={l.number}>
                {l.number}
                {l.name ? ` – ${l.name}` : ""}
              </option>
            ))}
          </select>
        </Field>
        <Field label={t("field.address")}>
          <input type="number" min={1} max={255} value={config.address} disabled={!isNew} onChange={(e) => setConfig({ ...config, address: num(e.target.value) })} />
        </Field>
        <Field label={t("field.secondAddress")} hint={t("field.secondAddress.hint")}>
          <input
            type="number"
            min={1}
            max={127}
            value={config.secondAddress ?? ""}
            onChange={(e) => setConfig({ ...config, secondAddress: e.target.value ? num(e.target.value) : null })}
          />
        </Field>
        <Field label={t("field.name")} wide>
          <input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
        </Field>
        <Field label={t("field.manufacturer")}>
          <select value={config.manufacturer} onChange={(e) => setConfig({ ...config, manufacturer: e.target.value })}>
            {shownManufacturers.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </Field>
        <Field label={t("field.design")}>
          <select value={config.design ?? ""} onChange={(e) => setConfig({ ...config, design: e.target.value || null })}>
            <option value="">{t("common.default")}</option>
            {designs.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </Field>
      </div>

      <h3>{t("section.profile")}</h3>
      <p className="hint">{t("section.profile.hint")}</p>
      {templates.length > 0 && (
        <Field label={t("field.template")} hint={t("field.template.hint")} wide>
          <select value="" onChange={(e) => applyTemplate(e.target.value)}>
            <option value="">{t("field.template.pick")}</option>
            {templates.map((x) => (
              <option key={x.file} value={x.file}>
                {x.name} ({x.profile.width}×{x.profile.rows * x.profile.rowHeight})
              </option>
            ))}
          </select>
        </Field>
      )}
      <div className="form-grid">
        <Field label={t("field.width")}>
          <input type="number" min={8} max={2048} value={config.profile.width} onChange={(e) => profile({ width: num(e.target.value) })} />
        </Field>
        <Field label={t("field.rows")}>
          <input type="number" min={1} max={64} value={config.profile.rows} onChange={(e) => profile({ rows: num(e.target.value) })} />
        </Field>
        <Field label={t("field.rowHeight")}>
          <input type="number" min={5} max={64} value={config.profile.rowHeight} onChange={(e) => profile({ rowHeight: num(e.target.value) })} />
        </Field>
        <Field label={t("field.fontFile")}>
          <select value={config.profile.fontFile ?? ""} onChange={(e) => profile({ fontFile: e.target.value || null })}>
            <option value="">{t("common.none")}</option>
            {config.profile.fontFile && !fonts.includes(config.profile.fontFile) && <option>{config.profile.fontFile}</option>}
            {fonts.map((f) => (
              <option key={f}>{f}</option>
            ))}
          </select>
        </Field>
        <label className="check">
          <input type="checkbox" checked={config.profile.monochrome} onChange={(e) => profile({ monochrome: e.target.checked })} />
          {t("field.monochrome")}
        </label>
        <label className="check">
          <input type="checkbox" checked={config.profile.swapRedGreen} onChange={(e) => profile({ swapRedGreen: e.target.checked })} />
          {t("field.swapRedGreen")}
        </label>
        <label className="check field-wide">
          <input type="checkbox" checked={config.profile.autoSize} onChange={(e) => profile({ autoSize: e.target.checked })} />
          {t("field.autoSize")}
        </label>
      </div>

      <h3>{t("section.header")}</h3>
      <p className="hint">{t("section.header.hint")}</p>
      <HeaderEditor header={config.profile.header} width={config.profile.width} onChange={(header) => profile({ header })} />

      <h3>{t("section.timing")}</h3>
      <div className="form-grid">
        <Field label={t("field.alternationMs")}>
          <input type="number" min={100} step={100} value={config.profile.alternationMs} onChange={(e) => profile({ alternationMs: num(e.target.value) })} />
        </Field>
        <Field label={t("field.scrollSpeed")}>
          <input type="number" min={1} max={500} value={config.profile.scrollSpeed} onChange={(e) => profile({ scrollSpeed: num(e.target.value) })} />
        </Field>
        <Field label={t("field.blinkOnMs")}>
          <input type="number" min={50} step={50} value={config.profile.blinkOnMs} onChange={(e) => profile({ blinkOnMs: num(e.target.value) })} />
        </Field>
        <Field label={t("field.blinkOffMs")}>
          <input type="number" min={50} step={50} value={config.profile.blinkOffMs} onChange={(e) => profile({ blinkOffMs: num(e.target.value) })} />
        </Field>
        <Field label={t("field.movingFont")}>
          <input
            value={`0x${config.profile.movingFont.toString(16).toUpperCase()}`}
            onChange={(e) => {
              const v = Number.parseInt(e.target.value.replace(/^0x/i, ""), 16);
              if (!Number.isNaN(v)) profile({ movingFont: v });
            }}
          />
        </Field>
      </div>
    </Dialog>
  );
}
