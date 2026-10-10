// Vyber databazy simulatora (SQLite alebo PostgreSQL) mimo samotnej databazy - vlastne ulozenie, prejavi sa po restarte
// servera; udaje sa do inej databazy prenasaju zalohou a obnovou.
import { useState, type KeyboardEvent } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { CircleAlert, PlugZap, RotateCw, Save, TriangleAlert, Undo2 } from "lucide-react";
import { toast } from "sonner";
import {
  errorText,
  getDatabaseSettingsOptions,
  getDatabaseSettingsQueryKey,
  getServerInfoOptions,
  getStateQueryKey,
  queryClient,
  testDatabase,
  updateDatabaseSettingsMutation,
  type DatabaseKind,
  type DatabaseSettingsDto,
  type DatabaseTestResultDto,
  type PostgresSettingsRequest,
  type UpdateDatabaseRequest,
} from "@/api";
import { CopyButton } from "@/components/common/copy-button";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { StatusBadge } from "@/components/common/status-badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { t, type TextKey } from "@/i18n";
import { useRestartServer } from "./use-restart-server";

const KIND_LABEL: Record<DatabaseKind, TextKey> = { sqlite: "database.sqlite", postgres: "database.postgres" };

// heslo null = nechat ulozene
function requestOf(settings: DatabaseSettingsDto): UpdateDatabaseRequest {
  const { passwordSet: _, ...postgres } = settings.postgres;
  return { provider: settings.provider, postgres: { ...postgres, password: null } };
}

/** Co skuska pripojenia zistila o databaze. */
function testText(result: DatabaseTestResultDto): { text: TextKey; warn: boolean } {
  if (result.exists && result.initialized) return { text: "database.test.initialized", warn: false };
  if (result.exists) return result.canCreate ? { text: "database.test.empty", warn: false } : { text: "database.test.noSchemaRight", warn: true };
  return result.canCreate ? { text: "database.test.missing", warn: false } : { text: "database.test.noCreateRight", warn: true };
}

function TestResult({ result }: { result: DatabaseTestResultDto }) {
  if (!result.ok)
    return (
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge tone="bad">{t("database.test.failed")}</StatusBadge>
        <span className="break-all text-destructive">{result.error}</span>
      </div>
    );
  const { text, warn } = testText(result);
  return (
    <div className="flex flex-col gap-1">
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge tone="ok">{t("database.test.ok")}</StatusBadge>
        {result.version && <span className="text-muted-foreground">{t("database.test.version", { version: result.version })}</span>}
      </div>
      <span className={warn ? "text-amber-700 dark:text-amber-400" : "text-muted-foreground"}>{t(text)}</span>
    </div>
  );
}

// Enter v poli by odoslal formular nastavenia servera - vyber databazy sa uklada vlastnym tlacidlom
function noSubmit(e: KeyboardEvent) {
  if (e.key === "Enter" && e.target instanceof HTMLInputElement) e.preventDefault();
}

