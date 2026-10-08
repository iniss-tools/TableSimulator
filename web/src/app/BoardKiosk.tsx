// Tabula alebo vsetky tabule bez ovladania (#/view/<id>, #/wall) - pre televizor, tablet, druhu obrazovku.
import { useConnection, useSimulatorState } from "@/api";
import { BoardView } from "@/features/boards/BoardView";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { KioskControls } from "./KioskControls";
import { toggleFullscreen, useKiosk } from "./kiosk";

export function BoardKiosk({ boardId }: { boardId: string | null }) {
  const live = useSimulatorState();
  const { connected } = useConnection();
  const boards = boardId ? live.boards.filter((b) => b.id === boardId) : live.boards;
  const board = boardId ? boards[0] : undefined;
  const active = useKiosk(boardId ? `/view/${boardId}` : "/wall", board ? board.name || board.id : t("nav.wall"));
  return (
    <div
      className={cn(boardId ? "h-dvh bg-black" : "flex min-h-dvh flex-col gap-[2vh] bg-black px-[2vw] py-[2vh]", !active && "cursor-none")}
      onDoubleClick={toggleFullscreen}
    >
      {boards.map((b) => (
        <BoardView key={b.id} board={b} fit={boardId ? "contain" : "width"} className={boardId ? "h-dvh" : undefined} />
      ))}
      {!connected && <div className="fixed bottom-3 left-3 text-[13px] text-[#ffb000]">{t("server.offline")}</div>}
      <KioskControls visible={active} />
    </div>
  );
}
