// Pristupove kluce: zariadenia (odkaz s klucom, len pozeranie) a programy (GVDEditor, skripty).
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteAccessKey, errorText, getAccessKeysOptions, getAccessKeysQueryKey, queryClient, type AccessKeyCreatedDto, type AccessKeyDto } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { formatDateTime } from "@/lib/format";
import { t, type TextKey } from "@/i18n";
import { CreatedKeyDialog, NewKeyDialog } from "./KeyDialogs";

const refreshKeys = () => queryClient.invalidateQueries({ queryKey: getAccessKeysQueryKey() });

const column = columnsFor<AccessKeyDto>();

export function KeysPage() {
  const query = useQuery(getAccessKeysOptions());
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState<AccessKeyCreatedDto | null>(null);

  const remove = async (k: AccessKeyDto) => {
    if (!(await confirmAction({ title: t("keys.delete.confirm", { name: k.name }), destructive: true }))) return;
    deleteAccessKey({ path: { id: k.id }, throwOnError: true }).then(refreshKeys, (e) => toast.error(errorText(e)));
  };

  const columns = useMemo(
    () => [
      column.accessor("name", { header: t("keys.name"), cell: ({ getValue }) => <span className="font-medium">{getValue()}</span> }),
      column.accessor((k) => t(`keys.kind.${k.kind}` as TextKey), { id: "kind", header: t("keys.kind") }),
      column.accessor((k) => t(`role.${k.role}` as TextKey), { id: "role", header: t("users.role") }),
      column.accessor("prefix", { header: t("keys.prefix"), enableSorting: false, cell: ({ getValue }) => <code className="font-mono text-xs">{getValue()}…</code> }),
      column.accessor("createdAt", {
        header: t("keys.created"),
        cell: ({ row }) => (
          <div className="flex flex-col whitespace-nowrap">
            {formatDateTime(row.original.createdAt)}
            <span className="text-xs text-muted-foreground">{row.original.createdBy}</span>
          </div>
        ),
      }),
      column.accessor((k) => k.lastUsedAt ?? "", {
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
        cell: ({ row }) => <RowActions actions={[{ label: t("common.delete"), icon: Trash2, destructive: true, onClick: () => remove(row.original) }]} />,
      }),
    ],
    [],
  );

  return (
    <>
      <PageHeader description={t("keys.hint")}>
        <Button onClick={() => setCreating(true)}>
          <Plus />
          {t("keys.add")}
        </Button>
      </PageHeader>
      {query.error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{errorText(query.error)}</AlertDescription>
        </Alert>
      )}
      <DataTable columns={columns} data={query.data ?? []} getRowId={(k) => k.id} pageSize={null} empty={t("keys.empty")} />
      {creating && (
        <NewKeyDialog
          onClose={(result) => {
            setCreating(false);
            if (result) {
              setCreated(result);
              refreshKeys();
            }
          }}
        />
      )}
      {created && <CreatedKeyDialog created={created} onClose={() => setCreated(null)} />}
    </>
  );
}
