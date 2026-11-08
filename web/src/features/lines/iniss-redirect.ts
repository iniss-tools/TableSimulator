// Presmerovanie liniek servera INISS2 na simulator - spolocne pre tlacidlo na stranke Linky a okno Nacitat z INISSu.
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { errorText, getInissRedirectQueryKey, getInissStationQueryKey, redirectInissLinesMutation } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { t } from "@/i18n";

/** Prepnutie presmerovania s potvrdenim; po zmene obnovi nahlad stanice aj stav tlacidla. */
export function useInissRedirect() {
  const client = useQueryClient();
  const mutation = useMutation({
    ...redirectInissLinesMutation(),
    onSuccess: (result, variables) => {
      client.setQueryData(getInissStationQueryKey(), result);
      void client.invalidateQueries({ queryKey: getInissRedirectQueryKey() });
      toast.success(t(variables.body.redirect ? "iniss.redirect.done" : "iniss.redirect.undone"), { duration: 10_000 });
    },
    onError: (e) => toast.error(errorText(e)),
  });

  /** redirected = linky su teraz presmerovane (tlacidlo ich vrati). */
  async function toggle(redirected: boolean) {
    const confirmed = await confirmAction(
      redirected
        ? { title: t("iniss.redirect.undoConfirm"), description: t("iniss.redirect.undoDescription"), confirmLabel: t("iniss.redirect.undo") }
        : { title: t("iniss.redirect.confirm"), description: t("iniss.redirect.description"), confirmLabel: t("iniss.redirect") },
    );
    if (confirmed) mutation.mutate({ body: { redirect: !redirected } });
  }

  return { toggle, pending: mutation.isPending };
}
