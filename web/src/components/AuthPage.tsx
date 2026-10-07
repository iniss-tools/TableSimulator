// Prihlasenie a zalozenie prveho spravcu (kym na serveri nie je ziaden pouzivatel).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { accessKey, errorText, forgetAccessKey, getMeQueryKey, loginMutation, queryClient, setupMutation, type MeDto } from "../api";
import { t } from "../i18n";
import { Field } from "./Dialog";

export function AuthPage({ setup }: { setup: boolean }) {
  const [userName, setUserName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [remember, setRemember] = useState(false);
  const [mismatch, setMismatch] = useState(false);
  // karta ma kluc zariadenia, ktory server neprijal (odstraneny alebo zle skopirovany)
  const [keyInvalid, setKeyInvalid] = useState(accessKey() !== null);

  // odpoved je prihlaseny pouzivatel - rozhranie sa hned prepne
  const signedIn = (me: MeDto) => queryClient.setQueryData(getMeQueryKey(), me);
  const login = useMutation({ ...loginMutation(), onSuccess: signedIn });
  const create = useMutation({ ...setupMutation(), onSuccess: signedIn });
  const busy = login.isPending || create.isPending;
  const failure = login.error ?? create.error;
  const error = mismatch ? t("auth.passwordMismatch") : failure ? errorText(failure) : null;

  const submit = () => {
    setMismatch(setup && password !== again);
    if (setup && password !== again) return;
    // prihlasenie heslom - neplatny kluc by ho prebil
    forgetAccessKey();
    setKeyInvalid(false);
    if (setup) create.mutate({ body: { userName: userName.trim(), displayName: displayName.trim(), password } });
    else login.mutate({ body: { userName: userName.trim(), password, remember } });
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
        {keyInvalid && <p className="error">{t("auth.keyInvalid")}</p>}
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
