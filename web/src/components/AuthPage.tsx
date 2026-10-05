// Prihlasenie a zalozenie prveho spravcu (kym na serveri nie je ziaden pouzivatel).
import { useState } from "react";
import { api, ApiError } from "../api";
import { t } from "../i18n";
import { loadMe } from "../store";
import { Field } from "./Dialog";

export function AuthPage({ setup }: { setup: boolean }) {
  const [userName, setUserName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [remember, setRemember] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (setup && password !== again) {
      setError(t("auth.passwordMismatch"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      if (setup) await api.setup(userName.trim(), displayName.trim(), password);
      else await api.login(userName.trim(), password, remember);
      await loadMe();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth">
      <form
        className="auth-box"
        onSubmit={(e) => {
          e.preventDefault();
          submit();
        }}
      >
        <h1>
          <img src="/favicon.svg" alt="" width={26} height={26} /> {t("app.title")}
        </h1>
        <h2>{setup ? t("auth.title.setup") : t("auth.title.login")}</h2>
        {setup && <p className="hint">{t("auth.setup.text")}</p>}
        <Field label={t("auth.userName")} wide>
          <input autoFocus autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} />
        </Field>
        {setup && (
          <Field label={t("auth.displayName")} wide>
            <input autoComplete="name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          </Field>
        )}
        <Field label={t("auth.password")} wide>
          <input type="password" autoComplete={setup ? "new-password" : "current-password"} value={password} onChange={(e) => setPassword(e.target.value)} />
        </Field>
        {setup ? (
          <Field label={t("auth.passwordAgain")} wide>
            <input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
          </Field>
        ) : (
          <label className="check">
            <input type="checkbox" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
            {t("auth.remember")}
          </label>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy || !userName.trim() || !password}>
          {setup ? t("auth.create") : t("auth.login")}
        </button>
      </form>
    </div>
  );
}
