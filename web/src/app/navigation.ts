// Hlavne menu: skupiny a stranky s ikonou a opravnenim (ram podla neho stranku ukaze alebo odmietne).
import {
  Activity,
  Archive,
  Cable,
  ChartColumn,
  Disc3,
  Frame,
  GitCompareArrows,
  Group,
  History,
  KeyRound,
  LayoutGrid,
  ListVideo,
  ShieldUser,
  Megaphone,
  MessageSquareText,
  Monitor,
  MonitorPlay,
  Palette,
  RadioTower,
  Settings,
  Siren,
  Users,
  type LucideIcon,
} from "lucide-react";
import { PERMISSIONS, type Permission } from "@/api";
import type { TextKey } from "@/i18n";

/** Stranky v rame (adresy za # - rovnake ako pred prestavbou, aby ulozene odkazy platili). */
export type NavPath =
  | "/"
  | "/designs"
  | "/faces"
  | "/lines"
  | "/scenarios"
  | "/log"
  | "/statistics"
  | "/recordings"
  | "/recordings/compare"
  | "/virtual"
  | "/archive/tables"
  | "/archive/reports"
  | "/archive/reports/live"
  | "/signage/panels"
  | "/signage/groups"
  | "/signage/playlists"
  | "/signage/messages"
  | "/users"
  | "/roles"
  | "/keys"
  | "/audit"
  | "/settings";

export interface NavItem {
  to: NavPath;
  label: TextKey;
  icon: LucideIcon;
  /** Opravnenie stranky; pri zozname staci jedno z nich. */
  permission: Permission | readonly Permission[];
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
      { to: "/designs", label: "nav.designs", icon: Palette, permission: PERMISSIONS.designs },
      { to: "/faces", label: "nav.faces", icon: Frame, permission: PERMISSIONS.designs },
      { to: "/lines", label: "nav.lines", icon: Cable, permission: PERMISSIONS.view },
      { to: "/scenarios", label: "nav.scenarios", icon: Siren, permission: PERMISSIONS.view },
      { to: "/log", label: "nav.log", icon: Activity, permission: PERMISSIONS.log },
      { to: "/statistics", label: "nav.statistics", icon: ChartColumn, permission: PERMISSIONS.statistics },
      { to: "/recordings", label: "nav.recordings", icon: Disc3, permission: PERMISSIONS.view },
      { to: "/recordings/compare", label: "nav.compare", icon: GitCompareArrows, permission: PERMISSIONS.view },
    ],
  },
  {
    label: "nav.group.inissview",
    items: [
      { to: "/virtual", label: "nav.virtual", icon: MonitorPlay, permission: PERMISSIONS.view },
      { to: "/archive/tables", label: "nav.archiveTables", icon: Archive, permission: PERMISSIONS.archive },
      { to: "/archive/reports", label: "nav.archiveReports", icon: Megaphone, permission: PERMISSIONS.archive },
      { to: "/archive/reports/live", label: "nav.reportMonitor", icon: RadioTower, permission: PERMISSIONS.archive },
    ],
  },
  {
    label: "nav.group.signage",
    items: [
      { to: "/signage/panels", label: "nav.signagePanels", icon: Monitor, permission: PERMISSIONS.view },
      { to: "/signage/groups", label: "nav.signageGroups", icon: Group, permission: PERMISSIONS.view },
      { to: "/signage/playlists", label: "nav.signagePlaylists", icon: ListVideo, permission: PERMISSIONS.view },
      { to: "/signage/messages", label: "nav.signageMessages", icon: MessageSquareText, permission: PERMISSIONS.view },
    ],
  },
  {
    label: "nav.group.admin",
    items: [
      { to: "/users", label: "nav.users", icon: Users, permission: PERMISSIONS.users },
      { to: "/roles", label: "nav.roles", icon: ShieldUser, permission: PERMISSIONS.users },
      { to: "/keys", label: "nav.keys", icon: KeyRound, permission: PERMISSIONS.keys },
      { to: "/audit", label: "nav.audit", icon: History, permission: PERMISSIONS.audit },
      { to: "/settings", label: "nav.settings", icon: Settings, permission: [PERMISSIONS.settings, PERMISSIONS.server] },
    ],
  },
];

/** Polozka menu stranky na adrese; undefined = adresa mimo menu. */
export function navItem(pathname: string): NavItem | undefined {
  for (const group of NAV) for (const item of group.items) if (item.to === pathname) return item;
  return undefined;
}
