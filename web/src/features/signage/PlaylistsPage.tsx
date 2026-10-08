// Playlisty informacnych panelov: spravy po poradi s casom platnosti; skupina panelov hra platny playlist.
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Copy, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  deleteSignagePlaylist,
  errorText,
  getSignageOverviewOptions,
  PERMISSIONS,
  useCan,
  type SetSignagePlaylistRequest,
  type SignagePlaylistDto,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import { PlaylistDialog } from "./PlaylistDialog";
import { durationText, itemDuration, newPlaylist, playlistSummary, todayIso } from "./signage";

const column = columnsFor<SignagePlaylistDto>();

export function PlaylistsPage() {
  const edit = useCan()(PERMISSIONS.edit);
  const data = useQuery(getSignageOverviewOptions()).data;
  const [editing, setEditing] = useState<{ id: number | null; playlist: SetSignagePlaylistRequest } | null>(null);

  const remove = async (playlist: SignagePlaylistDto) => {
    if (!(await confirmAction({ title: t("signage.playlist.delete", { name: playlist.definition.name }), destructive: true }))) return;
    deleteSignagePlaylist({ path: { id: playlist.id }, throwOnError: true }).catch((e) => toast.error(errorText(e)));
  };

  const columns = useMemo(
    () => [
      column.accessor((p) => p.definition.name, { id: "name", header: t("signage.col.name") }),
      column.accessor((p) => playlistSummary(p.definition), { id: "valid", header: t("signage.col.valid") }),
      column.accessor((p) => p.definition.items.length, {
        id: "items",
        header: t("signage.col.items"),
        cell: ({ row }) =>
          data &&
          t("signage.items.total", {
            count: row.original.definition.items.length,
            duration: durationText(row.original.definition.items.reduce((sum, i) => sum + itemDuration(i, data), 0)),
          }),
      }),
      column.accessor(
        (p) =>
          data?.groups
            .filter((g) => g.definition.playlists.includes(p.id) || g.definition.defaultPlaylist === p.id)
            .map((g) => g.definition.name)
            .join(", ") ?? "",
        {
          id: "groups",
          header: t("signage.col.groups"),
          cell: ({ getValue }) => <span className="text-muted-foreground">{getValue() || t("signage.unused")}</span>,
        },
      ),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              { label: t("common.edit"), icon: Pencil, onClick: () => setEditing({ id: row.original.id, playlist: row.original.definition }), hidden: !edit },
              {
                label: t("signage.duplicate"),
                icon: Copy,
                onClick: () => setEditing({ id: null, playlist: { ...row.original.definition, name: row.original.definition.name + t("designs.copySuffix") } }),
                hidden: !edit,
              },
              { label: t("common.delete"), icon: Trash2, onClick: () => remove(row.original), destructive: true, hidden: !edit },
            ]}
          />
        ),
      }),
    ],
    [edit, data],
  );

  return (
    <>
      <PageHeader description={t("signage.playlists.hint")}>
        {edit && (
          <Button onClick={() => setEditing({ id: null, playlist: newPlaylist(todayIso()) })}>
            <Plus />
            {t("signage.playlist.add")}
          </Button>
        )}
      </PageHeader>
      <DataTable
        columns={columns}
        data={data?.playlists ?? []}
        getRowId={(p) => String(p.id)}
        pageSize={null}
        empty={t("signage.playlists.empty")}
        storageKey="signage-playlists"
      />
      {editing && data && <PlaylistDialog id={editing.id} initial={editing.playlist} data={data} onClose={() => setEditing(null)} />}
    </>
  );
}
