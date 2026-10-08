// Ram aplikacie: bocny panel, zahlavie s nazvom stranky a stav spojenia, obsah stranky (ak na nu ma pouzivatel pravo).
import { Outlet, useRouterState } from "@tanstack/react-router";
import { CircleAlert } from "lucide-react";
import { can, useConnection, useMe, useSimulatorState } from "@/api";
import { Separator } from "@/components/ui/separator";
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { t } from "@/i18n";
import { AppSidebar } from "./AppSidebar";
import { navItem } from "./navigation";

export function AppShell() {
  // ram sa ukazuje az po zisteni pouzivatela (Root)
  const me = useMe()!;
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const { connected } = useConnection();
  const { loaded } = useSimulatorState();
  const item = navItem(pathname);
  const allowed = item === undefined || can(me, item.permission);

  return (
    <SidebarProvider>
      <AppSidebar me={me} />
      <SidebarInset className="min-w-0">
        <header className="sticky top-0 z-10 flex h-12 shrink-0 items-center gap-2 border-b bg-background px-3">
          <SidebarTrigger />
          <Separator orientation="vertical" className="mr-1 data-[orientation=vertical]:h-4" />
          <h1 className="truncate text-sm font-medium">{item ? t(item.label) : t("app.title")}</h1>
        </header>
        {!connected && loaded && (
          <div
            role="status"
            className="flex items-center gap-2 border-b border-amber-500/30 bg-amber-500/15 px-4 py-2 text-sm text-amber-900 dark:text-amber-200"
          >
            <CircleAlert className="size-4 shrink-0" />
            {t("server.offline")}
          </div>
        )}
        <div className="mx-auto w-full max-w-[1600px] flex-1 p-4">
          {allowed ? <Outlet /> : <p className="text-sm text-muted-foreground">{t("nav.forbidden")}</p>}
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
