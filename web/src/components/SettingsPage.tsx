// Nastavenie servera: pisma ELEN, vlastne vzhlady, siet.
import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  errorText,
  getDesignsOptions,
  getFontsOptions,
  getHttpsStatusQueryKey,
  getStateQueryKey,
  queryClient,
  updateSettingsMutation,
  useSimulatorState,
  type SettingsDto,
} from "../api";
import { t } from "../i18n";
import { clearFontCache } from "../render/fonts";
import { Field } from "./Dialog";
import { HttpsSettings } from "./HttpsSettings";

export function SettingsPage() {
  const live = useSimulatorState();
  const [form, setForm] = useState<SettingsDto | null>(live.settings);
  const fonts = useQuery(getFontsOptions()).data ?? [];
  const designs = useQuery(getDesignsOptions()).data ?? [];
  const [message, setMessage] = useState<string | null>(null);
  const update = useMutation(updateSettingsMutation());
  // text pola s priecinkami (riadok = priecinok) - prazdne riadky vynecha az server
  const [folders, setFolders] = useState((live.settings?.virtualFolders ?? []).join("\n"));
  // nove heslo certifikatu; null = nechat ulozene
  const [httpsPassword, setHttpsPassword] = useState<string | null>(null);

  useEffect(() => {
    if (!form && live.settings) {
      setForm(live.settings);
      setFolders(live.settings.virtualFolders.join("\n"));
    }
  }, [live.settings, form]);

  if (!form) return null;

  const save = () =>
    update.mutate(
      { body: { ...form, httpsPassword } },
      {
        onSuccess: (result) => {
          setHttpsPassword(null);
          setForm({ ...form, httpsPasswordSet: httpsPassword === null ? form.httpsPasswordSet : httpsPassword !== "" });
          clearFontCache();
          setMessage(result.restart ? t("settings.restart") : t("settings.saved"));
          queryClient.invalidateQueries({ queryKey: getStateQueryKey() });
          queryClient.invalidateQueries({ queryKey: getHttpsStatusQueryKey() });
        },
        onError: (e) => setMessage(errorText(e)),
      },
    );

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

        <Field label={t("settings.virtualFolders")} hint={t("settings.virtualFolders.hint")} wide>
          <textarea
            rows={3}
            value={folders}
            placeholder={"C:\\INISS\\Logy"}
            onChange={(e) => {
              setFolders(e.target.value);
              setForm({ ...form, virtualFolders: e.target.value.split(/\r?\n/) });
            }}
          />
        </Field>

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
        <label className="check">
          <input
            type="checkbox"
            checked={form.requireLogin || form.allowNetwork}
            disabled={form.allowNetwork}
            onChange={(e) => setForm({ ...form, requireLogin: e.target.checked })}
          />
          {t("settings.requireLogin")}
        </label>
        <p className="muted small">{t("settings.requireLogin.hint")}</p>
        <HttpsSettings form={form} setForm={setForm} password={httpsPassword} setPassword={setHttpsPassword} />
        <p className="muted small">{t("settings.databasePath", { path: form.databasePath })}</p>
        <div className="dialog-buttons">
          {message && <span className="muted">{message}</span>}
          <button type="submit">{t("common.save")}</button>
        </div>
      </form>
    </section>
  );
}
