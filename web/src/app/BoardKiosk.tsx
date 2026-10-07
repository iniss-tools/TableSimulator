// Tabula alebo vsetky tabule bez ovladania (#/view/<id>, #/wall) - pre televizor, tablet, druhu obrazovku.
import { useConnection, useSimulatorState } from "@/api";
import { BoardView } from "@/components/BoardView";
import { t } from "@/i18n";

function fullscreen() {
  if (document.fullscreenElement) document.exitFullscreen();
  else document.documentElement.requestFullscreen?.().catch(() => undefined);
}

export function BoardKiosk({ boardId }: { boardId: string | null }) {
  const live = useSimulatorState();
  const { connected } = useConnection();
  const boards = boardId ? live.boards.filter((b) => b.id === boardId) : live.boards;
  return (
    <div className={boardId ? "kiosk kiosk-single" : "kiosk"} onDoubleClick={fullscreen}>
      {boards.map((b) => (
        <BoardView key={b.id} board={b} fit={boardId ? "contain" : "width"} />
      ))}
      {!connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}
