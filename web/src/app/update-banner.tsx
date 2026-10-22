// Pruh s novou verziou simulatora pre toho, kto vidi nastavenie alebo spravu servera. Skryje sa do dalsej verzie
// (pamata si to tento prehliadac).
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { CircleArrowUp, X } from "lucide-react";
import { getServerUpdateOptions, PERMISSIONS, useCan } from "@/api";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";

const KEY = "ts.update.dismissed";
const HOUR = 60 * 60 * 1000;

function dismissedVersion(): string | null {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
}

export function UpdateBanner() {
  const allowed = useCan()([PERMISSIONS.settings, PERMISSIONS.server]);
  // server zistuje novu verziu dvakrat denne - stranke staci pozriet sa raz za hodinu
  const update = useQuery({ ...getServerUpdateOptions(), enabled: allowed, refetchInterval: HOUR }).data;
  const [dismissed, setDismissed] = useState(dismissedVersion);

  if (!allowed || !update?.latest || dismissed === update.latest) return null;
  const latest = update.latest;
  const dismiss = () => {
    try {
      localStorage.setItem(KEY, latest);
    } catch {
      // bez ulozenia sa pruh skryje len do obnovenia stranky
    }
    setDismissed(latest);
  };

  return (
    <div
      role="status"
      className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b border-sky-500/30 bg-sky-500/10 px-4 py-2 text-sm text-sky-900 dark:text-sky-200"
    >
      <CircleArrowUp className="size-4 shrink-0" />
      <span>{t("update.available", { version: latest, current: update.version })}</span>
      {update.installer && (
        <a className="font-medium underline underline-offset-4" href={update.installer}>
          {t("update.download")}
        </a>
      )}
      {update.page && (
        <a className="font-medium underline underline-offset-4" href={update.page} target="_blank" rel="noreferrer">
          {t("update.notes")}
        </a>
      )}
      <Button variant="ghost" size="icon-sm" className="ml-auto" onClick={dismiss} aria-label={t("update.dismiss")} title={t("update.dismiss")}>
        <X />
      </Button>
    </div>
  );
}
