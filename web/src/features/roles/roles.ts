// Spolocne pre roly: nazov roly, skupiny opravneni v okne roly, zoznam roli pre vyber a obnovenie po zmene.
import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getRolesOptions, getRolesQueryKey, isBuiltInRole, PERMISSIONS, queryClient, type Permission, type RoleDto } from "@/api";
import { t, type TextKey } from "@/i18n";

/** Nazov roly: vstavana podla jazyka, vlastna jej nazov. */
export function roleLabel(name: string, displayName?: string | null): string {
  return isBuiltInRole(name) ? t(`role.${name}`) : displayName || name;
}

/** Popis opravnenia pre obsluhu. */
export const permissionLabel = (permission: Permission) => t(`permission.${permission}` as TextKey);

/** Opravnenia po skupinach v poradi menu. */
export const PERMISSION_GROUPS: { label: TextKey; permissions: Permission[] }[] = [
  {
    label: "permissions.group.view",
    permissions: [PERMISSIONS.view, PERMISSIONS.log, PERMISSIONS.archive, PERMISSIONS.statistics, PERMISSIONS.audit],
  },
  {
    label: "permissions.group.simulation",
    permissions: [PERMISSIONS.lines, PERMISSIONS.boards, PERMISSIONS.scenarios, PERMISSIONS.designs, PERMISSIONS.recordings],
  },
  { label: "permissions.group.inissview", permissions: [PERMISSIONS.virtual, PERMISSIONS.signage] },
  { label: "permissions.group.admin", permissions: [PERMISSIONS.settings, PERMISSIONS.server, PERMISSIONS.users, PERMISSIONS.keys] },
];

/** Strucny popis, co rola smie (do napovedy pri vybere roly). */
export function roleSummary(role: RoleDto | undefined): string {
  if (!role) return "";
  if (role.name === "admin") return t("roles.admin.summary");
  return t("roles.summary", { list: role.permissions.map((p) => permissionLabel(p as Permission)).join(", ") });
}

/** Zoznam roli sa po zmene nacita znova. */
export const refreshRoles = () => queryClient.invalidateQueries({ queryKey: getRolesQueryKey() });

/** Roly zo servera: zoznam pre vyber, nazov a popis podla kluca. */
export function useRoles() {
  const data = useQuery(getRolesOptions()).data;
  // stabilne, kym sa zoznam nezmeni (stlpce tabuliek od neho zavisia)
  return useMemo(() => {
    const list = data ?? [];
    const find = (name: string) => list.find((r) => r.name === name);
    return {
      list,
      find,
      label: (name: string) => roleLabel(name, find(name)?.displayName),
      options: list.map((r) => ({ value: r.name, label: roleLabel(r.name, r.displayName) })),
    };
  }, [data]);
}
