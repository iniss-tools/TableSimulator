// Virtualna tabula na celu obrazovku (#/virtual/<cislo>) - obsah XML tabule INISSu podla rozlozenia tabule alebo
// stranka zo sablony XSL INISSView.
import { useQuery } from "@tanstack/react-query";
import { errorText, getVirtualViewOptions, useConnection } from "@/api";
import { t, useLanguage } from "@/i18n";
import { useNow } from "@/hooks/use-now";
import { KioskControls } from "@/app/KioskControls";
import { toggleFullscreen, useKiosk } from "@/app/kiosk";
import { cn } from "@/lib/utils";
import { VirtualDisplay } from "./VirtualDisplay";

export function VirtualKiosk({ id }: { id: number }) {
  useLanguage();
  const { connected } = useConnection();
  // obsah sa nacita znova pri zmene jej XML alebo definicii (live.ts); pri chybe ostava posledny
  const query = useQuery(getVirtualViewOptions({ path: { id } }));
  const view = query.data;
  const error = query.error && errorText(query.error);
  const now = useNow(250);
  const active = useKiosk(`/virtual/${id}`, view?.board.name || t("kiosk.virtualName", { id }));

  return (
    <div className={cn("h-dvh bg-black", !active && "cursor-none")} onDoubleClick={toggleFullscreen}>
      {view ? <VirtualDisplay view={view} now={now} error={error} /> : error && <div className="bg-[#5a1a1a] px-3 py-1 text-sm text-[#ffd0d0]">{error}</div>}
      {!connected && <div className="fixed bottom-3 left-3 text-[13px] text-[#ffb000]">{t("server.offline")}</div>}
      <KioskControls visible={active} />
    </div>
  );
}
