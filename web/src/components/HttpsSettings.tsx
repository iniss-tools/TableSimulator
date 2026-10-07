// HTTPS v nastaveniach: port, presmerovanie, vlastny certifikat a stav certifikatu na serveri.
import { useQuery } from "@tanstack/react-query";
import { errorText, getHttpsStatusOptions, type HttpsStatusDto as HttpsStatus, type SettingsDto as Settings } from "../api";
import { hasText, t } from "../i18n";
import { Field } from "./Dialog";
import { formatDateTime } from "./UsersPage";

interface HttpsSettingsProps {
  form: Settings;
  setForm: (form: Settings) => void;
  /** Nove heslo certifikatu; null = nechat ulozene, prazdne = zmazat. */
  password: string | null;
  setPassword: (password: string | null) => void;
}

/** Stav HTTPS na serveri - bezi s portom z nastavenia pri starte. */
function HttpsState({ status, form }: { status: HttpsStatus; form: Settings }) {
  const pending = (status.enabled ? status.port : 0) !== (form.httpsPort || 0);
  if (!status.enabled)
    return <p className={pending ? "banner-info" : "muted small"}>{t(pending ? "https.restartPending" : "https.off")}</p>;
  const url = `https://${location.hostname}:${status.port}/`;
  const errorKey = `error.${status.error}`;
  return (
    <div className="https-status">
      {pending && <p className="banner-info">{t("https.restartPending")}</p>}
      <p>
        {t("https.running")} <a href={url}>{url}</a>
        {status.redirect && <span className="muted small"> · {t("https.redirect")}</span>}
      </p>
      {status.error && <p className="banner-warn">{hasText(errorKey) ? t(errorKey) : status.errorDetail}</p>}
      <p className="muted small">
        {t(status.source === "generated" ? "https.generated" : "https.file")}{" "}
        <a href="/api/https/certificate" download>
          {t(status.source === "generated" ? "https.download" : "https.downloadFile")}
        </a>
      </p>
      <dl className="https-facts">
        <dt>{t("https.names")}</dt>
        <dd>{status.names.join(", ")}</dd>
        <dt>{t("https.validTo")}</dt>
        <dd>{formatDateTime(status.notAfter)}</dd>
        <dt>{t("https.thumbprint")}</dt>
        <dd>
          <code className="hex">{status.thumbprint}</code>
        </dd>
      </dl>
    </div>
  );
}

export function HttpsSettings({ form, setForm, password, setPassword }: HttpsSettingsProps) {
  // po ulozeni nastavenia ho SettingsPage zisti znova (iny certifikat alebo heslo)
  const { data: status, error } = useQuery(getHttpsStatusOptions());

  return (
    <fieldset className="settings-group">
      <legend>{t("settings.https")}</legend>
      <p className="muted small">{t("settings.https.hint")}</p>
      <Field label={t("settings.httpsPort")} hint={t("settings.httpsPort.hint")}>
        <input
          type="number"
          min={0}
          max={65535}
          value={form.httpsPort || ""}
          onChange={(e) => setForm({ ...form, httpsPort: Number.parseInt(e.target.value, 10) || 0 })}
        />
      </Field>
      <label className="check">
        <input
          type="checkbox"
          checked={form.httpsRedirect}
          disabled={!form.httpsPort}
          onChange={(e) => setForm({ ...form, httpsRedirect: e.target.checked })}
        />
        {t("settings.httpsRedirect")}
      </label>
      <Field label={t("settings.httpsCertificate")} hint={t("settings.httpsCertificate.hint")} wide>
        <input value={form.httpsCertificate} onChange={(e) => setForm({ ...form, httpsCertificate: e.target.value })} />
      </Field>
      {form.httpsCertificate.trim() && (
        <>
          <Field label={t("settings.httpsKey")} hint={t("settings.httpsKey.hint")} wide>
            <input value={form.httpsKey} onChange={(e) => setForm({ ...form, httpsKey: e.target.value })} />
          </Field>
          <Field label={t("settings.httpsPassword")} wide>
            <input
              type="password"
              autoComplete="new-password"
              value={password ?? ""}
              placeholder={form.httpsPasswordSet && password === null ? t("settings.httpsPassword.kept") : ""}
              onChange={(e) => setPassword(e.target.value || null)}
            />
          </Field>
          {form.httpsPasswordSet &&
            (password === "" ? (
              <p className="muted small">{t("settings.httpsPassword.cleared")}</p>
            ) : (
              <button type="button" className="secondary https-clear" onClick={() => setPassword("")}>
                {t("settings.httpsPassword.clear")}
              </button>
            ))}
        </>
      )}
      {error && <p className="error">{errorText(error)}</p>}
      {status && <HttpsState status={status} form={form} />}
    </fieldset>
  );
}
