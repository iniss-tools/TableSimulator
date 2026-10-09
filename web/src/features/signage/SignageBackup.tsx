// Zaloha informacnych panelov (ZIP so spravami, playlistami, skupinami, panelmi a nahratymi subormi) a jej obnova:
// nahradit vsetko (cisla panelov ostanu - monitory idu dalej) alebo pridat obsah s novymi cislami.
import { useRef } from "react";
import { useMutation } from "@tanstack/react-query";
import { ArchiveRestore, ChevronDown, Download } from "lucide-react";
import { toast } from "sonner";
import { errorText, getSignageOverviewQueryKey, importSignage, queryClient } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";

export function SignageBackup({ edit }: { edit: boolean }) {
  const input = useRef<HTMLInputElement>(null);
  const replace = useRef(false);
  const restore = useMutation({
    mutationFn: async (file: File) => (await importSignage({ body: file, query: { replace: replace.current }, throwOnError: true })).data,
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
      const { messages, playlists, groups, panels, media } = result;
      toast.success(t(result.replaced ? "signage.backup.replaced" : "signage.backup.added", { messages, playlists, groups, panels, media }));
    },
    onError: (e) => toast.error(errorText(e)),
  });

  const choose = (all: boolean) => {
    replace.current = all;
    input.current?.click();
  };
  const onFile = async (file: File | undefined) => {
    if (input.current) input.current.value = "";
    if (!file) return;
    if (
      replace.current &&
      !(await confirmAction({
        title: t("signage.backup.replaceTitle"),
        description: t("signage.backup.replaceConfirm"),
        confirmLabel: t("signage.backup.restore"),
      }))
    )
      return;
    restore.mutate(file);
  };

  return (
    <>
      <Button variant="outline" render={<a href="/api/signage/export" download title={t("signage.backup.export.hint")} />}>
        <Download />
        {t("signage.backup.export")}
      </Button>
      {edit && (
        <>
          <input ref={input} type="file" accept=".zip" hidden onChange={(e) => void onFile(e.target.files?.[0])} />
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="outline" disabled={restore.isPending} />}>
              {restore.isPending ? <Spinner /> : <ArchiveRestore />}
              {t("signage.backup.restore")}
              <ChevronDown />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => choose(false)}>{t("signage.backup.add")}</DropdownMenuItem>
              <DropdownMenuItem variant="destructive" onClick={() => choose(true)}>
                {t("signage.backup.replaceAll")}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </>
      )}
    </>
  );
}
