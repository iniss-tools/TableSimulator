// Potvrdenie akcie (namiesto window.confirm): `if (await confirmAction({...}))` - okno kresli ConfirmHost v koreni.
import { useSyncExternalStore, type ReactNode } from "react";
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

export interface ConfirmOptions {
  title: string;
  description?: ReactNode;
  /** Text potvrdzovacieho tlacidla (predvolene Odstranit pri destructive, inak Potvrdit). */
  confirmLabel?: string;
  /** Akcia nieco zmaze - tlacidlo je cervene. */
  destructive?: boolean;
}

interface Pending {
  options: ConfirmOptions;
  resolve: (confirmed: boolean) => void;
}

let pending: Pending | null = null;
const listeners = new Set<() => void>();

function setPending(value: Pending | null) {
  pending = value;
  listeners.forEach((l) => l());
}

/** Opyta sa obsluhy; true = potvrdila. */
export function confirmAction(options: ConfirmOptions): Promise<boolean> {
  pending?.resolve(false);
  return new Promise((resolve) => setPending({ options, resolve }));
}

export function ConfirmHost() {
  const current = useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => pending,
  );
  const close = (confirmed: boolean) => {
    current?.resolve(confirmed);
    setPending(null);
  };
  const options = current?.options;

  return (
    <AlertDialog open={current !== null} onOpenChange={(open) => !open && close(false)}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{options?.title}</AlertDialogTitle>
          {options?.description && <AlertDialogDescription>{options.description}</AlertDialogDescription>}
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
          <AlertDialogAction variant={options?.destructive ? "destructive" : "default"} onClick={() => close(true)}>
            {options?.confirmLabel ?? (options?.destructive ? t("common.delete") : t("common.confirm"))}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
