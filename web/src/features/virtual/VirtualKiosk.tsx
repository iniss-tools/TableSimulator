// Virtualna tabula na celu obrazovku (#/virtual/<cislo>) - obsah XML tabule INISSu podla rozlozenia tabule.
import { useQuery } from "@tanstack/react-query";
import { errorText, getVirtualViewOptions, useConnection } from "@/api";
import { t, useLanguage } from "@/i18n";
import { useNow, VirtualScreen } from "./VirtualScreen";

function fullscreen() {
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.().catch(() => undefined);
}

export function VirtualKiosk({ id }: { id: number }) {
  useLanguage();
  const { connected } = useConnection();
  // obsah sa nacita znova pri zmene jej XML alebo definicii (live.ts); pri chybe ostava posledny
  const query = useQuery(getVirtualViewOptions({ path: { id } }));
  const view = query.data;
  const error = query.error && errorText(query.error);
  const now = useNow(250);

  return (
    <div className="h-dvh bg-black" onDoubleClick={fullscreen}>
      {view ? <VirtualScreen view={view} layout={view.layout} now={now} error={error} /> : error && <div className="bg-[#5a1a1a] px-3 py-1 text-sm text-[#ffd0d0]">{error}</div>}
      {!connected && <div className="fixed bottom-3 left-3 text-[13px] text-[#ffb000]">{t("server.offline")}</div>}
    </div>
  );
}
