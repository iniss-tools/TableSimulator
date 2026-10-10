// Ako server bezi: sluzba Windows (restart z webu) alebo program v okne (prikaz na instalaciu sluzby), priecinky.
import { useQuery } from "@tanstack/react-query";
import { RotateCw } from "lucide-react";
import { getServerInfoOptions, PERMISSIONS, useCan } from "@/api";
import { CopyButton } from "@/components/common/copy-button";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";
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

export function ServerSection({ databasePath }: { databasePath: string }) {
  const info = useQuery(getServerInfoOptions()).data;
  const restart = useRestartServer();
  const canRestart = useCan()(PERMISSIONS.server);
  const install = info && `TableSimulator.exe --install-service --data "${info.dataFolder}"`;

  return (
    <div className="flex flex-col gap-4 text-sm">
      <dl className="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-4 gap-y-2">
        <dt className="text-muted-foreground">{t("server.mode")}</dt>
        <dd className="flex flex-wrap items-center gap-2">
          {info && <StatusBadge tone={info.service ? "ok" : "off"}>{t(info.service ? "server.mode.service" : "server.mode.window")}</StatusBadge>}
          {info?.version && <span className="text-muted-foreground">{t("server.version", { version: info.version })}</span>}
        </dd>
        {info && <PathRow label={t("server.dataFolder")} path={info.dataFolder} />}
        <PathRow label={t("server.database")} path={databasePath} />
      </dl>
      <p className="text-muted-foreground">{t("settings.database.hint")}</p>
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
