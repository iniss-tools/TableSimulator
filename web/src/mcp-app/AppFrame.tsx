// Ram aplikacie MCP Apps: podanie ruky s hostitelom (jazyk), argumenty volania nastroja (show_board, show_virtual_board)
// a vyska obsahu pre ramec hostitela.
import { useEffect, useRef, useState, type ReactNode } from "react";
import { setLanguage, t } from "@/i18n";
import type { McpAppBridge } from "./bridge";

interface AppFrameProps {
  bridge: McpAppBridge;
  /** Obsah podla argumentov volania nastroja. */
  children: (args: Record<string, unknown>) => ReactNode;
}

export function AppFrame({ bridge, children }: AppFrameProps) {
  const [args, setArgs] = useState<Record<string, unknown> | null>(null);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // ramec patri jednemu volaniu nastroja - platia argumenty z prveho oznamenia
    const off = bridge.on("ui/notifications/tool-input", (params) => {
      const input = (params as { arguments?: Record<string, unknown> } | undefined)?.arguments;
      if (input) setArgs((current) => current ?? input);
    });
    void bridge.initialize("TableSimulator").then(
      (locale) => {
        if (locale?.toLowerCase().startsWith("cs")) setLanguage("cs");
      },
      // hostitel bez odpovede na podanie ruky - argumenty mozu prist aj tak
      () => undefined,
    );
    return off;
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
      {args ? children(args) : <p className="text-sm text-neutral-400">{t("mcpApp.waiting")}</p>}
    </div>
  );
}

/** Zobrazenie zo servera - kym nepride, cakanie alebo chyba; potom obsah a pod nim posledna chyba spojenia. */
export function LiveContent({ error, children }: { error: string | null; children: ReactNode }) {
  if (!children) return <p className="text-sm text-neutral-400">{error ? t("mcpApp.error", { error }) : t("mcpApp.waiting")}</p>;
  return (
    <>
      {children}
      {error && <p className="mt-1 text-xs text-red-400">{t("mcpApp.error", { error })}</p>}
    </>
  );
}
