// Oznamenia o vypadkoch liniek mimo prehliadaca: webhook (POST s JSON, cita ho aj Teams a Slack) a e-mail cez SMTP;
// skusobne oznamenie podla formulara este pred ulozenim.
import { useMutation } from "@tanstack/react-query";
import { Send } from "lucide-react";
import { errorText, testAlerts, type AlertChannelResultDto, type AlertSettingsDto } from "@/api";
import { OptionSelect } from "@/components/common/option-select";
import { CheckField, FormField } from "@/components/common/form-field";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";
import { alertsRequest } from "./alerts";

interface AlertsSectionProps {
  alerts: AlertSettingsDto;
  change: (patch: Partial<AlertSettingsDto>) => void;
  smtpPassword: string | null;
  setSmtpPassword: (password: string | null) => void;
}

function ChannelResult({ label, result }: { label: string; result: AlertChannelResultDto }) {
  if (!result.configured) return null;
  return (
    <div className="flex flex-wrap items-center gap-2 text-sm">
      <span>{label}</span>
      {result.error === null ? (
        <StatusBadge tone="ok">{t("settings.alerts.sent")}</StatusBadge>
      ) : (
        <>
          <StatusBadge tone="bad">{t("settings.alerts.failed")}</StatusBadge>
          <span className="text-destructive">{result.error}</span>
        </>
      )}
    </div>
  );
}

export function AlertsSection({ alerts, change, smtpPassword, setSmtpPassword }: AlertsSectionProps) {
  const test = useMutation({
    mutationFn: async () => (await testAlerts({ body: alertsRequest(alerts, smtpPassword), throwOnError: true })).data,
  });
  const email = alerts.emailTo.trim().length > 0;

  return (
    <>
      <CheckField
        label={t("settings.alerts.enabled")}
        hint={t("settings.alerts.enabled.hint")}
        checked={alerts.enabled}
        onCheckedChange={(enabled) => change({ enabled })}
      />
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label={t("settings.alerts.delay")} hint={t("settings.alerts.delay.hint")}>
          <Input
            type="number"
            min={0}
            max={3600}
            className="max-w-32"
            value={alerts.delaySeconds}
            onChange={(e) => change({ delaySeconds: Number.parseInt(e.target.value, 10) || 0 })}
          />
        </FormField>
        <FormField label={t("settings.alerts.language")}>
          <OptionSelect<string>
            value={alerts.language}
            onValueChange={(language) => change({ language })}
            options={[
              { value: "sk", label: t("lang.sk") },
              { value: "cs", label: t("lang.cs") },
            ]}
            className="max-w-48"
          />
        </FormField>
      </div>
      <CheckField label={t("settings.alerts.recovery")} checked={alerts.recovery} onCheckedChange={(recovery) => change({ recovery })} />
      <FormField label={t("settings.alerts.webhook")} hint={t("settings.alerts.webhook.hint")}>
        <Input type="url" className="font-mono" placeholder="https://" value={alerts.webhookUrl} onChange={(e) => change({ webhookUrl: e.target.value })} />
      </FormField>
      <FormField label={t("settings.alerts.emailTo")} hint={t("settings.alerts.emailTo.hint")}>
        <Input value={alerts.emailTo} onChange={(e) => change({ emailTo: e.target.value })} />
      </FormField>
      {email && (
        <>
          <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_8rem]">
            <FormField label={t("settings.alerts.smtpHost")}>
              <Input value={alerts.smtpHost} onChange={(e) => change({ smtpHost: e.target.value })} />
            </FormField>
            <FormField label={t("settings.alerts.smtpPort")}>
              <Input
                type="number"
                min={1}
                max={65535}
                value={alerts.smtpPort}
                onChange={(e) => change({ smtpPort: Number.parseInt(e.target.value, 10) || 0 })}
              />
            </FormField>
          </div>
          <CheckField
            label={t("settings.alerts.smtpSsl")}
            hint={t("settings.alerts.smtpSsl.hint")}
            checked={alerts.smtpSsl}
            onCheckedChange={(smtpSsl) => change({ smtpSsl })}
          />
          <FormField label={t("settings.alerts.emailFrom")}>
            <Input value={alerts.emailFrom} onChange={(e) => change({ emailFrom: e.target.value })} />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("settings.alerts.smtpUser")} hint={t("settings.alerts.smtpUser.hint")}>
              <Input autoComplete="off" value={alerts.smtpUser} onChange={(e) => change({ smtpUser: e.target.value })} />
            </FormField>
            <FormField
              label={t("settings.alerts.smtpPassword")}
              hint={
                alerts.smtpPasswordSet &&
                (smtpPassword === "" ? (
                  t("settings.httpsPassword.cleared")
                ) : (
                  <Button type="button" variant="link" size="sm" className="h-auto p-0" onClick={() => setSmtpPassword("")}>
                    {t("settings.httpsPassword.clear")}
                  </Button>
                ))
              }
            >
              <Input
                type="password"
                autoComplete="new-password"
                value={smtpPassword ?? ""}
                placeholder={alerts.smtpPasswordSet && smtpPassword === null ? t("settings.httpsPassword.kept") : ""}
                onChange={(e) => setSmtpPassword(e.target.value || null)}
              />
            </FormField>
          </div>
        </>
      )}
      <div className="flex flex-col items-start gap-2">
        <Button type="button" variant="outline" disabled={test.isPending || (!alerts.webhookUrl.trim() && !email)} onClick={() => test.mutate()}>
          {test.isPending ? <Spinner /> : <Send />}
          {t("settings.alerts.test")}
        </Button>
        {test.error && <p className="text-sm text-destructive">{errorText(test.error)}</p>}
        {test.data && (
          <>
            <ChannelResult label={t("settings.alerts.webhook")} result={test.data.webhook} />
            <ChannelResult label={t("settings.alerts.email")} result={test.data.email} />
          </>
        )}
      </div>
    </>
  );
}
