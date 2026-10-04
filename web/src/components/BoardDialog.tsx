// Uprava tabule: identita, profil (hardver) a vzhlad.
import { useEffect, useState } from "react";
import { api, ApiError, type BoardConfig, type DesignInfo, type Profile } from "../api";
import { t } from "../i18n";
import { useLive } from "../store";
import { Dialog, Field } from "./Dialog";

interface Props {
  initial: BoardConfig;
  isNew: boolean;
  onClose: () => void;
}

export function BoardDialog({ initial, isNew, onClose }: Props) {
  const live = useLive();
  const [config, setConfig] = useState<BoardConfig>(initial);
  const [fonts, setFonts] = useState<string[]>([]);
  const [designs, setDesigns] = useState<DesignInfo[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.fonts().then(setFonts).catch(() => setFonts([]));
    api.designs().then(setDesigns).catch(() => setDesigns([]));
  }, []);

  const profile = (patch: Partial<Profile>) => setConfig({ ...config, profile: { ...config.profile, ...patch } });
  const num = (value: string) => Number.parseInt(value, 10) || 0;

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
          <select value={config.line} disabled={!isNew} onChange={(e) => setConfig({ ...config, line: num(e.target.value) })}>
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
        <Field label={t("field.name")} wide>
          <input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
        </Field>
        <Field label={t("field.manufacturer")}>
          <select value={config.manufacturer} onChange={(e) => setConfig({ ...config, manufacturer: e.target.value })}>
            {live.manufacturers.map((m) => (
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
            {fonts.map((f) => (
              <option key={f}>{f}</option>
            ))}
          </select>
        </Field>
        <label className="check field-wide">
          <input type="checkbox" checked={config.profile.autoSize} onChange={(e) => profile({ autoSize: e.target.checked })} />
          {t("field.autoSize")}
        </label>
      </div>

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
