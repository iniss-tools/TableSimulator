// Aplikacia MCP Apps so zivou tabulou: argumenty show_board urcia tabulu, udaje prichadzaju dookola z board_view
// (dlhe cakanie na zmenu) a kresli ich BoardView ako web - bez stahovania, ramec hostitela nesmie na siet.
import { useEffect, useState } from "react";
import { BoardView } from "@/features/boards/BoardView";
import type { McpAppBridge } from "./bridge";
import { AppFrame, LiveContent } from "./AppFrame";
import { viewState, type BoardViewData, type BoardViewState } from "./board-view";
import { follow } from "./follow";

/** Ako dlho server najviac caka na zmenu tabule [s]. */
const WAIT_SECONDS = 25;

export function BoardApp({ bridge }: { bridge: McpAppBridge }) {
  return <AppFrame bridge={bridge}>{(args) => typeof args.id === "string" && <LiveBoard bridge={bridge} id={args.id} />}</AppFrame>;
}

function LiveBoard({ bridge, id }: { bridge: McpAppBridge; id: string }) {
  const [view, setView] = useState<BoardViewState | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    // prvy raz hned, potom vzdy cakanie na novsiu verziu; pismo len ked sa zmeni subor
    let version = 0;
    let wait = false;
    let font: string | null = null;
    void follow<BoardViewData>(
      bridge,
      "board_view",
      {
        args: () => ({ id, afterVersion: version, waitForChange: wait, timeoutSeconds: WAIT_SECONDS, knownFontFile: font }),
        data: (data) => {
          if (data.font) font = data.font.file;
          version = data.board.version;
          wait = true;
          setView(viewState(data));
        },
        error: setError,
      },
      () => alive,
    );
    return () => {
      alive = false;
    };
  }, [bridge, id]);

  return <LiveContent error={error}>{view && <BoardView board={view.board} design={view.design} face={view.face} />}</LiveContent>;
}
