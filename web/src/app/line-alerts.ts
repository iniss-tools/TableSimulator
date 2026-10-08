// Upozornenie na vypadok linky (INISS sa odpojil, linka nebezi): oznamenie v rozhrani trva, kym sa linka nevrati,
// navrat kratko potvrdi; ked okno nie je vidiet, aj systemove oznamenie (ak ho obsluha zapla).
import { useEffect, useRef } from "react";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { useSimulatorState, type LineStatus } from "@/api";
import { isOutage, lineLabel } from "@/features/lines/line-status";
import { t } from "@/i18n";
import { notify } from "./notifications";

const toastId = (line: number) => `line-${line}`;

/** Sleduje zmeny stavu liniek - len zmeny pocas otvorenej stranky, trvajuci vypadok ukazuje prehlad liniek a menu. */
export function useLineAlerts() {
  const { loaded, lines } = useSimulatorState();
  const navigate = useNavigate();
  const previous = useRef<Map<number, LineStatus> | null>(null);

  useEffect(() => {
    if (!loaded) return;
    const before = previous.current;
    const now = new Map(lines.map((l) => [l.number, l.status]));
    previous.current = now;
    if (!before) return;

    const showLines = { label: t("alert.showLines"), onClick: () => void navigate({ to: "/lines" }) };
    for (const line of lines) {
      const was = before.get(line.number);
      if (was === undefined || was === line.status) continue;
      const id = toastId(line.number);
      if (isOutage(line.status)) {
        const title = t(`alert.${line.status}`, { line: lineLabel(line) });
        const description = line.status === "error" ? (line.statusDetail ?? t("alert.error.hint")) : t("alert.lost.hint");
        (line.status === "error" ? toast.error : toast.warning)(title, { id, description, duration: Infinity, action: showLines });
        notify(title, description, id, "#/lines");
      } else if (isOutage(was) && line.status === "connected") {
        // rovnake id nahradi oznamenie o vypadku
        const title = t("alert.back", { line: lineLabel(line) });
        toast.success(title, { id, description: undefined, duration: 6000, action: undefined });
        notify(title, "", id, "#/lines");
      } else if (isOutage(was)) {
        // linka sa zastavila alebo caka na inom koncovom bode - stare oznamenie neplati
        toast.dismiss(id);
      }
    }
    for (const number of before.keys()) if (!now.has(number)) toast.dismiss(toastId(number));
  }, [loaded, lines, navigate]);
}
