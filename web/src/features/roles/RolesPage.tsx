// Roly s opravneniami: vstavane (spravca ma vzdy vsetko) a vlastne. Kto spravuje pouzivatelov, smie upravit len
// roly, ktorych opravnenia sam ma, a nie vlastnu.
import { useCallback, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { ALL_PERMISSIONS, deleteRole, errorText, getRolesOptions, isBuiltInRole, PERMISSIONS, useMe, type Permission, type RoleDto } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import { permissionLabel, refreshRoles, roleLabel } from "./roles";
import { RoleDialog } from "./RoleDialog";

const column = columnsFor<RoleDto>();
const NEW_ROLE: RoleDto = { name: "", displayName: "", builtIn: false, permissions: [PERMISSIONS.view], users: 0, keys: 0 };

export function RolesPage() {
  const me = useMe();
  const query = useQuery(getRolesOptions());
  const [editing, setEditing] = useState<{ role: RoleDto; isNew: boolean } | null>(null);
  // rolu smie upravit, ak ma vsetky jej opravnenia a nie je to jeho rola
  const manageable = useCallback((role: RoleDto) => role.name !== me?.role && role.permissions.every((p) => me?.permissions.includes(p)), [me]);

  const remove = useCallback(async (role: RoleDto) => {
    if (await confirmAction({ title: t("roles.delete.confirm", { name: roleLabel(role.name, role.displayName) }), destructive: true }))
      deleteRole({ path: { name: role.name }, throwOnError: true }).then(refreshRoles, (e) => toast.error(errorText(e)));
  }, []);

  const columns = useMemo(
    () => [
      column.accessor((r) => roleLabel(r.name, r.displayName), {
        id: "name",
        header: t("roles.name"),
        cell: ({ row }) => (
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium">{roleLabel(row.original.name, row.original.displayName)}</span>
            {row.original.builtIn && <StatusBadge tone="off">{t("roles.builtIn")}</StatusBadge>}
            {row.original.name === me?.role && <span className="text-xs text-muted-foreground">{t("roles.yours")}</span>}
          </div>
        ),
      }),
      column.accessor((r) => r.permissions.length, {
        id: "permissions",
        header: t("roles.permissions"),
        cell: ({ row }) => {
          const list = row.original.permissions as Permission[];
          return (
            <span className="text-muted-foreground" title={list.map(permissionLabel).join("\n")}>
              {row.original.name === "admin" ? t("roles.all") : t("roles.count", { count: list.length, total: ALL_PERMISSIONS.length })}
            </span>
          );
        },
      }),
      column.accessor("users", { header: t("roles.users"), meta: { className: "w-28 text-right" } }),
      column.accessor("keys", { header: t("roles.keys"), meta: { className: "w-28 text-right" } }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => {
          const role = row.original;
          return (
            <RowActions
              actions={[
                {
                  label: t("common.edit"),
                  icon: Pencil,
                  hidden: role.name === "admin" || !manageable(role),
                  onClick: () => setEditing({ role, isNew: false }),
                },
                {
                  label: t("common.delete"),
                  icon: Trash2,
                  destructive: true,
                  hidden: isBuiltInRole(role.name) || !manageable(role),
                  onClick: () => remove(role),
                },
              ]}
            />
          );
        },
      }),
    ],
    [manageable, me?.role, remove],
  );

  return (
    <>
      <PageHeader description={t("roles.hint")}>
        <Button onClick={() => setEditing({ role: NEW_ROLE, isNew: true })}>
          <Plus />
          {t("roles.add")}
        </Button>
      </PageHeader>
      {query.error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{errorText(query.error)}</AlertDescription>
        </Alert>
      )}
      <DataTable columns={columns} data={query.data ?? []} getRowId={(r) => r.name} pageSize={null} />
      {editing && <RoleDialog initial={editing.role} isNew={editing.isNew} onClose={() => setEditing(null)} />}
    </>
  );
}
