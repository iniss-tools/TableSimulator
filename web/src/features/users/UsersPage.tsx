// Pouzivatelia simulatora a ich roly - len pre spravcu.
import { useCallback, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Info, KeyRound, LockOpen, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteUser, errorText, getUsersOptions, unlockUser, useMe, type UserDto } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { formatDateTime } from "@/lib/format";
import { t } from "@/i18n";
import { useRoles } from "@/features/roles/roles";
import { PasswordDialog } from "./PasswordDialog";
import { UserDialog } from "./UserDialog";
import { refreshUsers } from "./users";

/** Riadok tabulky s nazvom roly - stlpec si hodnotu pamata, kym sa nezmenia data, preto je nazov priamo v nich. */
type UserRow = UserDto & { roleName: string };

const column = columnsFor<UserRow>();
const NEW_USER: UserDto = { userName: "", displayName: "", role: "viewer", disabled: false, lockedUntil: null, createdAt: "", lastLoginAt: null };

const act = (action: Promise<unknown>) => action.then(refreshUsers, (e) => toast.error(errorText(e)));

export function UsersPage() {
  const me = useMe();
  const query = useQuery(getUsersOptions());
  const roles = useRoles();
  const [editing, setEditing] = useState<{ user: UserDto; isNew: boolean } | null>(null);
  const [password, setPassword] = useState<string | null>(null);
  const self = useCallback((u: UserDto) => me?.loginRequired === true && me.userName?.toLowerCase() === u.userName.toLowerCase(), [me]);

  const remove = async (u: UserDto) => {
    if (await confirmAction({ title: t("users.delete.confirm", { name: u.userName }), destructive: true }))
      void act(deleteUser({ path: { userName: u.userName }, throwOnError: true }));
  };

  // nazov roly v datach riadkov - po nacitani roli vzniknu nove riadky (aj pri vlastnej role)
  const rows = useMemo<UserRow[]>(() => (query.data ?? []).map((u) => ({ ...u, roleName: roles.label(u.role) })), [query.data, roles]);

  const columns = useMemo(
    () => [
      column.accessor("userName", {
        header: t("auth.userName"),
        cell: ({ row }) => {
          const u = row.original;
          return (
            <div className="flex flex-col">
              <code className="font-mono text-xs">{u.userName}</code>
              {u.displayName && u.displayName !== u.userName && <span className="text-xs text-muted-foreground">{u.displayName}</span>}
              {self(u) && <span className="text-xs text-muted-foreground">{t("users.you")}</span>}
            </div>
          );
        },
      }),
      column.accessor("roleName", { header: t("users.role") }),
      column.accessor((u) => (u.disabled ? t("users.disabled") : u.lockedUntil ? t("users.locked", { time: "" }) : t("users.active")), {
        id: "state",
        header: t("users.state"),
        cell: ({ row }) => {
          const u = row.original;
          return u.disabled ? (
            <StatusBadge tone="off">{t("users.disabled")}</StatusBadge>
          ) : u.lockedUntil ? (
            <StatusBadge tone="bad">{t("users.locked", { time: formatDateTime(u.lockedUntil) })}</StatusBadge>
          ) : (
            <StatusBadge tone="ok">{t("users.active")}</StatusBadge>
          );
        },
      }),
      column.accessor("createdAt", {
        header: t("users.created"),
        cell: ({ getValue }) => <span className="whitespace-nowrap">{formatDateTime(getValue())}</span>,
      }),
      column.accessor((u) => u.lastLoginAt ?? "", {
        id: "lastLogin",
        header: t("users.lastLogin"),
        cell: ({ row }) =>
          row.original.lastLoginAt ? (
            <span className="whitespace-nowrap">{formatDateTime(row.original.lastLoginAt)}</span>
          ) : (
            <span className="text-muted-foreground">{t("users.never")}</span>
          ),
      }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => {
          const u = row.original;
          return (
            <RowActions
              actions={[
                { label: t("common.edit"), icon: Pencil, onClick: () => setEditing({ user: u, isNew: false }) },
                { label: t("users.setPassword"), icon: KeyRound, onClick: () => setPassword(u.userName) },
                {
                  label: t("users.unlock"),
                  icon: LockOpen,
                  hidden: !u.lockedUntil,
                  onClick: () => act(unlockUser({ path: { userName: u.userName }, throwOnError: true })),
                },
                { label: t("common.delete"), icon: Trash2, destructive: true, hidden: self(u), onClick: () => remove(u) },
              ]}
            />
          );
        },
      }),
    ],
    // stlpce zavisia od prihlaseneho (self); nazov roly je v datach riadkov
    [self],
  );

  return (
    <>
      <PageHeader description={t("users.hint")}>
        <Button onClick={() => setEditing({ isNew: true, user: NEW_USER })}>
          <Plus />
          {t("users.add")}
        </Button>
      </PageHeader>
      {!me?.loginRequired && (
        <Alert className="mb-4">
          <Info />
          <AlertDescription>{t("users.loginOff")}</AlertDescription>
        </Alert>
      )}
      {query.error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{errorText(query.error)}</AlertDescription>
        </Alert>
      )}
      <DataTable columns={columns} data={rows} getRowId={(u) => u.userName} pageSize={null} />
      {editing && <UserDialog initial={editing.user} isNew={editing.isNew} onClose={() => setEditing(null)} />}
      {password && (
        <PasswordDialog
          userName={password}
          onClose={() => {
            setPassword(null);
            void refreshUsers();
          }}
        />
      )}
    </>
  );
}
