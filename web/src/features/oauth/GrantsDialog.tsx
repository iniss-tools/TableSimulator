// Vlastne prepojene aplikacie z ponuky pouzivatela - aj pre pouzivatela bez spravy klucov.
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { t } from "@/i18n";
import { GrantsTable } from "./GrantsTable";

export function GrantsDialog({ showUser, onClose }: { showUser: boolean; onClose: () => void }) {
  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] grid-rows-[auto_minmax(0,1fr)] sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>{t("oauth.grants")}</DialogTitle>
          <DialogDescription>{t("oauth.grants.hint")}</DialogDescription>
        </DialogHeader>
        <div className="-mx-4 min-h-0 overflow-y-auto px-4">
          <GrantsTable showUser={showUser} />
        </div>
      </DialogContent>
    </Dialog>
  );
}
