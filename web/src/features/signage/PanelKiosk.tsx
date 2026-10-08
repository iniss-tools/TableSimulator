// Informacny panel na celu obrazovku (#/panel/<cislo>): program skupiny podla hodin servera - vsetky panely skupiny
// hraju v tej istej chvili to iste. Program si panel vypyta pri zmene na serveri, pri zmene vyberu playlistu a aspon
// raz za minutu (tym sa zaroven ohlasi); bez servera hra posledny program dalej.
import { useEffect, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { errorText, getSignageProgramOptions, useConnection, type SignageOffline } from "@/api";
import { KioskControls } from "@/app/KioskControls";
import { toggleFullscreen, useKiosk } from "@/app/kiosk";
import { useNow } from "@/hooks/use-now";
import { t, useLanguage } from "@/i18n";
import { cn } from "@/lib/utils";
import { mediaUrl, slotAt } from "./signage";
import { SignageSlide } from "./SignageSlide";

/** Ako casto sa panel ohlasi a overi program. */
const REFRESH_MS = 60_000;

function Offline({ panel, reason }: { panel: string; reason: SignageOffline | null }) {
  return (
    <div className="flex size-full flex-col items-center justify-center gap-[1.5vw] text-center text-neutral-300">
      <div className="text-[5vw] font-semibold tracking-wide">{t("signage.offline")}</div>
      {reason && <div className="text-[1.8vw] text-neutral-500">{t(`signage.offline.${reason}`)}</div>}
      <div className="text-[1.4vw] text-neutral-600">{panel}</div>
    </div>
  );
}

export function PanelKiosk({ id }: { id: number }) {
  useLanguage();
  const { connected } = useConnection();
  const query = useQuery({ ...getSignageProgramOptions({ path: { id } }), refetchInterval: REFRESH_MS, refetchIntervalInBackground: true });
  const program = query.data;
  const { refetch } = query;
  const now = useNow(500);
  // hodiny servera minus hodiny prehliadaca v case nacitania programu
  const offset = useMemo(() => (program ? Date.parse(program.now) - query.dataUpdatedAt : 0), [program, query.dataUpdatedAt]);
  const active = useKiosk(`/panel/${id}`, program?.panel || t("kiosk.panelName", { id }));

  // vyber playlistu sa zmeni (zaciatok alebo koniec okna) - program znova
  useEffect(() => {
    if (!program) return;
    const wait = Date.parse(program.validUntil) - (Date.now() + offset);
    const timer = setTimeout(() => void refetch(), Math.min(REFRESH_MS, Math.max(1000, wait + 500)));
    return () => clearTimeout(timer);
  }, [program, offset, refetch]);

  const items = program?.items ?? [];
  const slot =
    program && items.length > 0
      ? slotAt(
          items.map((i) => i.duration),
          (now + offset - Date.parse(program.anchor)) / 1000,
        )
      : null;
  const current = slot ? items[slot.index] : null;
  const next = slot ? items[(slot.index + 1) % items.length] : null;

  // nasledujuci obrazok vopred - prepnutie bez prazdnej obrazovky
  const nextImage = next?.message.definition.kind === "image" ? next.message.definition.media : null;
  useEffect(() => {
    if (nextImage) new Image().src = mediaUrl(nextImage);
  }, [nextImage]);

  return (
    <div className={cn("h-dvh overflow-hidden bg-black", !active && "cursor-none")} onDoubleClick={toggleFullscreen}>
      {current && slot ? (
        <SignageSlide key={`${slot.index}-${current.message.id}`} message={current.message.definition} />
      ) : program ? (
        <Offline panel={program.panel} reason={program.offline} />
      ) : (
        query.error && <div className="bg-[#5a1a1a] px-3 py-1 text-sm text-[#ffd0d0]">{errorText(query.error)}</div>
      )}
      {!connected && <div className="fixed bottom-3 left-3 text-[13px] text-[#ffb000]">{t("server.offline")}</div>}
      <KioskControls visible={active} />
    </div>
  );
}
