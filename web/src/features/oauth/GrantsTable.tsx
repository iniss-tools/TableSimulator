// Prepojene aplikacie: klienti MCP s povolenym pristupom cez OAuth - kto, s akou rolou, posledne pouzitie a zrusenie.
import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteOAuthGrant, errorText, getOAuthGrantsOptions, getOAuthGrantsQueryKey, queryClient, type OAuthGrantDto } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { roleLabel } from "@/features/roles/roles";
import { formatDateTime } from "@/lib/format";
import { t } from "@/i18n";

const refreshGrants = () => queryClient.invalidateQueries({ queryKey: getOAuthGrantsQueryKey() });

/** Riadok s nazvom roly v datach - stlpec si hodnotu pamata, kym sa nezmenia data. */
type GrantRow = OAuthGrantDto & { roleLabel: string };

const column = columnsFor<GrantRow>();

/** <paramref name="showUser" /> = stlpec pouzivatela (spravca klucov vidi povolenia vsetkych). */
export function GrantsTable({ showUser }: { showUser: boolean }) {
  const query = useQuery(getOAuthGrantsOptions());

  const remove = async (g: OAuthGrantDto) => {
    if (!(await confirmAction({ title: t("oauth.revoke.confirm", { name: g.clientName }), destructive: true }))) return;
    deleteOAuthGrant({ path: { id: g.id }, throwOnError: true }).then(refreshGrants, (e) => toast.error(errorText(e)));
  };

  const rows = useMemo<GrantRow[]>(() => (query.data ?? []).map((g) => ({ ...g, roleLabel: roleLabel(g.role, g.roleName) })), [query.data]);

  const columns = useMemo(
    () => [
      column.accessor("clientName", { header: t("oauth.client"), cell: ({ getValue }) => <span className="font-medium">{getValue()}</span> }),
      ...(showUser ? [column.accessor("userName", { header: t("oauth.user") })] : []),
      column.accessor("roleLabel", { header: t("users.role") }),
      column.accessor("createdAt", {
        header: t("oauth.created"),
        cell: ({ getValue }) => <span className="whitespace-nowrap">{formatDateTime(getValue())}</span>,
      }),
      column.accessor((g) => g.lastUsedAt ?? "", {
        id: "lastUsed",
        header: t("keys.lastUsed"),
        cell: ({ row }) =>
          row.original.lastUsedAt ? (
            <span className="whitespace-nowrap">{formatDateTime(row.original.lastUsedAt)}</span>
          ) : (
            <span className="text-muted-foreground">{t("users.never")}</span>
          ),
      }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => <RowActions actions={[{ label: t("oauth.revoke"), icon: Trash2, destructive: true, onClick: () => remove(row.original) }]} />,
      }),
    ],
    [showUser],
  );

  return (
    <>
      {query.error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{errorText(query.error)}</AlertDescription>
        </Alert>
      )}
      <DataTable columns={columns} data={rows} getRowId={(g) => g.id} pageSize={null} empty={t("oauth.empty")} />
    </>
  );
}
