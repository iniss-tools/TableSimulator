// Ako server bezi: sluzba Windows (restart z webu) alebo program v okne (prikaz na instalaciu sluzby), priecinky,
// nova verzia z vydania.
import { useMutation, useQuery } from "@tanstack/react-query";
import { RefreshCw, RotateCw } from "lucide-react";
import { toast } from "sonner";
import {
  checkServerUpdateMutation,
  errorText,
  getServerInfoOptions,
  getServerUpdateOptions,
  getServerUpdateQueryKey,
  PERMISSIONS,
  queryClient,
  useCan,
  type ServerUpdateDto,
} from "@/api";
import { CopyButton } from "@/components/common/copy-button";
import { CheckField } from "@/components/common/form-field";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";
import { formatDateTime } from "@/lib/format";
import { useRestartServer } from "./use-restart-server";

function PathRow({ label, path }: { label: string; path: string }) {
  return (
    <>
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="flex min-w-0 items-center gap-1">
        <code className="min-w-0 flex-1 font-mono text-xs break-all">{path}</code>
        <CopyButton text={path} />
      </dd>
    </>
  );
}

/** Nova verzia: odkazy na instalator a co je nove, inak ci je verzia najnovsia a kedy sa to zistilo. */
function UpdateState({ update }: { update: ServerUpdateDto }) {
  if (!update.release) return <span className="text-muted-foreground">{t("server.update.notRelease")}</span>;
  return (
    <>
      {update.latest ? (
        <>
          <StatusBadge tone="wait">{t("server.update.newer", { version: update.latest })}</StatusBadge>
          {update.installer && (
            <a className="underline underline-offset-4" href={update.installer}>
              {t("update.download")}
            </a>
          )}
          {update.page && (
            <a className="underline underline-offset-4" href={update.page} target="_blank" rel="noreferrer">
              {t("update.notes")}
            </a>
          )}
        </>
      ) : (
        <span>{t(update.checkedAt ? "server.update.current" : "server.update.never")}</span>
      )}
      {update.checkedAt && <span className="text-muted-foreground">{t("server.update.checkedAt", { time: formatDateTime(update.checkedAt) })}</span>}
    </>
  );
}

export function ServerSection({
  databasePath,
  checkUpdates,
  onCheckUpdatesChange,
}: {
  databasePath: string;
  checkUpdates: boolean;
  onCheckUpdatesChange: (value: boolean) => void;
}) {
  const info = useQuery(getServerInfoOptions()).data;
  const update = useQuery(getServerUpdateOptions()).data;
  const restart = useRestartServer();
  const can = useCan();
  const canRestart = can(PERMISSIONS.server);
  const install = info && `TableSimulator.exe --install-service --data "${info.dataFolder}"`;
  const check = useMutation({
    ...checkServerUpdateMutation(),
    onSuccess: (result) => queryClient.setQueryData(getServerUpdateQueryKey(), result),
    onError: (e) => toast.error(errorText(e)),
  });

  return (
    <div className="flex flex-col gap-4 text-sm">
      <dl className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-4 gap-y-2">
        <dt className="text-muted-foreground">{t("server.mode")}</dt>
        <dd className="flex flex-wrap items-center gap-2">
          {info && <StatusBadge tone={info.service ? "ok" : "off"}>{t(info.service ? "server.mode.service" : "server.mode.window")}</StatusBadge>}
          {info?.version && <span className="text-muted-foreground">{t("server.version", { version: info.version })}</span>}
        </dd>
        {update && (
          <>
            <dt className="text-muted-foreground">{t("server.update")}</dt>
            <dd className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <UpdateState update={update} />
            </dd>
          </>
        )}
        {info && <PathRow label={t("server.dataFolder")} path={info.dataFolder} />}
        <PathRow label={t("server.database")} path={databasePath} />
      </dl>
      <p className="text-muted-foreground">{t("settings.database.hint")}</p>
      {update?.error && <p className="text-destructive">{t("server.update.error", { error: update.error })}</p>}
      {update?.release && (
        <div className="flex flex-wrap items-start gap-x-6 gap-y-3">
          {can(PERMISSIONS.settings) && (
            <CheckField
              className="w-auto flex-1"
              label={t("settings.checkUpdates")}
              hint={t("settings.checkUpdates.hint")}
              checked={checkUpdates}
              onCheckedChange={onCheckUpdatesChange}
            />
          )}
          <Button type="button" variant="outline" disabled={check.isPending} onClick={() => check.mutate({})}>
            {check.isPending ? <Spinner /> : <RefreshCw />}
            {t("server.update.check")}
          </Button>
        </div>
      )}
      {info?.service && canRestart && (
        <div className="flex flex-wrap items-center gap-3">
          <Button type="button" variant="outline" disabled={restart.pending} onClick={restart.run}>
            {restart.pending ? <Spinner /> : <RotateCw />}
            {t("server.restart")}
          </Button>
          <span className="text-muted-foreground">{t("server.restart.hint")}</span>
        </div>
      )}
      {install && !info.service && (
        <div className="flex flex-col gap-1">
          <span className="text-muted-foreground">{t("server.install.hint")}</span>
          <div className="flex items-center gap-1 rounded-lg border bg-muted/40 px-3 py-1.5">
            <code className="min-w-0 flex-1 font-mono text-xs break-all">{install}</code>
            <CopyButton text={install} />
          </div>
        </div>
      )}
    </div>
  );
}
