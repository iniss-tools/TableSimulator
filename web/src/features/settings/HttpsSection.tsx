// HTTPS v nastaveniach: stav na serveri (certifikat, presmerovanie), port, presmerovanie a vlastny certifikat.
import { useQuery } from "@tanstack/react-query";
import { CircleAlert, Download, Info, TriangleAlert } from "lucide-react";
import { errorText, getHttpsStatusOptions, type HttpsStatusDto, type SettingsDto } from "@/api";
import { CopyButton } from "@/components/common/copy-button";
import { CheckField, FormField } from "@/components/common/form-field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { formatDateTime } from "@/lib/format";
import { hasText, t } from "@/i18n";
import { RestartBadge } from "./restart-badge";

interface HttpsSectionProps {
  form: SettingsDto;
  change: (patch: Partial<SettingsDto>) => void;
  /** Nove heslo certifikatu; null = nechat ulozene, prazdne = zmazat. */
  password: string | null;
  setPassword: (password: string | null) => void;
}

/** Stav HTTPS na serveri - bezi s portom z nastavenia pri starte. */
function HttpsState({ status, port }: { status: HttpsStatusDto; port: number }) {
  const pending = (status.enabled ? status.port : 0) !== port;
  const pendingAlert = pending && (
    <Alert>
      <Info />
      <AlertDescription>{t("https.restartPending")}</AlertDescription>
    </Alert>
  );
  if (!status.enabled) return pendingAlert || <p className="text-sm text-muted-foreground">{t("https.off")}</p>;

  const url = `https://${location.hostname}:${status.port}/`;
  const errorKey = `error.${status.error}`;
  const generated = status.source === "generated";
  return (
    <div className="flex flex-col gap-3">
      {pendingAlert}
      <div className="flex flex-col gap-3 rounded-lg border p-3 text-sm">
        <p>
          {t("https.running")}{" "}
          <a className="font-medium underline underline-offset-4" href={url}>
            {url}
          </a>
          {status.redirect && <span className="text-muted-foreground"> · {t("https.redirect")}</span>}
        </p>
        {status.error && (
          <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
            <TriangleAlert />
            <AlertDescription className="text-current">{hasText(errorKey) ? t(errorKey) : status.errorDetail}</AlertDescription>
          </Alert>
        )}
        <dl className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-4 gap-y-1">
          <dt className="text-muted-foreground">{t("https.names")}</dt>
          <dd className="break-words">{status.names.join(", ")}</dd>
          <dt className="text-muted-foreground">{t("https.validTo")}</dt>
          <dd>{formatDateTime(status.notAfter)}</dd>
          <dt className="text-muted-foreground">{t("https.thumbprint")}</dt>
          <dd className="flex items-center gap-1">
            <code className="min-w-0 font-mono text-xs break-all">{status.thumbprint}</code>
            <CopyButton text={status.thumbprint} />
          </dd>
        </dl>
        <p className="text-muted-foreground">{t(generated ? "https.generated" : "https.file")}</p>
        <Button variant="outline" size="sm" className="self-start" nativeButton={false} render={<a href="/api/https/certificate" download />}>
          <Download />
          {t(generated ? "https.download" : "https.downloadFile")}
        </Button>
      </div>
    </div>
  );
}

export function HttpsSection({ form, change, password, setPassword }: HttpsSectionProps) {
  // po ulozeni nastavenia ho stranka zisti znova (iny certifikat alebo heslo)
  const { data: status, error } = useQuery(getHttpsStatusOptions());

  return (
    <>
      {error && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorText(error)}</AlertDescription>
        </Alert>
      )}
      {status && <HttpsState status={status} port={form.httpsPort || 0} />}
      <FormField
        label={
          <>
            {t("settings.httpsPort")}
            <RestartBadge />
          </>
        }
        hint={t("settings.httpsPort.hint")}
      >
        <Input
          type="number"
          min={0}
          max={65535}
          className="max-w-32"
          value={form.httpsPort || ""}
          onChange={(e) => change({ httpsPort: Number.parseInt(e.target.value, 10) || 0 })}
        />
      </FormField>
      <CheckField
        label={
          <>
            {t("settings.httpsRedirect")}
            <RestartBadge />
          </>
        }
        checked={form.httpsRedirect}
        disabled={!form.httpsPort}
        onCheckedChange={(value) => change({ httpsRedirect: value })}
      />
      <FormField label={t("settings.httpsCertificate")} hint={t("settings.httpsCertificate.hint")}>
        <Input value={form.httpsCertificate} onChange={(e) => change({ httpsCertificate: e.target.value })} />
      </FormField>
      {form.httpsCertificate.trim() && (
        <>
          <FormField label={t("settings.httpsKey")} hint={t("settings.httpsKey.hint")}>
            <Input value={form.httpsKey} onChange={(e) => change({ httpsKey: e.target.value })} />
          </FormField>
          <FormField
            label={t("settings.httpsPassword")}
            hint={
              form.httpsPasswordSet &&
              (password === "" ? (
                t("settings.httpsPassword.cleared")
              ) : (
                <Button type="button" variant="link" size="sm" className="h-auto p-0" onClick={() => setPassword("")}>
                  {t("settings.httpsPassword.clear")}
                </Button>
              ))
            }
          >
            <Input
              type="password"
              autoComplete="new-password"
              value={password ?? ""}
              placeholder={form.httpsPasswordSet && password === null ? t("settings.httpsPassword.kept") : ""}
              onChange={(e) => setPassword(e.target.value || null)}
            />
          </FormField>
        </>
      )}
    </>
  );
}
