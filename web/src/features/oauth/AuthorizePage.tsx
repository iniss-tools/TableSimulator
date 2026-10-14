// Povolenie pristupu klientovi MCP (VS Code, Claude Code) cez OAuth: server presmeruje sem (#/authorize/<ziadost>),
// prihlaseny pouzivatel vyberie rolu a rozhodne; prehliadac sa potom vrati ku klientovi s kodom alebo s chybou.
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { CircleAlert, CircleCheck } from "lucide-react";
import { decideOAuthRequestMutation, errorText, getOAuthRequestOptions, useMe } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { roleLabel } from "@/features/roles/roles";
import { t } from "@/i18n";

/** Ziadost, ktoru server odmietol este pred strankou (neznamy klient, cudzia adresa navratu). */
const INVALID = "invalid";

export function AuthorizePage({ id }: { id: string }) {
  const me = useMe();
  const query = useQuery({ ...getOAuthRequestOptions({ path: { id } }), enabled: id !== INVALID, retry: false });
  const request = query.data;
  const [role, setRole] = useState<string | null>(null);
  const [answer, setAnswer] = useState<"allow" | "deny" | null>(null);
  const decide = useMutation({
    ...decideOAuthRequestMutation(),
    // spat ku klientovi - ten si kod prevezme a zobrazi, ze je prihlaseny
    onSuccess: (result) => window.location.assign(result.redirectUrl),
  });

  const send = (allow: boolean) => {
    setAnswer(allow ? "allow" : "deny");
    decide.mutate({ path: { id }, body: { allow, role: allow ? (role ?? request?.role ?? null) : null } });
  };

  // bez prihlasovania pristup povoluje tento pocitac (spravca), pouzivatela netreba menovat
  const text = (clientName: string) =>
    me?.loginRequired ? t("oauth.text", { client: clientName, user: me.displayName || me.userName || "" }) : t("oauth.text.local", { client: clientName });
  const failed = id === INVALID ? t("oauth.invalid") : query.error ? errorText(query.error) : null;

  return (
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
            <img src="/favicon.svg" alt="" className="size-6" />
            {t("app.title")}
          </div>
          <CardTitle className="text-lg">{t("oauth.title")}</CardTitle>
          {request && <CardDescription>{text(request.clientName)}</CardDescription>}
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {failed && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription>{failed}</AlertDescription>
            </Alert>
          )}
          {query.isPending && id !== INVALID && <Skeleton className="h-24 w-full" />}
          {decide.isSuccess && (
            <Alert>
              <CircleCheck />
              <AlertDescription>{t(answer === "allow" ? "oauth.allowed" : "oauth.denied")}</AlertDescription>
            </Alert>
          )}
          {request && !decide.isSuccess && (
            <>
              <FormField label={t("users.role")} hint={t("oauth.role.hint")}>
                <OptionSelect
                  value={role ?? request.role}
                  onValueChange={setRole}
                  options={request.roles.map((r) => ({ value: r.name, label: roleLabel(r.name, r.displayName) }))}
                />
              </FormField>
              <p className="text-sm text-muted-foreground">
                {t("oauth.redirect")} <code className="font-mono text-xs break-all">{request.redirectHost}</code>
              </p>
              {decide.error && (
                <Alert variant="destructive">
                  <CircleAlert />
                  <AlertDescription>{errorText(decide.error)}</AlertDescription>
                </Alert>
              )}
            </>
          )}
        </CardContent>
        {request && !decide.isSuccess && (
          <CardFooter className="justify-end gap-2">
            <Button variant="outline" disabled={decide.isPending} onClick={() => send(false)}>
              {t("oauth.deny")}
            </Button>
            <Button disabled={decide.isPending} onClick={() => send(true)}>
              {t("oauth.allow")}
            </Button>
          </CardFooter>
        )}
      </Card>
    </div>
  );
}
