// Prihlasenie a zalozenie prveho spravcu (kym na serveri nie je ziaden pouzivatel).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { CircleAlert } from "lucide-react";
import { accessKey, errorText, forgetAccessKey, getMeQueryKey, loginMutation, queryClient, setupMutation, type MeDto } from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";

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
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-sm">
        <form
          className="flex flex-col gap-4"
          onSubmit={(e) => {
            e.preventDefault();
            submit();
          }}
        >
          <CardHeader>
            <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
              <img src="/favicon.svg" alt="" className="size-6" />
              {t("app.title")}
            </div>
            <CardTitle className="text-lg">{setup ? t("auth.title.setup") : t("auth.title.login")}</CardTitle>
            {setup && <CardDescription>{t("auth.setup.text")}</CardDescription>}
          </CardHeader>
          <CardContent>
            <FieldGroup>
              {keyInvalid && (
                <Alert variant="destructive">
                  <CircleAlert />
                  <AlertDescription>{t("auth.keyInvalid")}</AlertDescription>
                </Alert>
              )}
              <FormField label={t("auth.userName")}>
                <Input autoFocus autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} />
              </FormField>
              {setup && (
                <FormField label={t("auth.displayName")}>
                  <Input autoComplete="name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
                </FormField>
              )}
              <FormField label={t("auth.password")}>
                <Input
                  type="password"
                  autoComplete={setup ? "new-password" : "current-password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </FormField>
              {setup ? (
                <FormField label={t("auth.passwordAgain")}>
                  <Input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
                </FormField>
              ) : (
                <CheckField label={t("auth.remember")} checked={remember} onCheckedChange={setRemember} />
              )}
              {error && (
                <Alert variant="destructive">
                  <CircleAlert />
                  <AlertDescription className="whitespace-pre-line">{error}</AlertDescription>
                </Alert>
              )}
            </FieldGroup>
          </CardContent>
          <CardFooter>
            <Button type="submit" className="w-full" disabled={busy || !userName.trim() || !password}>
              {busy && <Spinner />}
              {setup ? t("auth.create") : t("auth.login")}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