export function DatabaseSection() {
  const current = useQuery(getDatabaseSettingsOptions()).data;
  const service = useQuery(getServerInfoOptions()).data?.service === true;
  const restart = useRestartServer();
  // rozpracovany vyber; null = ukazuje sa ulozeny
  const [draft, setDraft] = useState<UpdateDatabaseRequest | null>(null);
  const test = useMutation({
    mutationFn: async (body: PostgresSettingsRequest) => (await testDatabase({ body, throwOnError: true })).data,
  });
  const save = useMutation({
    ...updateDatabaseSettingsMutation(),
    onSuccess: (saved) => {
      queryClient.setQueryData(getDatabaseSettingsQueryKey(), saved);
      void queryClient.invalidateQueries({ queryKey: getStateQueryKey() });
      setDraft(null);
      test.reset();
      toast.success(t(saved.restartRequired ? "database.saved.restart" : "database.saved"));
    },
  });

  if (!current) return null;
  const form = draft ?? requestOf(current);
  const postgres = form.postgres;
  const dirty = draft !== null && JSON.stringify(draft) !== JSON.stringify(requestOf(current));
  const change = (patch: Partial<PostgresSettingsRequest>) => {
    setDraft({ ...form, postgres: { ...postgres, ...patch } });
    test.reset();
  };
  const discard = () => {
    setDraft(null);
    test.reset();
    save.reset();
  };

  return (
    <div className="flex flex-col gap-4 text-sm" onKeyDown={noSubmit}>
      <dl className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-4 gap-y-2">
        <dt className="text-muted-foreground">{t("database.active")}</dt>
        <dd className="flex min-w-0 flex-wrap items-center gap-2">
          <StatusBadge tone="ok">{t(KIND_LABEL[current.active])}</StatusBadge>
          <code className="min-w-0 flex-1 font-mono text-xs break-all">{current.activeDescription}</code>
          <CopyButton text={current.activeDescription} />
        </dd>
      </dl>

      {current.error && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription className="break-all">{t("database.configError", { error: current.error })}</AlertDescription>
        </Alert>
      )}
      {current.restartRequired && !dirty && (
        <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
          <TriangleAlert />
          <AlertTitle>{t("database.restart.title", { database: t(KIND_LABEL[current.provider]) })}</AlertTitle>
          <AlertDescription className="text-current">
            <p>{t(service ? "database.restart.service" : "database.restart.manual")}</p>
            {service && (
              <Button type="button" size="sm" variant="outline" className="mt-2" disabled={restart.pending} onClick={() => void restart.run()}>
                {restart.pending ? <Spinner /> : <RotateCw />}
                {t("server.restart")}
              </Button>
            )}
          </AlertDescription>
        </Alert>
      )}

      <FormField label={t("database.provider")} hint={t(form.provider === "postgres" ? "database.provider.postgres.hint" : "database.provider.sqlite.hint")}>
        <OptionSelect<DatabaseKind>
          value={form.provider}
          onValueChange={(provider) => setDraft({ ...form, provider })}
          options={[
            { value: "sqlite", label: t("database.sqlite.option") },
            { value: "postgres", label: t("database.postgres") },
          ]}
          className="max-w-80"
        />
      </FormField>

      {form.provider === "postgres" && (
        <>
          <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_8rem]">
            <FormField label={t("database.host")}>
              <Input autoComplete="off" placeholder="localhost" value={postgres.host} onChange={(e) => change({ host: e.target.value })} />
            </FormField>
            <FormField label={t("database.port")}>
              <Input type="number" min={1} max={65535} value={postgres.port} onChange={(e) => change({ port: Number.parseInt(e.target.value, 10) || 0 })} />
            </FormField>
          </div>
          <FormField label={t("database.name")}>
            <Input autoComplete="off" className="max-w-80" value={postgres.database} onChange={(e) => change({ database: e.target.value })} />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("database.username")} hint={t("database.username.hint")}>
              <Input autoComplete="off" value={postgres.username} onChange={(e) => change({ username: e.target.value })} />
            </FormField>
            <FormField
              label={t("database.password")}
              hint={
                current.postgres.passwordSet &&
                (postgres.password === "" ? (
                  t("settings.httpsPassword.cleared")
                ) : (
                  <Button type="button" variant="link" size="sm" className="h-auto p-0" onClick={() => change({ password: "" })}>
                    {t("settings.httpsPassword.clear")}
                  </Button>
                ))
              }
            >
              <Input
                type="password"
                autoComplete="new-password"
                value={postgres.password ?? ""}
                placeholder={current.postgres.passwordSet && postgres.password === null ? t("settings.httpsPassword.kept") : ""}
                onChange={(e) => change({ password: e.target.value || null })}
              />
            </FormField>
          </div>
          <FormField label={t("database.options")} hint={t("database.options.hint")}>
            <Input
              autoComplete="off"
              className="font-mono"
              placeholder="SSL Mode=Require"
              value={postgres.options}
              onChange={(e) => change({ options: e.target.value })}
            />
          </FormField>
          <p className="text-muted-foreground">{t("database.migration.hint")}</p>
          <div className="flex flex-col items-start gap-2">
            <Button type="button" variant="outline" disabled={test.isPending} onClick={() => test.mutate(postgres)}>
              {test.isPending ? <Spinner /> : <PlugZap />}
              {t("database.test")}
            </Button>
            {test.data && <TestResult result={test.data} />}
            {test.error && <span className="text-destructive">{errorText(test.error)}</span>}
          </div>
        </>
      )}

      {save.error && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription className="whitespace-pre-line">{errorText(save.error)}</AlertDescription>
        </Alert>
      )}
      <div className="flex flex-wrap items-center justify-end gap-2">
        <Button type="button" variant="outline" disabled={!dirty || save.isPending} onClick={discard}>
          <Undo2 />
          {t("database.discard")}
        </Button>
        <Button type="button" disabled={!dirty || save.isPending} onClick={() => save.mutate({ body: form })}>
          {save.isPending ? <Spinner /> : <Save />}
          {t("database.save")}
        </Button>
      </div>
    </div>
  );
}
