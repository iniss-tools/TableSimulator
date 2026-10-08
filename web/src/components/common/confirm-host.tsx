// Okno potvrdenia pre confirmAction - jedno v koreni aplikacie.
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { t } from "@/i18n";
import { settleConfirm, usePendingConfirm } from "./confirm";

export function ConfirmHost() {
  const options = usePendingConfirm();
  return (
    <AlertDialog open={options !== null} onOpenChange={(open) => !open && settleConfirm(false)}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{options?.title}</AlertDialogTitle>
          {options?.description && <AlertDialogDescription>{options.description}</AlertDialogDescription>}
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
          <AlertDialogAction variant={options?.destructive ? "destructive" : "default"} onClick={() => settleConfirm(true)}>
            {options?.confirmLabel ?? (options?.destructive ? t("common.delete") : t("common.confirm"))}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
