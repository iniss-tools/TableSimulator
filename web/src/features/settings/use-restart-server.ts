// Restart servera sluzby Windows - zo sekcie Server aj z oznamenia po ulozeni nastavenia.
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { errorText, restartServerMutation } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { t } from "@/i18n";

/** Restart servera sluzby Windows (po potvrdeni); stranka sa po nom pripoji sama. */
export function useRestartServer() {
  const restart = useMutation({
    ...restartServerMutation(),
    onSuccess: () => toast.info(t("server.restarting")),
    onError: (e) => toast.error(errorText(e)),
  });
  const run = async () => {
    if (await confirmAction({ title: t("server.restart.confirm"), description: t("server.restart.text"), confirmLabel: t("server.restart") }))
      restart.mutate({});
  };
  return { run, pending: restart.isPending };
}
