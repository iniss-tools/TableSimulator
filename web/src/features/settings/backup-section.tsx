// Zaloha celeho simulatora (databaza, vlastne vzhlady, subory panelov, volitelne zaznamy) a obnova, ktora sa pouzije
// pri najblizsom starte servera - pri sluzbe Windows hned po restarte z webu.
import { useRef, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArchiveRestore, CircleAlert, CircleCheck, Download, RotateCw, TriangleAlert, X } from "lucide-react";
import { toast } from "sonner";
import { cancelRestore, errorText, getBackupStatusOptions, getBackupStatusQueryKey, queryClient, restoreBackup } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { CheckField } from "@/components/common/form-field";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { locale, t } from "@/i18n";
import { useRestartServer } from "./use-restart-server";

const refresh = () => queryClient.invalidateQueries({ queryKey: getBackupStatusQueryKey() });

export function BackupSection() {
  const status = useQuery(getBackupStatusOptions()).data;
  const [recordings, setRecordings] = useState(false);
  const input = useRef<HTMLInputElement>(null);
  const restart = useRestartServer();
  const restore = useMutation({
    mutationFn: async (file: File) => (await restoreBackup({ body: file, throwOnError: true })).data,
    onSuccess: () => {
      void refresh();
      toast.success(t("backup.prepared"));
    },
    onError: (e) => toast.error(errorText(e)),
  });
  const cancel = useMutation({
    mutationFn: () => cancelRestore({ throwOnError: true }),
    onSuccess: () => void refresh(),
    onError: (e) => toast.error(errorText(e)),
  });

  const onFile = async (file: File | undefined) => {
    if (input.current) input.current.value = "";
    if (!file) return;
    if (await confirmAction({ title: t("backup.restore.title"), description: t("backup.restore.confirm"), confirmLabel: t("backup.restore") }))
      restore.mutate(file);
  };

  const pending = status?.pending;
  const last = status?.lastRestore;
  return (
    <div className="flex flex-col gap-4 text-sm">
      <div className="flex flex-wrap items-center gap-3">
        <Button type="button" variant="outline" render={<a href={`/api/backup/file?recordings=${recordings}`} download />}>
          <Download />
          {t("backup.download")}
        </Button>
        <CheckField label={t("backup.recordings")} checked={recordings} onCheckedChange={setRecordings} className="w-auto" />
        <input ref={input} type="file" accept=".zip" hidden onChange={(e) => void onFile(e.target.files?.[0])} />
        <Button type="button" variant="outline" className="ml-auto" disabled={restore.isPending} onClick={() => input.current?.click()}>
          {restore.isPending ? <Spinner /> : <ArchiveRestore />}
          {t("backup.restore")}
        </Button>
      </div>
      {last && !pending && (
        <Alert variant={last.error ? "destructive" : "default"}>
          {last.error ? <CircleAlert /> : <CircleCheck />}
          <AlertDescription>
            {last.error
              ? t("backup.last.failed", { at: new Date(last.at).toLocaleString(locale()), error: last.error, folder: last.folder })
              : t("backup.last.done", {
                  at: new Date(last.at).toLocaleString(locale()),
                  created: new Date(last.backup.created).toLocaleString(locale()),
                  folder: last.folder,
                })}
          </AlertDescription>
        </Alert>
      )}
      {pending && (
        <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
          <TriangleAlert />
          <AlertTitle>{t("backup.pending.title")}</AlertTitle>
          <AlertDescription className="text-current">
            <p>
              {t("backup.pending.text", {
                created: new Date(pending.created).toLocaleString(locale()),
                version: pending.serverVersion,
                designs: pending.designs,
                media: pending.media,
              })}{" "}
              {pending.recordings !== null && t("backup.pending.recordings", { count: pending.recordings })}
            </p>
            <p className="mt-1">{t(status.service ? "backup.pending.service" : "backup.pending.manual")}</p>
            <div className="mt-2 flex flex-wrap gap-2">
              {status.service && (
                <Button type="button" size="sm" variant="outline" disabled={restart.pending} onClick={() => void restart.run()}>
                  {restart.pending ? <Spinner /> : <RotateCw />}
                  {t("server.restart")}
                </Button>
              )}
              <Button type="button" size="sm" variant="outline" disabled={cancel.isPending} onClick={() => cancel.mutate()}>
                <X />
                {t("backup.cancel")}
              </Button>
            </div>
          </AlertDescription>
        </Alert>
      )}
    </div>
  );
}
