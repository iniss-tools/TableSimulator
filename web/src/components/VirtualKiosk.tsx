// Virtualna tabula na celu obrazovku (#/virtual/<cislo>) - obsah XML tabule INISSu podla rozlozenia tabule.
import { useQuery } from "@tanstack/react-query";
import { errorText, getVirtualViewOptions, useConnection } from "../api";
import { t, useLanguage } from "../i18n";
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
    <div className="vkiosk" onDoubleClick={fullscreen}>
      {view ? <VirtualScreen view={view} layout={view.layout} now={now} error={error} /> : error && <div className="vboard-note">{error}</div>}
      {!connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}
