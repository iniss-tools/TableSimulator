// Virtualna tabula na celu obrazovku (#/virtual/<cislo>) - obsah XML tabule INISSu podla rozlozenia tabule.
import { useEffect, useRef, useState } from "react";
import { api, ApiError, type VirtualView } from "../api";
import { t, useLanguage } from "../i18n";
import { onVirtualChanged, useLive } from "../store";
import { useNow, VirtualScreen } from "./VirtualScreen";

function fullscreen() {
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.().catch(() => undefined);
}

export function VirtualKiosk({ id }: { id: number }) {
  useLanguage();
  const live = useLive();
  const [view, setView] = useState<VirtualView | null>(null);
  const [error, setError] = useState<string | null>(null);
  // subor tabule - zmena ineho XML ju nezaujima
  const source = useRef<string | null>(null);
  const now = useNow(250);

  useEffect(() => {
    const load = () =>
      api
        .virtualView(id)
        .then((v) => {
          source.current = v.board.source.toLowerCase();
          setView(v);
          setError(null);
        })
        .catch((e) => setError(e instanceof ApiError ? e.message : String(e)));
    load();
    return onVirtualChanged((sources, definitions) => {
      if (definitions || sources.some((s) => s.toLowerCase() === source.current)) load();
    });
  }, [id]);

  return (
    <div className="vkiosk" onDoubleClick={fullscreen}>
      {view ? <VirtualScreen view={view} layout={view.layout} now={now} error={error} /> : error && <div className="vboard-note">{error}</div>}
      {!live.connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}
