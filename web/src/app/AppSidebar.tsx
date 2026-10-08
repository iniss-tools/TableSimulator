// Bocny panel: nazov aplikacie, skupiny menu (len stranky, na ktore ma pouzivatel opravnenie; pri Linkach pocet
// vypadkov) a ponuka pouzivatela.
import { Link, useRouterState } from "@tanstack/react-router";
import { can, useSimulatorState, type MeDto } from "@/api";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  useSidebar,
} from "@/components/ui/sidebar";
import { isOutage } from "@/features/lines/line-status";
import { t } from "@/i18n";
import { NAV } from "./navigation";
import { UserMenu } from "./UserMenu";

export function AppSidebar({ me }: { me: MeDto }) {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const { isMobile, setOpenMobile } = useSidebar();
  // na mobile sa panel po vybere stranky zavrie
  const navigated = () => isMobile && setOpenMobile(false);
  const outages = useSimulatorState().lines.filter((l) => isOutage(l.status)).length;

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" render={<Link to="/" onClick={navigated} />}>
              <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary">
                <img src="/favicon.svg" alt="" className="size-5" />
              </span>
              <span className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-semibold">{t("app.title")}</span>
                <span className="truncate text-xs text-muted-foreground">INISS</span>
              </span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        {NAV.map((group) => {
          const items = group.items.filter((item) => can(me, item.permission));
          if (items.length === 0) return null;
          return (
            <SidebarGroup key={group.label}>
              <SidebarGroupLabel>{t(group.label)}</SidebarGroupLabel>
              <SidebarMenu>
                {items.map((item) => (
                  <SidebarMenuItem key={item.to}>
                    <SidebarMenuButton isActive={pathname === item.to} tooltip={t(item.label)} render={<Link to={item.to} onClick={navigated} />}>
                      <item.icon />
                      <span>{t(item.label)}</span>
                    </SidebarMenuButton>
                    {item.to === "/lines" && outages > 0 && (
                      <SidebarMenuBadge
                        className="bg-red-600 text-white peer-hover/menu-button:text-white peer-data-active/menu-button:text-white"
                        title={t("alert.badge")}
                      >
                        {outages}
                      </SidebarMenuBadge>
                    )}
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroup>
          );
        })}
      </SidebarContent>
      <SidebarFooter>
        <UserMenu me={me} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
