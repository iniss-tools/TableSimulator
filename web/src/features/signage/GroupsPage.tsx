// Skupiny panelov: vsetky panely skupiny hraju to iste - platny playlist skupiny, inak predvoleny playlist alebo spravu.
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteSignageGroup, errorText, getSignageOverviewOptions, PERMISSIONS, useCan, type SetSignageGroupRequest, type SignageGroupDto } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import { SignageBackup } from "./SignageBackup";
import { GroupDialog } from "./GroupDialog";
import { messageName } from "./signage";

const column = columnsFor<SignageGroupDto>();

const EMPTY: SetSignageGroupRequest = { name: "", note: "", playlists: [], defaultPlaylist: null, defaultMessage: null };

export function GroupsPage() {
  const edit = useCan()(PERMISSIONS.signage);
  const data = useQuery(getSignageOverviewOptions()).data;
  const [editing, setEditing] = useState<{ id: number | null; group: SetSignageGroupRequest } | null>(null);

  const remove = async (group: SignageGroupDto) => {
    if (!(await confirmAction({ title: t("signage.group.delete", { name: group.definition.name }), destructive: true }))) return;
    deleteSignageGroup({ path: { id: group.id }, throwOnError: true }).catch((e) => toast.error(errorText(e)));
  };

  const columns = useMemo(() => {
    const playlistName = (id: number) => data?.playlists.find((p) => p.id === id)?.definition.name ?? `#${id}`;
    return [
      column.accessor((g) => g.definition.name, { id: "name", header: t("signage.col.name") }),
      column.accessor((g) => g.definition.playlists.map(playlistName).join(", "), {
        id: "playlists",
        header: t("signage.col.playlists"),
        cell: ({ getValue }) => getValue() || <span className="text-muted-foreground">{t("signage.none")}</span>,
      }),
      column.accessor(
        (g) =>
          g.definition.defaultPlaylist !== null
            ? t("signage.fallback.playlist", { name: playlistName(g.definition.defaultPlaylist) })
            : g.definition.defaultMessage !== null
              ? t("signage.fallback.message", { name: messageName(g.definition.defaultMessage, data) })
              : "",
        {
          id: "fallback",
          header: t("signage.col.fallback"),
          cell: ({ getValue }) => getValue() || <span className="text-muted-foreground">{t("signage.none")}</span>,
        },
      ),
      column.accessor((g) => data?.panels.filter((p) => p.definition.group === g.id).length ?? 0, { id: "panels", header: t("signage.col.panels") }),
      column.accessor((g) => g.definition.note, { id: "note", header: t("signage.col.note") }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              { label: t("common.edit"), icon: Pencil, onClick: () => setEditing({ id: row.original.id, group: row.original.definition }), hidden: !edit },
              { label: t("common.delete"), icon: Trash2, onClick: () => remove(row.original), destructive: true, hidden: !edit },
            ]}
          />
        ),
      }),
    ];
  }, [edit, data]);

  return (
    <>
      <PageHeader description={t("signage.groups.hint")}>
        <SignageBackup edit={edit} />
        {edit && (
          <Button onClick={() => setEditing({ id: null, group: EMPTY })}>
            <Plus />
            {t("signage.group.add")}
          </Button>
        )}
      </PageHeader>
      <DataTable
        columns={columns}
        data={data?.groups ?? []}
        getRowId={(g) => String(g.id)}
        pageSize={null}
        empty={t("signage.groups.empty")}
        storageKey="signage-groups"
      />
      {editing && data && <GroupDialog id={editing.id} initial={editing.group} data={data} onClose={() => setEditing(null)} />}
    </>
  );
}
