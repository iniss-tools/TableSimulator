// Potvrdenie akcie (namiesto window.confirm): `if (await confirmAction({...}))` - okno kresli ConfirmHost v koreni.
import { useSyncExternalStore, type ReactNode } from "react";

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

/** Otazka, na ktoru sa caka (pre ConfirmHost); null = ziadna. */
export function usePendingConfirm(): ConfirmOptions | null {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => pending?.options ?? null,
  );
}

/** Odpoved obsluhy na otazku, na ktoru sa caka. */
export function settleConfirm(confirmed: boolean) {
  pending?.resolve(confirmed);
  setPending(null);
}
