// Aplikacia MCP Apps so zivou tabulou: argumenty show_board urcia tabulu, udaje prichadzaju dookola z board_view
// (dlhe cakanie na zmenu) a kresli ich BoardView ako web - bez stahovania, ramec hostitela nesmie na siet.
import { useEffect, useRef, useState } from "react";
import { BoardView } from "@/features/boards/BoardView";
import { setLanguage, t } from "@/i18n";
import type { McpAppBridge } from "./bridge";
import { viewState, type BoardViewData, type BoardViewState } from "./board-view";

/** Ako dlho server najviac caka na zmenu tabule [s]. */
const WAIT_SECONDS = 25;

/** Pauza po chybe pred dalsim pokusom [ms]. */
const RETRY_MS = 3000;

export function BoardApp({ bridge }: { bridge: McpAppBridge }) {
  const [view, setView] = useState<BoardViewState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let alive = true;
    let started = false;

    // prvy raz hned, potom vzdy cakanie na novsiu verziu; pismo len ked sa zmeni subor
    const follow = async (id: string) => {
      let version = 0;
      let wait = false;
      let font: string | null = null;
      while (alive) {
        try {
          const data: BoardViewData | null = await bridge.callTool<BoardViewData>("board_view", {
            id,
            afterVersion: version,
            waitForChange: wait,
            timeoutSeconds: WAIT_SECONDS,
            knownFontFile: font,
          });
          if (!alive) return;
          if (data) {
            if (data.font) font = data.font.file;
            version = data.board.version;
            wait = true;
            setView(viewState(data));
            setError(null);
          }
        } catch (e) {
          if (!alive) return;
          setError(e instanceof Error ? e.message : String(e));
          await new Promise((resolve) => setTimeout(resolve, RETRY_MS));
        }
      }
    };

    const off = bridge.on("ui/notifications/tool-input", (params) => {
      const id = (params as { arguments?: { id?: unknown } } | undefined)?.arguments?.id;
      if (typeof id !== "string" || started) return;
      started = true;
      void follow(id);
    });
    void bridge.initialize("TableSimulator").then(
      (locale) => {
        if (locale?.toLowerCase().startsWith("cs")) setLanguage("cs");
      },
      // hostitel bez odpovede na podanie ruky - argumenty mozu prist aj tak
      () => undefined,
    );
    return () => {
      alive = false;
      off();
    };
  }, [bridge]);

  // vyska obsahu pre ramec hostitela
  useEffect(() => {
    const el = root.current;
    if (!el) return;
    // len skutocna zmena - zaokruhlenie mierky tabule by inak hlasilo stale nove vysky o zlomok bodu
    let reported = 0;
    const observer = new ResizeObserver(() => {
      const height = Math.ceil(el.getBoundingClientRect().height);
      if (Math.abs(height - reported) <= 1) return;
      reported = height;
      bridge.resize(height);
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, [bridge]);

  return (
    <div ref={root} className="p-2">
      {view ? (
        <BoardView board={view.board} design={view.design} face={view.face} />
      ) : (
        <p className="text-sm text-neutral-400">{error ? t("mcpApp.error", { error }) : t("mcpApp.waiting")}</p>
      )}
      {view && error && <p className="mt-1 text-xs text-red-400">{t("mcpApp.error", { error })}</p>}
    </div>
  );
}
