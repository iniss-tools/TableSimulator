// Nastavenie servera: pisma ELEN, vlastne vzhlady, siet.
import { useEffect, useState } from "react";
import { api, ApiError, type DesignInfo, type Settings } from "../api";
import { t } from "../i18n";
import { clearFontCache } from "../render/fonts";
import { reload, useLive } from "../store";
import { Field } from "./Dialog";

export function SettingsPage() {
  const live = useLive();
  const [form, setForm] = useState<Settings | null>(live.settings);
  const [fonts, setFonts] = useState<string[]>([]);
  const [designs, setDesigns] = useState<DesignInfo[]>([]);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!form && live.settings) setForm(live.settings);
  }, [live.settings, form]);

  useEffect(() => {
    api.fonts().then(setFonts).catch(() => setFonts([]));
    api.designs().then(setDesigns).catch(() => setDesigns([]));
  }, [live.settings]);

  if (!form) return null;

  const save = async () => {
    try {
      const result = await api.putSettings(form);
      clearFontCache();
      setMessage(result.restart ? t("settings.restart") : t("settings.saved"));
      await reload();
    } catch (e) {
      setMessage(e instanceof ApiError ? e.message : String(e));
    }
  };

  return (
    <section className="settings">
      <h2>{t("settings.title")}</h2>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          save();
        }}
      >
        <Field label={t("settings.fontsFolder")} hint={t("settings.fontsFolder.hint")} wide>
          <input value={form.fontsFolder} onChange={(e) => setForm({ ...form, fontsFolder: e.target.value })} />
        </Field>
        <p className="muted small">{fonts.length ? t("settings.fontsFound", { list: fonts.join(", ") }) : t("settings.noFonts")}</p>

        <Field label={t("settings.monitorFolder")} hint={t("settings.monitorFolder.hint")} wide>
          <input value={form.monitorFolder} onChange={(e) => setForm({ ...form, monitorFolder: e.target.value })} />
        </Field>

        <Field
          label={t("settings.designsFolder")}
          hint={t("settings.designsFolder.hint", { folder: "%AppData%\\TableSimulator\\designs" })}
          wide
        >
          <input value={form.designsFolder} onChange={(e) => setForm({ ...form, designsFolder: e.target.value })} />
        </Field>
        <p className="muted small">{t("settings.designsFound", { list: designs.map((d) => `${d.name} (${d.id})`).join(", ") })}</p>

        <Field label={t("settings.connectHost")} hint={t("settings.connectHost.hint")} wide>
          <input value={form.connectHost} onChange={(e) => setForm({ ...form, connectHost: e.target.value })} />
        </Field>
        <Field label={t("settings.httpPort")}>
          <input type="number" min={1} max={65535} value={form.httpPort} onChange={(e) => setForm({ ...form, httpPort: Number.parseInt(e.target.value, 10) || 0 })} />
        </Field>
        <label className="check">
          <input type="checkbox" checked={form.allowNetwork} onChange={(e) => setForm({ ...form, allowNetwork: e.target.checked })} />
          {t("settings.allowNetwork")}
        </label>
        <p className="muted small">{t("settings.configPath", { path: form.configPath })}</p>
        <div className="dialog-buttons">
          {message && <span className="muted">{message}</span>}
          <button type="submit">{t("common.save")}</button>
        </div>
      </form>
    </section>
  );
}
