// Hlavne menu: skupiny a stranky s ikonou a opravnenim (ram podla neho stranku ukaze alebo odmietne).
import { Activity, Archive, Cable, Disc3, History, KeyRound, LayoutGrid, Megaphone, MonitorPlay, Settings, Users, type LucideIcon } from "lucide-react";
import { PERMISSIONS, type Permission } from "@/api";
import type { TextKey } from "@/i18n";

/** Stranky v rame (adresy za # - rovnake ako pred prestavbou, aby ulozene odkazy platili). */
export type NavPath =
  "/" | "/lines" | "/log" | "/recordings" | "/virtual" | "/archive/tables" | "/archive/reports" | "/users" | "/keys" | "/audit" | "/settings";

export interface NavItem {
  to: NavPath;
  label: TextKey;
  icon: LucideIcon;
  permission: Permission;
}

export interface NavGroup {
  label: TextKey;
  items: NavItem[];
}

export const NAV: NavGroup[] = [
  {
    label: "nav.group.simulation",
    items: [
      { to: "/", label: "nav.boards", icon: LayoutGrid, permission: PERMISSIONS.view },
      { to: "/lines", label: "nav.lines", icon: Cable, permission: PERMISSIONS.view },
      { to: "/log", label: "nav.log", icon: Activity, permission: PERMISSIONS.log },
      { to: "/recordings", label: "nav.recordings", icon: Disc3, permission: PERMISSIONS.view },
    ],
  },
  {
    label: "nav.group.inissview",
    items: [
      { to: "/virtual", label: "nav.virtual", icon: MonitorPlay, permission: PERMISSIONS.view },
      { to: "/archive/tables", label: "nav.archiveTables", icon: Archive, permission: PERMISSIONS.view },
      { to: "/archive/reports", label: "nav.archiveReports", icon: Megaphone, permission: PERMISSIONS.view },
    ],
  },
  {
    label: "nav.group.admin",
    items: [
      { to: "/users", label: "nav.users", icon: Users, permission: PERMISSIONS.users },
      { to: "/keys", label: "nav.keys", icon: KeyRound, permission: PERMISSIONS.users },
      { to: "/audit", label: "nav.audit", icon: History, permission: PERMISSIONS.audit },
      { to: "/settings", label: "nav.settings", icon: Settings, permission: PERMISSIONS.settings },
    ],
  },
];

/** Polozka menu stranky na adrese; undefined = adresa mimo menu. */
export function navItem(pathname: string): NavItem | undefined {
  for (const group of NAV) for (const item of group.items) if (item.to === pathname) return item;
  return undefined;
}
