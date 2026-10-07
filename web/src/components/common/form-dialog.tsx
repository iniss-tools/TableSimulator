// Okno s formularom: nadpis, polia, chyba zo servera a tlacidla Zrusit / Ulozit. Komponent sa ukazuje, kym je pripojeny.
import { useId, type ReactNode } from "react";
import { CircleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";

const SIZES = {
  default: "sm:max-w-lg",
  lg: "sm:max-w-2xl",
  xl: "sm:max-w-6xl",
} as const;

interface FormDialogProps {
  title: string;
  description?: ReactNode;
  children: ReactNode;
  onClose: () => void;
  onSubmit: () => void;
  /** Chyba zo servera (viac chyb pod sebou). */
  error?: string | null;
  busy?: boolean;
  /** Text potvrdzovacieho tlacidla (predvolene Ulozit). */
  submitLabel?: string;
  /** Bez tlacidla Zrusit - len oznamenie. */
  noCancel?: boolean;
  size?: keyof typeof SIZES;
}

export function FormDialog({ title, description, children, onClose, onSubmit, error, busy, submitLabel, noCancel, size = "default" }: FormDialogProps) {
  const formId = useId();
  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className={cn("max-h-[calc(100dvh-2rem)] grid-rows-[auto_minmax(0,1fr)_auto]", SIZES[size])}>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>
        <form
          id={formId}
          className="-mx-4 flex min-h-0 flex-col gap-4 overflow-y-auto px-4"
          onSubmit={(e) => {
            e.preventDefault();
            onSubmit();
          }}
        >
          {children}
          {error && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription className="whitespace-pre-line">{error}</AlertDescription>
            </Alert>
          )}
        </form>
        <DialogFooter>
          {!noCancel && <DialogClose render={<Button type="button" variant="outline" />}>{t("common.cancel")}</DialogClose>}
          <Button type="submit" form={formId} disabled={busy}>
            {busy && <Spinner />}
            {submitLabel ?? t("common.save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
