// Aplikacia MCP Apps so zivou virtualnou tabulou: argumenty show_virtual_board urcia tabulu, obsah prichadza dookola
// z virtual_view (server caka na zmenu XML, definicie alebo rozlozenia) a kresli ho VirtualScreen ako kiosk.
// Stranku zo sablony XSL nie - potrebuje subory priecinka INISSView a ramec hostitela nesmie na siet.
import { useEffect, useState } from "react";
import type { VirtualViewDto } from "@/api";
import { VirtualScreen } from "@/features/virtual/VirtualScreen";
import { useNow } from "@/hooks/use-now";
import { t } from "@/i18n";
import type { McpAppBridge } from "./bridge";
import { AppFrame, LiveContent } from "./AppFrame";
import { follow } from "./follow";

/** Ako dlho server najviac caka na zmenu [s]. */
const WAIT_SECONDS = 25;

/** Vysledok virtual_view servera (Mcp/AppTools.cs). */
interface VirtualViewData {
  changed: boolean;
  /** Odtlacok obsahu - dalsie volanie caka na iny. */
  hash: string;
  view: VirtualViewDto;
}

export function VirtualApp({ bridge }: { bridge: McpAppBridge }) {
  return (
    <AppFrame bridge={bridge}>
      {(args) => {
        const id = Number(args.id);
        return Number.isInteger(id) && <LiveVirtual bridge={bridge} id={id} />;
      }}
    </AppFrame>
  );
}

function LiveVirtual({ bridge, id }: { bridge: McpAppBridge; id: number }) {
  const [view, setView] = useState<VirtualViewDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const now = useNow(250);

  useEffect(() => {
    let alive = true;
    let hash: string | null = null;
    void follow<VirtualViewData>(
      bridge,
      "virtual_view",
      {
        args: () => ({ id, afterHash: hash, timeoutSeconds: WAIT_SECONDS }),
        data: (data) => {
          hash = data.hash;
          if (data.changed) setView(data.view);
        },
        error: setError,
      },
      () => alive,
    );
    return () => {
      alive = false;
    };
  }, [bridge, id]);

  return (
    <LiveContent error={error}>
      {view &&
        (view.page ? (
          <p className="text-sm text-neutral-400">{t("mcpApp.template", { template: view.board.template })}</p>
        ) : (
          <VirtualScreen view={view} layout={view.layout} now={now} preview />
        ))}
    </LiveContent>
  );
}
