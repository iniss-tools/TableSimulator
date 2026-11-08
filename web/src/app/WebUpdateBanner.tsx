// Pruh s novym rozhranim na serveri (novsia zostava alebo aktualizacia) - obsluha stranku nacita znova, ked jej to
// vyhovuje (moze mat rozpracovane okno).
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import { useWebOutdated } from "./use-web-version";

export function WebUpdateBanner() {
  if (!useWebOutdated()) return null;

  return (
    <div
      role="status"
      className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b border-sky-500/30 bg-sky-500/10 px-4 py-2 text-sm text-sky-900 dark:text-sky-200"
    >
      <RefreshCw className="size-4 shrink-0" />
      <span>{t("web.outdated")}</span>
      <Button variant="outline" size="sm" className="ml-auto" onClick={() => location.reload()}>
        {t("web.reload")}
      </Button>
    </div>
  );
}
