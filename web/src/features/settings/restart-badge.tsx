// Znacka pri volbe, ktoru server pouzije az po restarte simulatora (porty, pristup, prihlasovanie, priecinok vzhladov).
import { RotateCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { t } from "@/i18n";

export function RestartBadge() {
  return (
    <Badge variant="outline" className="ml-1.5 align-middle font-normal text-muted-foreground" title={t("settings.afterRestart.hint")}>
      <RotateCw />
      {t("settings.afterRestart")}
    </Badge>
  );
}
