// Tabula alebo vsetky tabule bez ovladania (#/view/<id>, #/wall) - pre televizor, tablet, druhu obrazovku.
import { useConnection, useSimulatorState } from "@/api";
import { BoardView } from "@/features/boards/BoardView";
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
    <div className={boardId ? "h-dvh bg-black" : "flex min-h-dvh flex-col gap-[2vh] bg-black px-[2vw] py-[2vh]"} onDoubleClick={fullscreen}>
      {boards.map((b) => (
        <BoardView key={b.id} board={b} fit={boardId ? "contain" : "width"} className={boardId ? "h-dvh" : undefined} />
      ))}
      {!connected && <div className="fixed bottom-3 left-3 text-[13px] text-[#ffb000]">{t("server.offline")}</div>}
    </div>
  );
}
