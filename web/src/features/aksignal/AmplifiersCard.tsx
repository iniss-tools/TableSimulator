// Spolocny prehlad zosilnovacov, ktore INISS zopina pred hlasenim: jednotky ELSVO (trieda 129) a spinanie DTR (INISS 3.39).
import type { BoardDto } from "@/api";
import { StatusBadge } from "@/components/common/status-badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { locale, t } from "@/i18n";

const time = (value: string | null | undefined) => (value ? new Date(value).toLocaleTimeString(locale()) : "–");

export function AmplifiersCard({ boards }: { boards: BoardDto[] }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("amplifiers.title")}</CardTitle>
        <CardDescription>{t("amplifiers.hint")}</CardDescription>
      </CardHeader>
      <CardContent>
        <ul className="flex flex-col gap-2 text-sm">
          {boards.map((board) => {
            const amplifier = board.display.amplifier;
            const on = (amplifier?.band ?? 0) > 0;
            const dtr = board.manufacturer.toUpperCase() === "DTR";
            return (
              <li key={board.id} className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-3">
                <span className="min-w-0 truncate">
                  {board.name || t("amplifier.unnamed", { address: board.address })}
                  <span className="text-xs text-muted-foreground">
                    {" "}
                    · {t(dtr ? "amplifiers.where.dtr" : "amplifiers.where.elsvo", { line: board.line, address: board.address })}
                    {" · "}
                    {on ? t("amplifiers.since", { time: time(amplifier?.onSince) }) : t("amplifiers.switches", { switches: amplifier?.switches ?? 0 })}
                  </span>
                </span>
                <StatusBadge tone={board.poweredOff ? "bad" : on ? "ok" : "off"}>
                  {board.poweredOff ? t("board.poweredOff") : on ? t("amplifiers.on") : t("amplifiers.off")}
                </StatusBadge>
              </li>
            );
          })}
        </ul>
      </CardContent>
    </Card>
  );
}
